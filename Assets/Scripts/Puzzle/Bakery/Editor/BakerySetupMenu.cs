using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Bakery (4-5)  (Village 씬에서 실행)
///  - 빵집 실내(1-7 청사진 (110,0) 12×9): 빵집 사장, 작업대 + 번진 봉투, 주소 복원 퍼즐 + 봉투 조각 보상
///  - 마을 바깥 글자 스티커 3장: F1 광장 게시판 (36,17) / F2 나무 다리 (48,41) / F3 입구 아치 (35,44)
///  - UICanvas/AddressRestoreView: 주소 맞추기 화면
///  - 청사진의 빵집 사장·조각 5·글자 조각 표시는 끔
/// 실내 좌표 (lx, ly) → 월드 칸 (110 + lx, 47 − ly) / 바깥 MAP (x, y) → (x, 47 − y)
/// </summary>
public static class BakerySetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues/Bakery";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";
    private const string EnvelopePiece = "Assets/ScriptableObjects/CardPieces/Piece_5_Envelope.asset";

    private static Vector3 Room(float lx, float ly) => new(110 + lx + 0.5f, 47 - ly + 0.5f, 0f);
    private static Vector3 Map(int x, int y) => new(x + 0.5f, 47 - y + 0.5f, 0f);

    // 스티커: id, 글자, MAP 위치, 주울 때 첫 줄 (정답 칸 순서와 일부러 다르게 섞음)
    private static readonly (string id, string letter, int x, int y, string found)[] Fragments =
    {
        ("f1", "머", 36, 17, "게시판 전단 귀퉁이에 글자 스티커가 붙어 있다."),
        ("f2", "게", 48, 41, "나무 다리 난간에 글자 스티커가 걸려 있다."),
        ("f3", "너", 35, 44, "아치 옆 눈 속에 글자 스티커가 반쯤 묻혀 있다."),
    };

    private static readonly Color Night = new(0.08f, 0.13f, 0.24f, 1f);
    private static readonly Color Cream = new(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color Red = new(0.72f, 0.2f, 0.16f, 1f);
    private static readonly Color Smudge = new(0.32f, 0.3f, 0.38f, 1f);

    [MenuItem("Merry Memory/Setup Bakery (4-5)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        var canvas = GameObject.Find("UICanvas");
        if (gameplay == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Bakery", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (gameplay.transform.Find("Bakery") != null)
        {
            EditorUtility.DisplayDialog("Setup Bakery", "이미 Gameplay/Bakery가 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var bakerFirst = Dialogue("Baker_First",
            ("빵집 사장", "어서 와요! 우체국에서 왔구나. 마침 잘 왔어."),
            ("빵집 사장", "마을 카드 봉투를 내가 맡아 두고 있었거든. 주소는 아이들이 글자 스티커로 예쁘게 붙여 줬고."),
            ("빵집 사장", "그런데 오븐 김에 몇 글자는 번져 버리고, 스티커 세 장은 바람에 날아가 버렸지 뭐야."),
            ("빵집 사장", "광장 게시판, 개울 나무 다리, 마을 입구 아치 쪽을 찾아보면 있을 거야."),
            ("빵집 사장", "세 장 다 모아 오면 저기 작업대에서 봉투 주소를 맞춰 보자고."));
        var bakerRepeat = Dialogue("Baker_Repeat",
            ("빵집 사장", "스티커는 광장 게시판, 개울 나무 다리, 마을 입구 아치 쪽에 있을 거야."),
            ("빵집 사장", "주소는 보통 '~에게'로 끝나잖아? 그것부터 맞춰 봐."));

        Undo.SetCurrentGroupName("Setup Bakery");
        int group = Undo.GetCurrentGroup();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var box = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);

        var root = new GameObject("Bakery");
        Undo.RegisterCreatedObjectUndo(root, "Create Bakery");
        root.transform.SetParent(gameplay.transform, false);
        root.transform.position = Room(8, 6); // 작업대 (7,6) 3×1의 가운데

        // ── 화면 ──
        EnsureEventSystem();
        var view = CreateView(canvas.transform, font);

        // ── 작업대 위 번진 봉투 ──
        var envelope = Block(root.transform, "Envelope", Room(8, 6) + new Vector3(0f, 0.1f), box, new Vector2(0.7f, 0.45f), new Color(0.93f, 0.88f, 0.78f), -3);

        // ── 퍼즐 + 보상 ──
        var puzzle = root.AddComponent<AddressRestorePuzzle>();
        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(EnvelopePiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        // ── 글자 스티커 3장 (바깥) ──
        var pickups = new List<LetterFragmentPickup>();
        foreach (var f in Fragments)
        {
            var go = Block(root.transform, $"LetterFragment_{f.id}", Map(f.x, f.y), box, new Vector2(0.4f, 0.4f), new Color(1f, 0.97f, 0.75f), -3);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f); // 난간·게시판 옆이라 조금 넉넉하게
            var pickup = go.AddComponent<LetterFragmentPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("fragmentId").stringValue = f.id;
            so.FindProperty("letter").stringValue = f.letter;
            so.FindProperty("foundText").stringValue = f.found;
            so.FindProperty("puzzle").objectReferenceValue = puzzle;
            so.ApplyModifiedPropertiesWithoutUndo();
            pickups.Add(pickup);
        }

        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = FlagKeys.AddressRestore;
        var arr = pso.FindProperty("fragments");
        arr.arraySize = pickups.Count;
        for (int i = 0; i < pickups.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pickups[i];
        pso.FindProperty("envelope").objectReferenceValue = envelope;
        pso.FindProperty("view").objectReferenceValue = view;
        pso.ApplyModifiedPropertiesWithoutUndo();

        // ── 작업대 (청사진 작업대 위에 겹치는 조사용 트리거) ──
        var desk = new GameObject("EnvelopeDesk");
        desk.transform.SetParent(root.transform, false);
        desk.transform.position = Room(8, 6);
        var dcol = desk.AddComponent<BoxCollider2D>();
        dcol.isTrigger = true;
        dcol.size = new Vector2(3f, 1f);
        var ed = desk.AddComponent<EnvelopeDesk>();
        var eso = new SerializedObject(ed);
        eso.FindProperty("puzzle").objectReferenceValue = puzzle;
        eso.ApplyModifiedPropertiesWithoutUndo();

        // ── 빵집 사장 (4,2): 계산대 (2,3) 뒤라서 계산대 앞까지 닿는 TalkArea ──
        var baker = new GameObject("Baker");
        baker.transform.SetParent(root.transform, false);
        baker.transform.position = new Vector3(114.5f, 45.1f, 0f);
        var bsr = baker.AddComponent<SpriteRenderer>();
        bsr.sprite = AssetDatabase.LoadAllAssetsAtPath(MaleIdle).OfType<Sprite>()
            .Where(s => s.name.Contains("_Front_")).OrderBy(s => s.name).FirstOrDefault();
        bsr.color = new Color(1f, 0.9f, 0.7f);
        bsr.spriteSortPoint = SpriteSortPoint.Pivot;
        var bcol = baker.AddComponent<CapsuleCollider2D>();
        bcol.direction = CapsuleDirection2D.Horizontal;
        bcol.size = new Vector2(0.6f, 0.3f);
        bcol.offset = new Vector2(0f, 0.15f);
        var npc = baker.AddComponent<NpcTalk>();
        var nso = new SerializedObject(npc);
        nso.FindProperty("firstDialogue").objectReferenceValue = bakerFirst;
        nso.FindProperty("repeatDialogue").objectReferenceValue = bakerRepeat;
        nso.FindProperty("talkedFlag").stringValue = FlagKeys.BakerTalked;
        nso.ApplyModifiedPropertiesWithoutUndo();
        var area = new GameObject("TalkArea");
        area.transform.SetParent(baker.transform, false);
        var acol = area.AddComponent<BoxCollider2D>();
        acol.isTrigger = true;
        acol.offset = new Vector2(0f, -0.9f);
        acol.size = new Vector2(1f, 2f);

        HideBlueprintMarkers();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("빵집(4-5)을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static void HideBlueprintMarkers()
    {
        var bp = GameObject.Find("Village Blueprint");
        if (bp == null) return;
        var ids = new HashSet<string> { "F1", "F2", "F3" };
        foreach (var info in bp.GetComponentsInChildren<BlueprintInfo>(true))
        {
            if (!ids.Contains(info.id)) continue;
            Undo.RecordObject(info.gameObject, "Hide Blueprint");
            info.gameObject.SetActive(false);
        }
        foreach (var t in bp.GetComponentsInChildren<Transform>(true))
        {
            if (t.parent == null || !t.parent.name.Contains("빵집 실내")) continue;
            if (t.name.EndsWith(" 빵집 사장") || t.name.EndsWith(" 조각 5"))
            {
                Undo.RecordObject(t.gameObject, "Hide Blueprint");
                t.gameObject.SetActive(false);
            }
        }
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

    // ───────────────────────── 주소 맞추기 화면 ─────────────────────────

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null) return;
        var es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static AddressRestoreView CreateView(Transform canvas, TMP_FontAsset font)
    {
        var root = UI("AddressRestoreView", canvas);
        Stretch(root);
        var dialogueView = canvas.Find("DialogueView");
        if (dialogueView != null) root.SetSiblingIndex(dialogueView.GetSiblingIndex());

        var panel = UI("Panel", root);
        Stretch(panel);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var dialog = Box(panel, "Dialog", Vector2.zero, new Vector2(1100, 620), Night);
        Text(dialog, "Title", "번진 봉투 주소 맞추기", 48, Cream, new Vector2(0, 250), new Vector2(1000, 70), font);

        // 봉투 종이
        var paper = Box(dialog, "Paper", new Vector2(0, 90), new Vector2(960, 170), new Color(0.93f, 0.88f, 0.78f));

        // 주소 한 줄:  To. [번짐][번짐]  [칸0][칸1] 의  [번짐] 에 [칸2]
        //   (번짐 = 읽을 수 없는 글자, 칸 = 버튼)
        var ink = new Color(0.2f, 0.18f, 0.25f);
        var slots = new Button[3];
        var slotTexts = new TMP_Text[3];
        float x = -400f;
        void Advance(float w, float gap = 8f) => x += w + gap;
        void Word(string s, float w) { Text(paper, $"Word_{s}", s, 52, ink, new Vector2(x + w / 2f, 0), new Vector2(w, 90), font); Advance(w); }
        void Blot() { var b = Box(paper, "Smudge", new Vector2(x + 35f, 0), new Vector2(70, 70), Smudge); b.GetComponent<Image>().color = new Color(Smudge.r, Smudge.g, Smudge.b, 0.75f); Advance(70); }
        void Slot(int i)
        {
            slots[i] = MakeButton(paper, $"Slot_{i + 1}", "?", new Vector2(x + 45f, 0), new Vector2(90, 100), font, 52);
            slotTexts[i] = slots[i].GetComponentInChildren<TMP_Text>();
            Advance(90);
        }
        Word("To.", 100); x += 22f;
        Blot(); Blot(); x += 30f;
        Slot(0); Slot(1); Word("의", 64); x += 22f;
        Blot(); Word("에", 64); Slot(2);

        var letters = Text(dialog, "Letters", "가진 글자 스티커:", 32, Cream, new Vector2(0, -30), new Vector2(1000, 50), font);
        var message = Text(dialog, "Message", "", 32, new Color(1f, 0.84f, 0.45f), new Vector2(0, -95), new Vector2(1040, 60), font);
        var submit = MakeButton(dialog, "Submit", "확인", new Vector2(0, -180), new Vector2(240, 80), font, 36);
        Text(dialog, "Hint", "칸을 눌러 글자 바꾸기    방향키 + Enter (또는 클릭)    Esc: 닫기", 26, new Color(1f, 1f, 1f, 0.55f), new Vector2(0, -265), new Vector2(1040, 44), font);

        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            if (g.GetComponent<Button>() == null) g.raycastTarget = g.gameObject == panel.gameObject; // 패널은 뒤쪽 클릭을 막음

        var view = root.gameObject.AddComponent<AddressRestoreView>();
        var so = new SerializedObject(view);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        var sb = so.FindProperty("slotButtons"); sb.arraySize = 3;
        var st = so.FindProperty("slotTexts"); st.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            sb.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            st.GetArrayElementAtIndex(i).objectReferenceValue = slotTexts[i];
        }
        so.FindProperty("submitButton").objectReferenceValue = submit;
        so.FindProperty("lettersText").objectReferenceValue = letters;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create AddressRestoreView");
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

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, TMP_FontAsset font, float fontSize)
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
        Text(rt, "Label", label, fontSize, Cream, Vector2.zero, size, font);
        return button;
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
