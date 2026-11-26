using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Toy Workshop (4-2)  (Village 씬에서 실행)
///  - 공방 실내(1-7 청사진 (96,0) 12×9): 공방 주인, 작업대, 부품 3개, 팝업 장치 퍼즐 + 팝업 트리 보상
///  - UICanvas/PopupAssemblyView: 조립 화면 (부품 버튼 3, 칸 3, 메시지)
///  - 3-3 임시 스위치 퍼즐(같은 팝업 트리 조각을 줌)을 씬에서 치움
///  - 청사진의 공방 주인·조각 2·부품 표시는 진짜 오브젝트와 겹치므로 끔
/// 실내 좌표 (lx, ly) → 월드 칸 (96 + lx, 47 − ly)
/// </summary>
public static class ToyWorkshopSetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues/ToyWorkshop";
    private const string CommonFolder = "Assets/ScriptableObjects/Dialogues/Common";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string CircleSprite = "Assets/Art/Blueprint/Textures/Block_Circle.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";
    private const string PopupPiece = "Assets/ScriptableObjects/CardPieces/Piece_2_PopupTree.asset";

    private static Vector3 Cell(int lx, int ly) => new(96 + lx + 0.5f, 47 - ly + 0.5f, 0f);
    private static Vector3 Feet(int lx, int ly) => new(96 + lx + 0.5f, 47 - ly + 0.1f, 0f);

    // 부품: tag, 이름, 위치 (MAP 7.2)
    private static readonly (string tag, string label, int lx, int ly, Color color)[] Parts =
    {
        ("popup_gear",   "톱니바퀴",     1, 3, new Color(0.75f, 0.78f, 0.85f)),
        ("popup_spring", "태엽 스프링",  10, 3, new Color(0.95f, 0.75f, 0.45f)),
        ("popup_board",  "종이 트리 판",  9, 6, new Color(0.55f, 0.9f, 0.6f)),
    };

    private static readonly Color Night = new(0.08f, 0.13f, 0.24f, 1f);
    private static readonly Color Cream = new(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color Red = new(0.72f, 0.2f, 0.16f, 1f);

    [MenuItem("Merry Memory/Setup Toy Workshop (4-2)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        var canvas = GameObject.Find("UICanvas");
        if (gameplay == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Toy Workshop", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (gameplay.transform.Find("ToyWorkshop") != null)
        {
            EditorUtility.DisplayDialog("Setup Toy Workshop", "이미 Gameplay/ToyWorkshop이 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var ownerFirst = Dialogue(DialogueFolder, "Owner_First",
            ("공방 주인", "오, 우체국 일을 돕는 친구로구나! 마침 잘 왔어."),
            ("공방 주인", "카드에 넣을 팝업 트리 장치가 아까 바람에 날려 와서 망가져 버렸지 뭐야."),
            ("공방 주인", "부품 세 개가 공방 여기저기로 굴러갔어. 톱니바퀴, 태엽 스프링, 종이 트리 판."),
            ("공방 주인", "찾아서 작업대에 올려 주면 같이 조립해 보자."),
            ("공방 주인", "조립 순서? 톱니가 돌아야 태엽이 감기고, 태엽이 풀려야 트리가 솟아오르지."));
        var ownerRepeat = Dialogue(DialogueFolder, "Owner_Repeat",
            ("공방 주인", "톱니가 돌아야 태엽이 감기고, 태엽이 풀려야 트리가 솟아오른단다."));
        var wrongItem = Dialogue(DialogueFolder, "Workbench_WrongItem",
            ("{이름}", "이건 이 장치 부품이 아니야."));
        var alreadyHolding = Dialogue(CommonFolder, "Carry_AlreadyHolding",
            ("{이름}", "이미 다른 걸 들고 있어. 먼저 내려놓을 곳을 찾자."));

        Undo.SetCurrentGroupName("Setup Toy Workshop");
        int group = Undo.GetCurrentGroup();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var root = new GameObject("ToyWorkshop");
        Undo.RegisterCreatedObjectUndo(root, "Create ToyWorkshop");
        root.transform.SetParent(gameplay.transform, false);
        root.transform.position = Cell(5, 3);

        // ── 조립 화면 UI ──
        EnsureEventSystem(); // 없으면 버튼이 클릭·Enter를 전혀 못 받음
        var view = CreateAssemblyView(canvas.transform, font);

        // ── 퍼즐 + 보상 ──
        var puzzle = root.AddComponent<PopupAssemblyPuzzle>();
        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(PopupPiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        // ── 부품 3개 ──
        var pickups = new CarryPickup[Parts.Length];
        var box = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);
        for (int i = 0; i < Parts.Length; i++)
        {
            var p = Parts[i];
            var go = new GameObject($"Part_{p.tag}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = Cell(p.lx, p.ly);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = box;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.5f, 0.5f);
            sr.color = p.color;
            sr.sortingOrder = -4;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            var pickup = go.AddComponent<CarryPickup>();
            var so = new SerializedObject(pickup);
            var item = so.FindProperty("item");
            item.FindPropertyRelative("id").stringValue = p.tag;
            item.FindPropertyRelative("label").stringValue = p.label;
            item.FindPropertyRelative("tag").stringValue = p.tag;
            so.FindProperty("alreadyHoldingDialogue").objectReferenceValue = alreadyHolding;
            so.ApplyModifiedPropertiesWithoutUndo();
            pickups[i] = pickup;
        }

        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = FlagKeys.PopupAssembly;
        var arr = pso.FindProperty("partPickups");
        arr.arraySize = pickups.Length;
        for (int i = 0; i < pickups.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pickups[i];
        pso.FindProperty("view").objectReferenceValue = view;
        pso.ApplyModifiedPropertiesWithoutUndo();

        // ── 작업대 (청사진 작업대 (5,3) 3×1 위에 겹치는 조사용 트리거) ──
        var bench = new GameObject("Workbench");
        bench.transform.SetParent(root.transform, false);
        bench.transform.position = new Vector3(96 + 5 + 1.5f, 47 - 3 + 0.5f, 0f);
        var bcol = bench.AddComponent<BoxCollider2D>();
        bcol.isTrigger = true;
        bcol.size = new Vector2(3f, 1f);
        var wb = bench.AddComponent<Workbench>();
        var wso = new SerializedObject(wb);
        wso.FindProperty("puzzle").objectReferenceValue = puzzle;
        wso.FindProperty("wrongItemDialogue").objectReferenceValue = wrongItem;
        wso.ApplyModifiedPropertiesWithoutUndo();

        // ── 공방 주인 (8,4) ──
        var owner = new GameObject("WorkshopOwner");
        owner.transform.SetParent(root.transform, false);
        owner.transform.position = Feet(8, 4);
        var osr = owner.AddComponent<SpriteRenderer>();
        osr.sprite = AssetDatabase.LoadAllAssetsAtPath(MaleIdle).OfType<Sprite>().Where(s => s.name.Contains("_Front_")).OrderBy(s => s.name).FirstOrDefault();
        osr.color = new Color(0.7f, 0.95f, 0.75f); // 임시: 다른 사람과 구분되는 색
        osr.spriteSortPoint = SpriteSortPoint.Pivot;
        var ocol = owner.AddComponent<CapsuleCollider2D>();
        ocol.direction = CapsuleDirection2D.Horizontal;
        ocol.size = new Vector2(0.6f, 0.3f);
        ocol.offset = new Vector2(0f, 0.15f);
        var npc = owner.AddComponent<NpcTalk>();
        var nso = new SerializedObject(npc);
        nso.FindProperty("firstDialogue").objectReferenceValue = ownerFirst;
        nso.FindProperty("repeatDialogue").objectReferenceValue = ownerRepeat;
        nso.FindProperty("talkedFlag").stringValue = FlagKeys.WorkshopOwnerTalked;
        nso.ApplyModifiedPropertiesWithoutUndo();

        // ── 정리: 임시 스위치 퍼즐, 겹치는 청사진 표시 ──
        var test = gameplay.transform.Find("TestSwitchPuzzle");
        if (test != null) Undo.DestroyObjectImmediate(test.gameObject);

        var bpRoom = GameObject.Find("Village Blueprint/Interiors/장난감 공방 실내");
        if (bpRoom != null)
        {
            foreach (Transform t in bpRoom.transform)
            {
                if (t.name.EndsWith(" 공방 주인") || t.name.EndsWith(" 조각 2") || t.name.EndsWith(" 톱니")
                    || t.name.EndsWith(" 태엽") || t.name.EndsWith(" 트리 판"))
                {
                    Undo.RecordObject(t.gameObject, "Hide Blueprint");
                    t.gameObject.SetActive(false);
                }
            }
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("장난감 공방(4-2)을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    // ───────────────────────── 조립 화면 UI ─────────────────────────

    /// <summary>uGUI 버튼은 씬에 EventSystem이 있어야 입력을 받습니다. (새 Input System용 모듈)</summary>
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null) return;
        var es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static PopupAssemblyView CreateAssemblyView(Transform canvas, TMP_FontAsset font)
    {
        var root = UI("PopupAssemblyView", canvas);
        Stretch(root);
        var dialogueView = canvas.Find("DialogueView");
        if (dialogueView != null) root.SetSiblingIndex(dialogueView.GetSiblingIndex());

        var panel = UI("Panel", root);
        Stretch(panel);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var dialog = Box(panel, "Dialog", Vector2.zero, new Vector2(1100, 600), Night);
        Text(dialog, "Title", "팝업 트리 장치 조립", 48, Cream, new Vector2(0, 240), new Vector2(1000, 70), font);

        // 칸 3개
        var slots = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = Box(dialog, $"Slot_{i + 1}", new Vector2(-330 + i * 330, 110), new Vector2(290, 100), new Color(1f, 1f, 1f, 0.12f));
            slots[i] = Text(slot, "Label", $"{i + 1}", 34, Cream, Vector2.zero, new Vector2(270, 90), font);
            if (i < 2) Text(dialog, $"Arrow_{i + 1}", ">", 40, new Color(1f, 1f, 1f, 0.5f), new Vector2(-165 + i * 330, 110), new Vector2(40, 60), font);
        }

        var message = Text(dialog, "Message", "부품을 맞는 순서대로 끼워 보자.", 32, new Color(1f, 0.84f, 0.45f), new Vector2(0, 10), new Vector2(1000, 60), font);

        // 부품 버튼 3개
        string[] labels = { "톱니바퀴", "태엽 스프링", "종이 트리 판" };
        var buttons = new Button[3];
        for (int i = 0; i < 3; i++)
            buttons[i] = MakeButton(dialog, $"Part_{i + 1}", labels[i], new Vector2(-330 + i * 330, -110), new Vector2(290, 90), font);

        Text(dialog, "Hint", "방향키로 고르고 Enter (또는 클릭)    Esc: 닫기", 26, new Color(1f, 1f, 1f, 0.55f), new Vector2(0, -240), new Vector2(1000, 44), font);

        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            if (g.GetComponent<Button>() == null) g.raycastTarget = g.gameObject == panel.gameObject; // 패널은 뒤쪽 클릭을 막음

        var view = root.gameObject.AddComponent<PopupAssemblyView>();
        var so = new SerializedObject(view);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        var b = so.FindProperty("partButtons"); b.arraySize = 3;
        var s = so.FindProperty("slotTexts"); s.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            b.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            s.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        so.FindProperty("messageText").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create PopupAssemblyView");
        return view;
    }

    private static RectTransform UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var rt = UI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box, TMP_FontAsset font)
    {
        var rt = UI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, TMP_FontAsset font)
    {
        var rt = Box(parent, name, pos, size, Red);
        var button = rt.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.78f, 0.78f, 0.78f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        button.colors = colors;
        Text(rt, "Label", label, 34, Cream, Vector2.zero, size, font);
        return button;
    }

    private static DialogueData Dialogue(string folder, string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(folder);
        string path = $"{folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }
}
