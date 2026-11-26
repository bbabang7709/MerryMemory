using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Ending (5-4)  (Village 씬에서 실행, 다시 실행하면 새로 만듦)
///  - 대사: Dialogues/Ending/ 에 모임·깨달음·배달 세 묶음 (2-5의 Ending.asset은 Ending_Realize로 옮겨 다시 씀)
///  - UICanvas/EndingView/Card: 카드 화면 (조각 칸 5 + 주소 + 한마디 5줄)
///  - Gameplay/Ending/EndingCrowd: 트리 앞에 모인 마을 사람 8명 (평소엔 꺼 둠, 각 사람의 모습을 그대로 본뜸)
///  - Gameplay/Ending/GatherPoint: 주인공이 설 자리
///  - EndingSequence 연결: 조각 1~5, 숨길 것(조각 현황·들고 있는 것 UI, 테스트 NPC)
/// 메뉴 Merry Memory → Setup Credits (5-5): From. 칸 형식의 크레딧 화면 (아래 SetupCredits)
/// </summary>
public static class EndingSetupMenu5
{
    private const string Folder = "Assets/ScriptableObjects/Dialogues/Ending";
    private const string OldEnding = "Assets/ScriptableObjects/Dialogues/Ending.asset";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";

    private static readonly Vector3 GatherPoint = new(32f, 20.6f, 0f);

    // 모인 자리: (원래 사람 이름, 설 자리) — 주인공 양옆으로 반원
    private static readonly (string source, Vector2 pos)[] CrowdLayout =
    {
        ("PostMaster",      new(30.4f, 21.0f)),
        ("Baker",           new(33.6f, 21.0f)),
        ("WorkshopOwner",   new(29.2f, 20.2f)),
        ("BellRinger",      new(34.8f, 20.2f)),
        ("HummingVillager", new(28.4f, 19.2f)),
        ("Kid_RibbonOwner", new(30.9f, 19.3f)),
        ("Kid_Tip",         new(33.1f, 19.3f)),
        ("Kid_Hint",        new(35.6f, 19.2f)),
    };

    private static readonly Color Paper = new(0.97f, 0.94f, 0.86f);
    private static readonly Color Ribbon = new(0.72f, 0.2f, 0.16f);
    private static readonly Color Ink = new(0.2f, 0.18f, 0.25f);
    private static readonly Color Empty = new(0.82f, 0.8f, 0.76f);

    [MenuItem("Merry Memory/Setup Ending (5-4)")]
    private static void Setup()
    {
        var ending = Object.FindFirstObjectByType<EndingSequence>(FindObjectsInactive.Include);
        var view = GameObject.Find("UICanvas")?.transform.Find("EndingView");
        if (ending == null || view == null)
        {
            EditorUtility.DisplayDialog("Setup Ending", "Village 씬에서 실행하세요. (Gameplay/Ending, UICanvas/EndingView 필요)", "확인");
            return;
        }

        // ── 대사 ──
        var gather = Dialogue("Ending_Gather",
            ("우체국장", "다들 모였구나! 자정까지 얼마 안 남았어."),
            ("우체국장", "{이름}{아/야}, 모아 온 조각으로 카드를 완성해 주렴."),
            ("눈 언덕 아이", "빨리빨리! 누구한테 가는 카드인지 궁금해!"));
        var realize = MoveOldEnding("Ending_Realize",
            ("{이름}", "...아. 그래서 다들 그렇게 말한 거구나."),
            ("{이름}", "날 여기까지 데려와 준 거, 너였구나."));
        var deliver = Dialogue("Ending_Deliver",
            ("{이름}", "배달 왔습니다! 우리 마을 모두가 너한테 보내는 카드야."),
            ("{이름}", "같이 와 줘서 고마워. 그럼, 다 같이..."));

        Undo.SetCurrentGroupName("Setup Ending 5-4");
        int group = Undo.GetCurrentGroup();

        // ── 카드 화면 ──
        var old = view.Find("Card");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var cardView = CreateCard(view, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));

        // ── 모인 마을 사람들 ──
        var oldCrowd = ending.transform.Find("EndingCrowd");
        if (oldCrowd != null) Undo.DestroyObjectImmediate(oldCrowd.gameObject);
        var crowd = new GameObject("EndingCrowd");
        Undo.RegisterCreatedObjectUndo(crowd, "Create Crowd");
        crowd.transform.SetParent(ending.transform, false);
        int people = 0;
        foreach (var (source, pos) in CrowdLayout)
        {
            var src = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(r => r.name == source && (r.GetComponent<NpcTalk>() != null || r.GetComponent<HummingNpc>() != null));
            if (src == null) continue;
            var go = new GameObject(source);
            go.transform.SetParent(crowd.transform, false);
            go.transform.position = pos;
            go.transform.localScale = src.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.color = src.color;
            sr.sharedMaterial = src.sharedMaterial;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder;
            people++;
        }
        crowd.SetActive(false);

