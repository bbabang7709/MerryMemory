using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Setup Doors  (Village 씬에서 실행, 로드맵 4-0)
///  - Camera Bounds 아래에 실내 5곳의 카메라 경계를 만들고
///  - Gameplay/Doors 아래에 문 10개(건물 4곳 안·밖 + 시계탑 1층↔종탑 계단)와 도착점을 놓고
///  - CM Follow Camera에 CameraAreaSwitcher를 붙여 연결합니다.
/// 좌표는 MAP.md와 1-7 청사진(실내: 우체국 (80,0), 공방 (96,0), 시계탑 1층 (80,14), 종탑 (90,14), 빵집 (110,0))
/// 을 Unity 월드 좌표로 옮긴 값입니다. (셀 = (x, 47 − y))
/// </summary>
public static class DoorSetupMenu
{
    // 실내 카메라 경계: 방 + 둘레 벽 1칸 (월드 xMin, yMin, xMax, yMax)
    private static readonly (string name, Rect rect)[] Rooms =
    {
        ("PostOffice",       Rect.MinMaxRect(79, 37, 95, 48)),
        ("ToyWorkshop",      Rect.MinMaxRect(95, 38, 109, 48)),
        ("ClockTower1F",     Rect.MinMaxRect(79, 25, 89, 34)),
        ("ClockTowerBelfry", Rect.MinMaxRect(89, 25, 99, 34)),
        ("Bakery",           Rect.MinMaxRect(109, 38, 123, 48)),
    };

    private sealed class DoorSpec
    {
        public string Name, Prompt, Area;
        public Vector2 Center, Size, Destination, Face;
    }

    // 문 위치(트리거) → 도착점, 도착 구역. Area가 "Village"면 마을 경계.
    private static readonly DoorSpec[] Doors =
    {
        // 우체국 W1 (24,37) ↔ 실내 출구 (86,9)
        D("Door_PostOffice_Enter", "들어가기", new(25f, 10.5f), new(2f, 1f), new(87f, 39.1f), Vector2.up, "PostOffice"),
        D("Door_PostOffice_Exit",  "나가기",   new(87f, 38.5f), new(2f, 1f), new(25f, 9.1f),  Vector2.down, "Village"),
        // 장난감 공방 W2 (9,22) ↔ 실내 출구 (101,8)
        D("Door_ToyWorkshop_Enter", "들어가기", new(10f, 25.5f),  new(2f, 1f), new(102f, 40.1f), Vector2.up, "ToyWorkshop"),
        D("Door_ToyWorkshop_Exit",  "나가기",   new(102f, 39.5f), new(2f, 1f), new(10f, 24.1f),  Vector2.down, "Village"),
        // 시계탑 W3 (31,12) ↔ 1층 출구 (83,21)
        D("Door_ClockTower_Enter", "들어가기", new(32f, 35.5f), new(2f, 1f), new(84f, 27.1f), Vector2.up, "ClockTower1F"),
        D("Door_ClockTower_Exit",  "나가기",   new(84f, 26.5f), new(2f, 1f), new(32f, 34.1f), Vector2.down, "Village"),
        // 시계탑 1층 계단 (86,15~16) ↔ 종탑 계단 (91,20)
        D("Stairs_ClockTower_Up",   "올라가기", new(86.5f, 32f),   new(1f, 2f), new(92.5f, 27.1f), Vector2.up, "ClockTowerBelfry"),
        D("Stairs_ClockTower_Down", "내려가기", new(91.5f, 27.5f), new(1f, 1f), new(86.5f, 30.1f), Vector2.down, "ClockTower1F"),
        // 빵집 W4 (53,22) ↔ 실내 출구 (115,8)
        D("Door_Bakery_Enter", "들어가기", new(54f, 25.5f),  new(2f, 1f), new(116f, 40.1f), Vector2.up, "Bakery"),
        D("Door_Bakery_Exit",  "나가기",   new(116f, 39.5f), new(2f, 1f), new(54f, 24.1f),  Vector2.down, "Village"),
    };

