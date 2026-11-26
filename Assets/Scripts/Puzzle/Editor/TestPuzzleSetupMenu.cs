using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Setup Test Switch Puzzle  (Village 씬에서 실행, 로드맵 3-3)
/// 남쪽 큰길에 스위치 두 개짜리 임시 퍼즐을 놓고, 풀면 팝업 트리 조각(2번)을 주도록 연결합니다.
/// 4단계에서 진짜 공방 퍼즐이 생기면 지웁니다.
/// </summary>
public static class TestPuzzleSetupMenu
{
    private const string CircleSprite = "Assets/Art/Blueprint/Textures/Block_Circle.png";
    private const string RewardPiece = "Assets/ScriptableObjects/CardPieces/Piece_2_PopupTree.asset";

    [MenuItem("Merry Memory/Setup Test Switch Puzzle")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        if (gameplay == null)
        {
            EditorUtility.DisplayDialog("Setup Test Switch Puzzle", "Village 씬의 Gameplay 오브젝트가 없습니다. (2-5 Setup을 먼저)", "확인");
            return;
        }
        if (gameplay.transform.Find("TestSwitchPuzzle") != null)
        {
            EditorUtility.DisplayDialog("Setup Test Switch Puzzle", "이미 TestSwitchPuzzle이 있습니다.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Setup Test Switch Puzzle");
        int group = Undo.GetCurrentGroup();

        // MAP.md 남쪽 큰길 (30~33, 31~45). 우체국 앞에서 위로 조금 올라간 자리
        var root = new GameObject("TestSwitchPuzzle");
        Undo.RegisterCreatedObjectUndo(root, "Create TestSwitchPuzzle");
        root.transform.SetParent(gameplay.transform);
        root.transform.position = new Vector3(32f, 12.5f, 0f);

        var a = CreateSwitch(root.transform, "Switch_A", new Vector3(30.5f, 12.5f, 0f));
        var b = CreateSwitch(root.transform, "Switch_B", new Vector3(33.5f, 12.5f, 0f));

        var puzzle = root.AddComponent<SwitchPuzzle>();
        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = "switch_test";
        var arr = pso.FindProperty("switches");
        arr.arraySize = 2;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = a;
        arr.GetArrayElementAtIndex(1).objectReferenceValue = b;
        pso.ApplyModifiedPropertiesWithoutUndo();

        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(RewardPiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("임시 스위치 퍼즐을 놓았습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static PuzzleSwitch CreateSwitch(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSprite);
        sr.sortingOrder = -4; // 청사진 블록과 같은 높이 (플레이어보다 아래)
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var col = go.AddComponent<BoxCollider2D>(); // 막힘 (스위치 위로 못 지나감)
        col.size = Vector2.one;

        var sw = go.AddComponent<PuzzleSwitch>();
        var so = new SerializedObject(sw);
        so.FindProperty("body").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();
        return sw;
    }
}
