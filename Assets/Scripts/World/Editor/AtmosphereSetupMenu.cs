using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 메뉴 Merry Memory → Setup Snow and Lights (5-1)  (Village 씬에서 실행, 다시 실행하면 새로 만듦)
///  - 눈: 1~2픽셀짜리 눈송이 파티클(Snow.prefab)을 Main Camera 자식으로 → 어디서든 눈이 내림 (OutdoorSnow: 바깥에서만)
///  - 조명: Global Light 2D를 밤(어두운 파랑)으로, 가로등 18개·광장 트리·건물 문 앞에 따뜻한 Point Light 2D
///          실내 5곳은 방 전체를 데우는 큰 불빛 + 벽난로·오븐 불빛 (LampFlicker로 일렁임)
///  스프라이트가 Sprite-Lit-Default여야 빛을 받습니다. (이 프로젝트는 이미 그렇게 되어 있음)
/// </summary>
public static class AtmosphereSetupMenu
{
    private const string EffectsArt = "Assets/Art/Effects";
    private const string FlakePath = EffectsArt + "/SnowFlake.png";
    private const string SnowMatPath = EffectsArt + "/Snow.mat";
    private const string SnowPrefabPath = "Assets/Prefabs/Effects/Snow.prefab";

    private static readonly Color NightGlobal = new(0.62f, 0.7f, 0.95f);
    private static readonly Color Warm = new(1f, 0.78f, 0.45f);
    private static readonly Color RoomWarm = new(1f, 0.88f, 0.7f);
    private static readonly Color Fire = new(1f, 0.55f, 0.25f);

    // 실내 (카메라 경계와 같은 사각형) — DoorSetupMenu와 같은 값
    private static readonly (string name, Rect rect)[] Rooms =
    {
        ("PostOffice",       Rect.MinMaxRect(79, 37, 95, 48)),
        ("ToyWorkshop",      Rect.MinMaxRect(95, 38, 109, 48)),
        ("ClockTower1F",     Rect.MinMaxRect(79, 25, 89, 34)),
        ("ClockTowerBelfry", Rect.MinMaxRect(89, 25, 99, 34)),
        ("Bakery",           Rect.MinMaxRect(109, 38, 123, 48)),
    };

    [MenuItem("Merry Memory/Setup Snow and Lights (5-1)")]
    private static void Setup()
    {
        var cam = Camera.main;
        var bp = GameObject.Find("Village Blueprint");
        if (cam == null || bp == null)
        {
            EditorUtility.DisplayDialog("Setup Snow & Lights", "Village 씬에서 실행하세요.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Setup Snow & Lights");
        int group = Undo.GetCurrentGroup();

        // ── 눈 ──
        var prefab = BuildSnowPrefab();
        var old = cam.transform.Find("Snow");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var snow = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cam.transform);
        snow.name = "Snow";
        snow.transform.localPosition = new Vector3(0f, 0f, 10f); // 카메라 z=-10 → 월드 z=0
        Undo.RegisterCreatedObjectUndo(snow, "Create Snow");

        // ── 조명 ──
        var global = Object.FindFirstObjectByType<Light2D>(FindObjectsInactive.Include);
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) global = l;
        if (global != null)
        {
            Undo.RecordObject(global, "Night Global Light");
            global.color = NightGlobal;
            global.intensity = 0.7f;
        }

        var oldRoot = GameObject.Find("Lights");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        var root = new GameObject("Lights");
        Undo.RegisterCreatedObjectUndo(root, "Create Lights");

        int lamps = 0;
        foreach (var info in bp.GetComponentsInChildren<BlueprintInfo>(true))
        {
            if (info.id.StartsWith("DL"))
            {
                // 가로등: 기둥 머리 쪽에 따뜻한 빛
                Point(root.transform, $"Lamp_{info.id}", info.transform.position + new Vector3(0f, 0.4f), Warm, 1.4f, 0.5f, 3.5f, true);
                lamps++;
            }
            else if (info.id == "P0")
            {
                Point(root.transform, "Tree_Glow", info.transform.position, new Color(1f, 0.85f, 0.55f), 1.3f, 1.5f, 5.5f, true);
            }
        }

        int doors = 0;
        foreach (var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!door.name.EndsWith("_Enter")) continue; // 바깥 쪽 문만
            Point(root.transform, $"DoorGlow_{door.name}", door.transform.position + new Vector3(0f, 0.8f), Warm, 1.0f, 0.3f, 2.2f, false);
            doors++;
        }

        foreach (var (name, rect) in Rooms)
        {
            float r = Mathf.Max(rect.width, rect.height) * 0.75f;
            Point(root.transform, $"Room_{name}", rect.center, RoomWarm, 0.55f, r * 0.6f, r, false);
        }
        // 공방 벽난로 · 빵집 오븐
        Point(root.transform, "Fire_Workshop", new Vector3(107.5f, 42f), Fire, 0.9f, 0.3f, 2.5f, true);
        Point(root.transform, "Fire_Bakery", new Vector3(120f, 46f), Fire, 0.9f, 0.3f, 2.5f, true);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        Debug.Log($"눈과 조명(5-1)을 배치했습니다. 가로등 {lamps}, 문 {doors}, 실내 {Rooms.Length}. 씬을 저장하세요 (Ctrl+S).");
    }

    private static void Point(Transform parent, string name, Vector3 pos, Color color, float intensity, float inner, float outer, bool flicker)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = inner;
        light.pointLightOuterRadius = outer;
        light.falloffIntensity = 0.6f;
        ApplyToAllSortingLayers(light);
        if (flicker) go.AddComponent<LampFlicker>();
    }

    /// <summary>새 Light 2D가 모든 Sorting Layer를 비추게 (기본값이 비어 있을 수 있음)</summary>
    private static void ApplyToAllSortingLayers(Light2D light)
    {
        var so = new SerializedObject(light);
        var arr = so.FindProperty("m_ApplyToSortingLayers");
        if (arr == null) return;
        var layers = SortingLayer.layers;
        arr.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++) arr.GetArrayElementAtIndex(i).intValue = layers[i].id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────────────────────── 눈 프리팹 ─────────────────────────

    private static GameObject BuildSnowPrefab()
    {
        Directory.CreateDirectory(EffectsArt);
        Directory.CreateDirectory(Path.GetDirectoryName(SnowPrefabPath));

        // 2×2 흰 점 텍스처 (점 필터라 또렷한 사각 픽셀)
        if (!File.Exists(FlakePath))
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            File.WriteAllBytes(FlakePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(FlakePath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(FlakePath);
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(SnowMatPath);
        if (mat == null)
        {
            // 조명을 받지 않는(Unlit) 스프라이트 셰이더: 밤이어도 눈은 하얗게
            mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(mat, SnowMatPath);
        }
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FlakePath);
        EditorUtility.SetDirty(mat);

        var go = new GameObject("Snow");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;                                  // 시작하자마자 화면 가득
        main.duration = 5f;
        main.startLifetime = 7f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1f / 32f, 2f / 32f); // 1~2픽셀 (PPU 32)
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.maxParticles = 800;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 카메라가 움직여도 눈은 제자리에서 내림

        var emission = ps.emission;
        emission.rateOverTime = 70f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, 9f, 0.1f);            // 화면(약 10×5.6)보다 넉넉하게

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.55f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 500;                          // 건물·사람보다 앞

        go.AddComponent<OutdoorSnow>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, SnowPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