    private static DoorSpec D(string name, string prompt, Vector2 center, Vector2 size, Vector2 dest, Vector2 face, string area) =>
        new() { Name = name, Prompt = prompt, Center = center, Size = size, Destination = dest, Face = face, Area = area };

    [MenuItem("Merry Memory/Setup Doors")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        var boundsRoot = GameObject.Find("Camera Bounds");
        var cm = GameObject.Find("CM Follow Camera");
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (gameplay == null || boundsRoot == null || cm == null || player == null)
        {
            EditorUtility.DisplayDialog("Setup Doors", "Village 씬에서 실행하세요. (Gameplay, Camera Bounds, CM Follow Camera, Player 필요)", "확인");
            return;
        }
        if (gameplay.transform.Find("Doors") != null)
        {
            EditorUtility.DisplayDialog("Setup Doors", "이미 Gameplay/Doors가 있습니다.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Setup Doors");
        int group = Undo.GetCurrentGroup();

        // ── 카메라 경계 ──
        var areas = new Dictionary<string, Collider2D>();
        var village = boundsRoot.transform.Find("Village");
        if (village != null) areas["Village"] = village.GetComponent<Collider2D>();
        foreach (var (name, rect) in Rooms)
        {
            var existing = boundsRoot.transform.Find(name);
            if (existing != null) { areas[name] = existing.GetComponent<Collider2D>(); continue; }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(boundsRoot.transform, false);
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = new[]
            {
                new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax),
            };
            areas[name] = poly;
        }

        // ── 문 ──
        var doorsRoot = new GameObject("Doors");
        Undo.RegisterCreatedObjectUndo(doorsRoot, "Create Doors");
        doorsRoot.transform.SetParent(gameplay.transform, false);

        foreach (var spec in Doors)
        {
            var go = new GameObject(spec.Name);
            go.transform.SetParent(doorsRoot.transform, false);
            go.transform.position = spec.Center;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; // 막는 건 건물·벽이 하고, 이건 조사 감지용
            col.size = spec.Size;

            var dest = new GameObject("Destination");
            dest.transform.SetParent(go.transform, false);
            dest.transform.position = spec.Destination;

            var door = go.AddComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("prompt").stringValue = spec.Prompt;
            // 실내 출구는 방 맨 아래에 있어서, 안내를 위로 띄우면 문 바로 위에 선 플레이어 머리와 겹침 → 문 칸에 붙여 표시
            if (spec.Name.EndsWith("_Exit")) so.FindProperty("promptOffset").vector2Value = new Vector2(0f, 0.1f);
            so.FindProperty("destination").objectReferenceValue = dest.transform;
            so.FindProperty("faceOnArrive").vector2Value = spec.Face;
            so.FindProperty("destinationArea").objectReferenceValue = areas.TryGetValue(spec.Area, out var a) ? a : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 카메라 구역 전환기 ──
        var switcher = cm.GetComponent<CameraAreaSwitcher>();
        if (switcher == null) switcher = Undo.AddComponent<CameraAreaSwitcher>(cm);
        var sso = new SerializedObject(switcher);
        var list = sso.FindProperty("areas");
        var order = new List<Collider2D> { areas.TryGetValue("Village", out var v) ? v : null };
        foreach (var (name, _) in Rooms) order.Add(areas[name]);
        list.arraySize = order.Count;
        for (int i = 0; i < order.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = order[i];
        sso.FindProperty("player").objectReferenceValue = player.transform;
        sso.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(gameplay.scene);
        Selection.activeGameObject = doorsRoot;
        Debug.Log($"문 {Doors.Length}개와 실내 카메라 경계 {Rooms.Length}개를 놓았습니다. 씬을 저장하세요 (Ctrl+S).");
    }
}
