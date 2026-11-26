using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Snow Hill (4-4)  (Village 씬에서 실행)
///  - 얼어붙은 연못 MAP (5,4) 10×7: 미끄럼 퍼즐 + 리본 보상, 바위 6개, 연못 막이(일반 이동으로는 못 들어감)
///  - 시작 칸 MAP (10,11): 얼음에 올라서기 (IceSlideMover), 다시 하기 안내판 MAP (12,11)
///  - 리본 나무 MAP (9,2)에 걸린 리본, 눈 언덕 아이 3명 (MAP N1~N3)
///  - UICanvas/IceSlideHint: 얼음 위 조작 안내
///  - 청사진의 예시 바위(PK1~5)·시작·안내판·아이 표시는 끔
/// 바위 배치는 MAP 예시를 다듬은 것: 5번 이동(위 왼 위 오 위)으로 풀림, 구석은 막다른 곳 → R로 다시
/// 좌표는 모두 월드 칸 (MAP (x, y) → (x, 47 − y))
/// </summary>
public static class SnowHillSetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues/SnowHill";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string CircleSprite = "Assets/Art/Blueprint/Textures/Block_Circle.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";
    private const string RibbonPiece = "Assets/ScriptableObjects/CardPieces/Piece_4_Ribbon.asset";

    private static readonly RectInt Pond = new(5, 37, 10, 7);
    private static readonly Vector2Int Start = new(10, 36);
    private static readonly Vector2Int Goal = new(9, 43);
    // MAP (5,8) (7,9) (8,7) (10,6) (10,8) (14,9)
    private static readonly Vector2Int[] Rocks =
    {
        new(5, 39), new(7, 38), new(8, 40), new(10, 41), new(10, 39), new(14, 38),
    };

    private static Vector3 Center(Vector2Int c) => new(c.x + 0.5f, c.y + 0.5f, 0f);

    [MenuItem("Merry Memory/Setup Snow Hill (4-4)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        var canvas = GameObject.Find("UICanvas");
        if (gameplay == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Snow Hill", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (gameplay.transform.Find("SnowHill") != null)
        {
            EditorUtility.DisplayDialog("Setup Snow Hill", "이미 Gameplay/SnowHill이 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var ownerFirst = Dialogue("Kid_Owner_First",
            ("눈 언덕 아이", "흑... 내 리본이 바람에 날려서 저 나무에 걸렸어."),
            ("눈 언덕 아이", "카드에 달 리본이었는데, 연못이 꽁꽁 얼어서 나무까지 갈 수가 없어."),
            ("눈 언덕 아이", "얼음 위를 잘 미끄러져 가면 닿을 수 있을 텐데..."));
        var ownerRepeat = Dialogue("Kid_Owner_Repeat",
            ("눈 언덕 아이", "리본이 걸린 나무는 연못 위쪽이야. 미끄러져서 나무 바로 밑에 딱 멈춰야 해."));
        var tip = Dialogue("Kid_Tip",
            ("눈 언덕 아이", "얼음 위에선 한 번 밀면 뭔가에 부딪칠 때까지 안 멈춰!"),
            ("눈 언덕 아이", "바위를 멈춤판으로 쓰는 거야. 바위 바로 앞에서 딱 서거든."));
        var hintTalk = Dialogue("Kid_Hint",
            ("눈 언덕 아이", "막혔어? 얼음 위에서 R을 누르면 처음 자리로 돌아와."),
            ("눈 언덕 아이", "나는 처음에 위로 갔다가 왼쪽으로 가 봤어. 그다음은 비밀!"));
        var sign = Dialogue("ResetSign",
            ("", "얼음 연못 이용 안내"),
            ("", "방향키로 미끄러지기. 막히면 R을 눌러 처음 자리로."));
        var reached = Dialogue("Ribbon_Reached",
            ("", "나무 바로 밑이다. 손을 뻗으니 리본에 닿았다!"),
            ("", "걸린 리본을 조심스럽게 풀었다."));

        Undo.SetCurrentGroupName("Setup Snow Hill");
        int group = Undo.GetCurrentGroup();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var box = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);
        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSprite);

        var root = new GameObject("SnowHill");
        Undo.RegisterCreatedObjectUndo(root, "Create SnowHill");
        root.transform.SetParent(gameplay.transform, false);
        root.transform.position = new Vector3(Pond.center.x, Pond.center.y, 0f);

        // ── 리본 (나무 MAP (9,2) 2×2 = 월드 x 9~10, y 44~45) ──
        var ribbon = Block(root.transform, "Ribbon", new Vector3(9.7f, 44.6f, 0f), box, new Vector2(0.45f, 0.35f), new Color(0.9f, 0.2f, 0.25f), -3);

        // ── 퍼즐 + 보상 ──
        var puzzle = root.AddComponent<IceSlidePuzzle>();
        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = FlagKeys.IceSlide;
        pso.FindProperty("pond").rectIntValue = Pond;
        pso.FindProperty("startCell").vector2IntValue = Start;
        pso.FindProperty("goalCell").vector2IntValue = Goal;
        var rocks = pso.FindProperty("rocks");
        rocks.arraySize = Rocks.Length;
        for (int i = 0; i < Rocks.Length; i++) rocks.GetArrayElementAtIndex(i).vector2IntValue = Rocks[i];
        pso.FindProperty("ribbon").objectReferenceValue = ribbon;
        pso.ApplyModifiedPropertiesWithoutUndo();

        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(RibbonPiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        // ── 연못 막이: 일반 걸음으로는 얼음에 못 들어감 (얼음 위에서는 물리를 끄므로 상관없음) ──
        var blocker = new GameObject("PondBlocker");
        blocker.transform.SetParent(root.transform, false);
        blocker.transform.position = root.transform.position;
        blocker.AddComponent<BoxCollider2D>().size = new Vector2(Pond.width, Pond.height);

        // ── 바위 ──
        foreach (var r in Rocks)
            Block(root.transform, $"Rock_{r.x}_{r.y}", Center(r), circle, new Vector2(0.85f, 0.85f), new Color(0.55f, 0.6f, 0.68f), -4);

        // ── UI 안내 ──
        var hint = CreateHint(canvas.transform, font);

        // ── 시작 칸: 얼음에 올라서기 ──
        var entry = new GameObject("IceEntry");
        entry.transform.SetParent(root.transform, false);
        entry.transform.position = Center(Start);
        var ecol = entry.AddComponent<BoxCollider2D>();
        ecol.isTrigger = true;
        ecol.size = new Vector2(0.9f, 0.9f);
        var mover = entry.AddComponent<IceSlideMover>();
        var mso = new SerializedObject(mover);
        mso.FindProperty("puzzle").objectReferenceValue = puzzle;
        mso.FindProperty("hint").objectReferenceValue = hint;
        mso.FindProperty("reachedDialogue").objectReferenceValue = reached;
        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── 다시 하기 안내판 MAP (12,11) ──
        var signGo = Block(root.transform, "ResetSign", Center(new Vector2Int(12, 36)), box, new Vector2(0.7f, 0.5f), new Color(0.75f, 0.55f, 0.35f), -4);
        var scol = signGo.AddComponent<BoxCollider2D>();
        scol.isTrigger = true;
        scol.size = new Vector2(0.9f, 0.9f);
        var insp = signGo.AddComponent<Inspectable>();
        var iso = new SerializedObject(insp);
        iso.FindProperty("dialogue").objectReferenceValue = sign;
        iso.FindProperty("promptOffset").vector2Value = new Vector2(0f, 0.7f);
        iso.ApplyModifiedPropertiesWithoutUndo();

        // ── 눈 언덕 아이들 ──
        Kid(root.transform, "Kid_RibbonOwner", new Vector3(17.5f, 41.1f), new Color(1f, 0.75f, 0.8f), ownerFirst, ownerRepeat);
        Kid(root.transform, "Kid_Tip", new Vector3(18.5f, 39.1f), new Color(0.75f, 0.9f, 1f), tip, null);
        Kid(root.transform, "Kid_Hint", new Vector3(16.5f, 38.1f), new Color(0.85f, 1f, 0.75f), hintTalk, null);

        HideBlueprintMarkers();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("눈 언덕(4-4)을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static GameObject Block(Transform parent, string name, Vector3 pos, Sprite sprite, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>임시 아이: 플레이어 앞모습을 작게 + 색 (2단계 아트 전까지)</summary>
    private static void Kid(Transform parent, string name, Vector3 feet, Color tint, DialogueData first, DialogueData repeat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath(MaleIdle).OfType<Sprite>()
            .Where(s => s.name.Contains("_Front_")).OrderBy(s => s.name).FirstOrDefault();
        sr.color = tint;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = new Vector2(0.6f, 0.3f);
        col.offset = new Vector2(0f, 0.15f);
        var npc = go.AddComponent<NpcTalk>();
        var so = new SerializedObject(npc);
        so.FindProperty("firstDialogue").objectReferenceValue = first;
        so.FindProperty("repeatDialogue").objectReferenceValue = repeat;
        so.FindProperty("promptOffset").vector2Value = new Vector2(0f, 1.35f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateHint(Transform canvas, TMP_FontAsset font)
    {
        var go = new GameObject("IceSlideHint", typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(canvas, false);
        var dialogueView = canvas.Find("DialogueView");
        if (dialogueView != null) go.transform.SetSiblingIndex(dialogueView.GetSiblingIndex());
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(820f, 64f);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.13f, 0.24f, 0.85f);
        bg.raycastTarget = false;

        var label = new GameObject("Label", typeof(RectTransform));
        label.layer = go.layer;
        label.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = label.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = "방향키: 미끄러지기      R: 처음 자리로";
        t.fontSize = 30;
        t.color = new Color(0.96f, 0.95f, 0.9f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        go.SetActive(false);
        Undo.RegisterCreatedObjectUndo(go, "Create IceSlideHint");
        return go;
    }

    private static void HideBlueprintMarkers()
    {
        var bp = GameObject.Find("Village Blueprint");
        if (bp == null) return;
        var ids = new HashSet<string> { "PK1", "PK2", "PK3", "PK4", "PK5", "PS", "PR", "N1", "N2", "N3" };
        foreach (var info in bp.GetComponentsInChildren<BlueprintInfo>(true))
        {
            if (!ids.Contains(info.id)) continue;
            Undo.RecordObject(info.gameObject, "Hide Blueprint");
            info.gameObject.SetActive(false);
        }
    }

    private static DialogueData Dialogue(string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(DialogueFolder);
        string path = $"{DialogueFolder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }
}