        // ── 주인공 설 자리 ──
        var oldPoint = ending.transform.Find("GatherPoint");
        if (oldPoint != null) Undo.DestroyObjectImmediate(oldPoint.gameObject);
        var point = new GameObject("GatherPoint").transform;
        Undo.RegisterCreatedObjectUndo(point.gameObject, "Create GatherPoint");
        point.SetParent(ending.transform, false);
        point.position = GatherPoint;

        // ── 연결 ──
        var pieces = AssetDatabase.FindAssets("t:CardPieceData")
            .Select(g => AssetDatabase.LoadAssetAtPath<CardPieceData>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(p => p.number).ToArray();
        var canvas = view.parent;
        var testNpc = GameObject.Find("Test_NPC"); // 2-2 대화 테스트용 사람 (광장에 서 있음)
        var hide = new[] { canvas.Find("CardProgressView"), canvas.Find("CarriedItemView"), testNpc != null ? testNpc.transform : null }
            .Where(t => t != null).Select(t => t.gameObject).ToArray();

        var so = new SerializedObject(ending);
        so.FindProperty("gatherPoint").objectReferenceValue = point;
        so.FindProperty("crowd").objectReferenceValue = crowd;
        Fill(so.FindProperty("hideDuringEnding"), hide);
        so.FindProperty("card").objectReferenceValue = cardView;
        Fill(so.FindProperty("pieces"), pieces);
        so.FindProperty("gatherDialogue").objectReferenceValue = gather;
        so.FindProperty("realizeDialogue").objectReferenceValue = realize;
        so.FindProperty("deliverDialogue").objectReferenceValue = deliver;
        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(ending.gameObject.scene);
        Debug.Log($"엔딩(5-4)을 배치했습니다. 모인 사람 {people}명, 조각 {pieces.Length}개, 숨길 것 {hide.Length}개. 씬을 저장하세요 (Ctrl+S).");
    }

