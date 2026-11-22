using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Setup Follow Camera  (로드맵 1-8)
///  - Main Camera에 CinemachineBrain
///  - "CM Follow Camera": 플레이어를 부드럽게 따라가는 CinemachineCamera
///      Position Composer(따라가기) + Confiner 2D(마을 밖 안 보이게) + Pixel Perfect
///  - "Camera Bounds/Village": 마을 64 × 48칸을 덮는 PolygonCollider2D (Is Trigger)
/// 실행 후 씬을 저장하세요.
/// </summary>
public static class CameraSetupMenu
{
    // MAP.md: 바깥 마을 64 × 48칸, 왼쪽 아래가 (0, 0)
    private static readonly Vector2 VillageMin = new(0f, 0f);
    private static readonly Vector2 VillageMax = new(64f, 48f);

    [MenuItem("Merry Memory/Setup Follow Camera")]
    private static void Setup()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        var mainCam = Camera.main;
        if (player == null || mainCam == null)
        {
            EditorUtility.DisplayDialog("Setup Follow Camera", "씬에 PlayerController와 Main Camera가 있어야 합니다.", "확인");
            return;
        }
        if (Object.FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Setup Follow Camera", "씬에 이미 CinemachineCamera가 있습니다.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Setup Follow Camera");
        int group = Undo.GetCurrentGroup();

        // ── Brain: 실제 카메라를 CinemachineCamera가 움직이게 함 ──
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCam.gameObject);

        // ── 카메라 경계: 이 모양 밖은 화면에 나오지 않음 ──
        var boundsRoot = GameObject.Find("Camera Bounds");
        if (boundsRoot == null)
        {
            boundsRoot = new GameObject("Camera Bounds");
            Undo.RegisterCreatedObjectUndo(boundsRoot, "Create Camera Bounds");
        }
        var village = new GameObject("Village");
        Undo.RegisterCreatedObjectUndo(village, "Create Village Bounds");
        village.transform.SetParent(boundsRoot.transform, false);
        village.layer = LayerMask.NameToLayer("Ignore Raycast");
        var shape = village.AddComponent<PolygonCollider2D>();
        shape.isTrigger = true; // 플레이어를 막지 않도록
        shape.points = new[]
        {
            VillageMin, new Vector2(VillageMax.x, VillageMin.y),
            VillageMax, new Vector2(VillageMin.x, VillageMax.y),
        };

        // ── 따라가는 카메라 ──
        var camGO = new GameObject("CM Follow Camera");
        Undo.RegisterCreatedObjectUndo(camGO, "Create CM Follow Camera");
        camGO.transform.position = player.transform.position + new Vector3(0f, 0f, -10f);

        var cm = camGO.AddComponent<CinemachineCamera>();
        cm.Follow = player.transform;
        var lens = cm.Lens;
        lens.OrthographicSize = mainCam.orthographicSize; // Pixel Perfect가 다시 맞춤
        cm.Lens = lens;

        var composer = camGO.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10f;
        composer.Damping = new Vector3(0.3f, 0.3f, 0f); // 살짝 늦게 따라와서 부드럽게
        // 플레이어 발밑이 아니라 몸 가운데쯤을 화면 중앙에
        composer.TargetOffset = new Vector3(0f, 0.75f, 0f);

        var confiner = camGO.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = shape;

        camGO.AddComponent<CinemachinePixelPerfect>();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(camGO.scene);
        Selection.activeGameObject = camGO;
        Debug.Log("따라가는 카메라를 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
    }
}