    /// <summary>
    /// 메뉴 Merry Memory → Setup Credits (5-5): UICanvas/EndingView/Credits 를 만들고 EndingSequence에 연결.
    /// 검은 화면에 "From." + 이름 8줄 + 마지막 인사. 제작자 이름은 Credits의 Creator Name 칸에서 바꿉니다.
    /// </summary>
    [MenuItem("Merry Memory/Setup Credits (5-5)")]
    private static void SetupCredits()
    {
        var ending = Object.FindFirstObjectByType<EndingSequence>(FindObjectsInactive.Include);
        var view = GameObject.Find("UICanvas")?.transform.Find("EndingView");
        if (ending == null || view == null)
        {
            EditorUtility.DisplayDialog("Setup Credits", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        Undo.SetCurrentGroupName("Setup Credits 5-5");
        int group = Undo.GetCurrentGroup();
        var old = view.Find("Credits");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var root = UI("Credits", view);
        Stretch(root);
        root.SetAsLastSibling(); // MERRY CHRISTMAS보다도 위
        root.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.1f, 1f);
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        var cream = new Color(0.96f, 0.95f, 0.9f);
        var header = Text(root, "From", "From.", 64, new Color(1f, 0.84f, 0.45f), new Vector2(0, 340), new Vector2(1200, 90), font);
        var lines = new TMP_Text[8];
        for (int i = 0; i < lines.Length; i++)
            lines[i] = Text(root, $"Line_{i + 1}", "", 38, cream, new Vector2(0, 230 - i * 62), new Vector2(1400, 56), font);
        var farewell = Text(root, "Farewell", "", 36, new Color(1f, 0.84f, 0.45f), new Vector2(0, -350), new Vector2(1400, 56), font);
        foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        var credits = root.gameObject.AddComponent<EndingCredits>();
        var so = new SerializedObject(credits);
        so.FindProperty("group").objectReferenceValue = cg;
        so.FindProperty("header").objectReferenceValue = header;
        Fill(so.FindProperty("lines"), lines);
        so.FindProperty("farewell").objectReferenceValue = farewell;
        so.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Credits");

        var eso = new SerializedObject(ending);
        eso.FindProperty("credits").objectReferenceValue = credits;
        eso.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(ending.gameObject.scene);
        Debug.Log("크레딧(5-5)을 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static void Fill(SerializedProperty arr, Object[] items)
    {
        arr.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    // ───────────────────────── 카드 화면 ─────────────────────────

    private static EndingCardView CreateCard(Transform view, TMP_FontAsset font)
    {
        var root = UI("Card", view);
        Stretch(root);
        var dim = view.Find("Dim");
        root.SetSiblingIndex(dim != null ? dim.GetSiblingIndex() + 1 : 0); // 어두운 판 바로 위, 클로즈업 아래
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Box(root, "Border", new Vector2(0, 30), new Vector2(1236, 676), Ribbon);
        var paper = Box(root, "Paper", new Vector2(0, 30), new Vector2(1200, 640), Paper);
        // 조립 칸 5개 (+ 카드 앞면 제목: 펼치면 칸과 함께 사라지고 그 자리에 주소가 올라옴)
        var slots = UI("Slots", paper);
        Stretch(slots);
        var slotsGroup = slots.gameObject.AddComponent<CanvasGroup>();
        Text(slots, "Title", "마을 모두가 함께 만든 크리스마스 카드", 34, Ink, new Vector2(0, 268), new Vector2(1100, 56), font);
        var images = new Image[5];
        var labels = new TMP_Text[5];
        for (int i = 0; i < 5; i++)
        {
            float x = -460 + i * 230;
            var slot = Box(slots, $"Slot_{i + 1}", new Vector2(x, 80), new Vector2(190, 190), Empty);
            images[i] = slot.GetComponent<Image>();
            Text(slot, "Number", $"{i + 1}", 30, new Color(1f, 1f, 1f, 0.9f), new Vector2(-70, 70), new Vector2(40, 40), font);
            labels[i] = Text(slots, $"Label_{i + 1}", "", 30, Ink, new Vector2(x, -40), new Vector2(220, 44), font);
        }
        var hint = Text(paper, "Hint", "", 30, new Color(0.45f, 0.4f, 0.4f), new Vector2(0, -110), new Vector2(1100, 44), font);
        var address = Text(paper, "Address", "", 52, Ink, new Vector2(0, -210), new Vector2(1100, 70), font);

        // 펼친 카드의 한마디 5줄
        var messages = UI("Messages", paper);
        Stretch(messages);
        var messagesGroup = messages.gameObject.AddComponent<CanvasGroup>();
        var lines = new TMP_Text[5];
        for (int i = 0; i < 5; i++)
        {
            lines[i] = Text(messages, $"Message_{i + 1}", "", 29, Ink, new Vector2(0, 160 - i * 92), new Vector2(1100, 86), font);
            lines[i].alignment = TextAlignmentOptions.MidlineLeft;
            lines[i].textWrappingMode = TextWrappingModes.Normal;
        }

        foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        var cardView = root.gameObject.AddComponent<EndingCardView>();
        var so = new SerializedObject(cardView);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("slotsGroup").objectReferenceValue = slotsGroup;
        var im = so.FindProperty("slotImages"); im.arraySize = 5;
        var lb = so.FindProperty("slotLabels"); lb.arraySize = 5;
        var ms = so.FindProperty("messageTexts"); ms.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            im.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            lb.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            ms.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
        }
        so.FindProperty("hintText").objectReferenceValue = hint;
        so.FindProperty("addressText").objectReferenceValue = address;
        so.FindProperty("messagesGroup").objectReferenceValue = messagesGroup;
        var colors = so.FindProperty("pieceColors");
        var palette = new[]
        {
            new Color(0.85f, 0.35f, 0.3f),  // 우표
            new Color(0.3f, 0.65f, 0.4f),   // 팝업 트리
            new Color(0.95f, 0.78f, 0.3f),  // 멜로디 칩
            new Color(0.85f, 0.3f, 0.5f),   // 리본
            new Color(0.55f, 0.7f, 0.92f),  // 봉투
        };
        colors.arraySize = palette.Length;
        for (int i = 0; i < palette.Length; i++) colors.GetArrayElementAtIndex(i).colorValue = palette[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Ending Card");
        return cardView;
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
        return t;
    }

    // ───────────────────────── 대사 ─────────────────────────

    private static DialogueData Dialogue(string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(Folder);
        string path = $"{Folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing; // 직접 고친 대사는 덮어쓰지 않음

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    /// <summary>
    /// 2-5의 Ending.asset(주소 + 깨달음 + 배달이 한데 묶인 대사)을 Ending/Ending_Realize로 옮기고(GUID 유지)
    /// 깨달음 두 줄만 남깁니다. 주소는 카드 화면이, 배달은 Ending_Deliver가 맡습니다.
    /// </summary>
    private static DialogueData MoveOldEnding(string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        string path = $"{Folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<DialogueData>(path) == null && AssetDatabase.LoadAssetAtPath<DialogueData>(OldEnding) != null)
        {
            string error = AssetDatabase.MoveAsset(OldEnding, path);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning(error);
            var moved = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            moved.lines.Clear();
            foreach (var (speaker, text) in lines) moved.lines.Add(new DialogueLine { speaker = speaker, text = text });
            EditorUtility.SetDirty(moved);
            AssetDatabase.SaveAssets();
            return moved;
        }
        return Dialogue(fileName, lines);
    }
}
