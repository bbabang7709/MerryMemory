using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Post Office (4-1)  (Village 씬에서 실행)
///  - 우체국 실내(1-7 청사진 (80,0) 14×10)에 편지 6통, 분류함 4칸, 편지 분류 퍼즐 + 우표 보상을 놓고
///  - 2-5 임시물 정리: 앞마당 우체국장 → 실내 (5,2), 앞마당 우표 줍기 삭제
///  - 시작 위치를 우체국 실내 (6,6)로, 출구는 퍼즐을 풀 때까지 잠금
///  - 왼쪽 위 "들고 있는 것" 표시(CarriedItemView)
///  - 청사진의 편지·우체국장·조각 1 표시는 진짜 오브젝트와 겹치므로 끔
/// 실내 좌표 (lx, ly) → 월드 칸 (80 + lx, 47 − ly)
/// </summary>
public static class PostOfficeSetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues/PostOffice";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string StampPiece = "Assets/ScriptableObjects/CardPieces/Piece_1_Stamp.asset";

    private static Vector3 Cell(int lx, int ly) => new(80 + lx + 0.5f, 47 - ly + 0.5f, 0f); // 칸 가운데
    private static Vector3 Feet(int lx, int ly) => new(80 + lx + 0.5f, 47 - ly + 0.1f, 0f); // 캐릭터 발

    // 분류함 칸: 위에서부터 (MAP 7.1)
    private static readonly (string tag, string label)[] Slots =
    {
        ("clocktower", "시계탑 길"), ("workshop", "공방 길"), ("bakery", "빵집 길"), ("snowhill", "눈 언덕"),
    };

    // 편지 6통: 위치(MAP 7.1)와 받는 곳
    private static readonly (int lx, int ly, int slot)[] Letters =
    {
        (4, 6, 0), (7, 5, 2), (9, 7, 1), (11, 6, 3), (3, 8, 2), (12, 8, 0),
    };

    [MenuItem("Merry Memory/Setup Post Office (4-1)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        var canvas = GameObject.Find("UICanvas");
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (gameplay == null || canvas == null || player == null)
        {
            EditorUtility.DisplayDialog("Setup Post Office", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (gameplay.transform.Find("PostOffice") != null)
        {
            EditorUtility.DisplayDialog("Setup Post Office", "이미 Gameplay/PostOffice가 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var alreadyHolding = Dialogue("Mail_AlreadyHolding", ("{이름}", "이미 편지를 들고 있어. 먼저 분류함에 넣자."));
        var wrongSlot = Dialogue("Mail_WrongSlot", ("{이름}", "여긴 아닌 것 같아. 편지에 적힌 받는 곳을 다시 보자."));
        var emptyHand = Dialogue("Mail_SlotEmptyHand", ("", "분류함이다. 위에서부터 시계탑 길, 공방 길, 빵집 길, 눈 언덕."));
        var exitLocked = Dialogue("PostOffice_ExitLocked", ("우체국장", "{이름}{아/야}, 나가기 전에 흩어진 편지부터 분류함에 정리해 주겠니?"));
        UpdatePostMasterLines();

        Undo.SetCurrentGroupName("Setup Post Office");
        int group = Undo.GetCurrentGroup();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var box = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);

        var root = new GameObject("PostOffice");
        Undo.RegisterCreatedObjectUndo(root, "Create PostOffice");
        root.transform.SetParent(gameplay.transform, false);
        root.transform.position = Cell(1, 5);

        // ── 퍼즐 + 보상 ──
        var puzzle = root.AddComponent<MailSortPuzzle>();
        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(StampPiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        // ── 분류함 4칸 (청사진 분류함 블록 (1,4)~(1,7) 위에 겹쳐 놓는 조사용 트리거 + 칸 이름) ──
        for (int i = 0; i < Slots.Length; i++)
        {
            var go = new GameObject($"Slot_{i + 1}_{Slots[i].tag}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = Cell(1, 4 + i);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 0.9f);

            var slot = go.AddComponent<MailSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("destinationTag").stringValue = Slots[i].tag;
            so.FindProperty("puzzle").objectReferenceValue = puzzle;
            so.FindProperty("emptyHandDialogue").objectReferenceValue = emptyHand;
            so.FindProperty("wrongDialogue").objectReferenceValue = wrongSlot;
            so.ApplyModifiedPropertiesWithoutUndo();

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshPro>();
            label.transform.SetParent(go.transform, false);
            if (font != null) label.font = font;
            label.text = Slots[i].label;
            label.rectTransform.sizeDelta = new Vector2(1f, 0.9f);
            label.enableAutoSizing = true; label.fontSizeMin = 1f; label.fontSizeMax = 2.4f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.sortingOrder = -3;
        }

        // ── 편지 6통 ──
        var letterArray = new MailLetter[Letters.Length];
        for (int i = 0; i < Letters.Length; i++)
        {
            var (lx, ly, slotIndex) = Letters[i];
            var go = new GameObject($"Letter_{i + 1}_{Slots[slotIndex].tag}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = Cell(lx, ly);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = box;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.6f, 0.42f);
            sr.color = new Color(0.95f, 0.91f, 0.84f);
            sr.sortingOrder = -4;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.7f, 0.7f);

            var letter = go.AddComponent<MailLetter>();
            var so = new SerializedObject(letter);
            var item = so.FindProperty("item");
            item.FindPropertyRelative("id").stringValue = $"letter_{i + 1}";
            item.FindPropertyRelative("label").stringValue = $"편지 ({Slots[slotIndex].label})";
            item.FindPropertyRelative("tag").stringValue = Slots[slotIndex].tag;
            so.FindProperty("alreadyHoldingDialogue").objectReferenceValue = alreadyHolding;
            so.ApplyModifiedPropertiesWithoutUndo();
            letterArray[i] = letter;
        }
        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = FlagKeys.MailSort;
        var arr = pso.FindProperty("letters");
        arr.arraySize = letterArray.Length;
        for (int i = 0; i < letterArray.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = letterArray[i];
        pso.ApplyModifiedPropertiesWithoutUndo();

        // ── 2-5 임시물 정리 ──
        var master = gameplay.transform.Find("PostMaster");
        if (master != null)
        {
            Undo.RecordObject(master, "Move PostMaster");
            master.position = Feet(5, 2);

            // 우체국장은 카운터 뒤에 서 있어서, 카운터 앞에서도 말을 걸 수 있게 아래로 긴 감지 영역을 덧댐
            if (master.Find("TalkArea") == null)
            {
                var area = new GameObject("TalkArea");
                Undo.RegisterCreatedObjectUndo(area, "Create TalkArea");
                area.transform.SetParent(master, false);
                var talkCol = area.AddComponent<BoxCollider2D>();
                talkCol.isTrigger = true;
                talkCol.offset = new Vector2(0f, -0.5f);
                talkCol.size = new Vector2(1f, 1.6f);
            }
        }
        var oldStamp = gameplay.transform.Find("Piece_Stamp");
        if (oldStamp != null) Undo.DestroyObjectImmediate(oldStamp.gameObject);

        // 청사진의 편지·우체국장·조각 1 표시 끄기 (진짜 오브젝트와 겹침)
        var bpRoom = GameObject.Find("Village Blueprint/Interiors/우체국 실내");
        if (bpRoom != null)
        {
            foreach (Transform t in bpRoom.transform)
            {
                if (t.name.EndsWith(" 편지") || t.name.EndsWith(" 우체국장") || t.name.EndsWith(" 조각 1"))
                {
                    Undo.RecordObject(t.gameObject, "Hide Blueprint");
                    t.gameObject.SetActive(false);
                }
                // 분류함 블록의 "분류함" 글자는 칸 이름과 겹치므로 글자만 끔
                if (t.name.EndsWith(" 분류함") && t.Find("Label") != null)
                {
                    Undo.RecordObject(t.Find("Label").gameObject, "Hide Label");
                    t.Find("Label").gameObject.SetActive(false);
                }
            }
        }

        // ── 출구 잠금 ──
        var exit = gameplay.transform.Find("Doors/Door_PostOffice_Exit");
        if (exit != null)
        {
            var dso = new SerializedObject(exit.GetComponent<Door>());
            dso.FindProperty("requiredFlag").stringValue = FlagKeys.PuzzleSolved(FlagKeys.MailSort);
            dso.FindProperty("lockedDialogue").objectReferenceValue = exitLocked;
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 시작 위치: 우체국 실내 (6,6) ──
        Undo.RecordObject(player.transform, "Move Player");
        player.transform.position = Feet(6, 6);
        foreach (var camName in new[] { "CM Follow Camera", "Main Camera" })
        {
            var cam = GameObject.Find(camName);
            if (cam == null) continue;
            Undo.RecordObject(cam.transform, "Move Camera");
            cam.transform.position = player.transform.position + new Vector3(0f, 0.75f, -10f);
        }

        // ── 왼쪽 위 "들고 있는 것" 표시 ──
        CreateCarriedItemView(canvas.transform, font);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("우체국(4-1)을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static void CreateCarriedItemView(Transform canvas, TMP_FontAsset font)
    {
        if (Object.FindFirstObjectByType<CarriedItemView>(FindObjectsInactive.Include) != null) return;

        var rootGO = new GameObject("CarriedItemView", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootGO, "Create CarriedItemView");
        rootGO.layer = LayerMask.NameToLayer("UI");
        var root = (RectTransform)rootGO.transform;
        root.SetParent(canvas, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(32f, -32f);
        root.sizeDelta = new Vector2(560f, 64f);
        var endingView = canvas.Find("EndingView");
        if (endingView != null) root.SetSiblingIndex(endingView.GetSiblingIndex());

        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        boxGO.layer = rootGO.layer;
        var boxRT = (RectTransform)boxGO.transform;
        boxRT.SetParent(root, false);
        boxRT.anchorMin = Vector2.zero; boxRT.anchorMax = Vector2.one; boxRT.offsetMin = boxRT.offsetMax = Vector2.zero;
        var img = boxGO.GetComponent<Image>();
        img.color = new Color(0.07f, 0.09f, 0.17f, 0.75f);
        img.raycastTarget = false;

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.layer = rootGO.layer;
        var textRT = (RectTransform)textGO.transform;
        textRT.SetParent(boxRT, false);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20f, 0f); textRT.offsetMax = new Vector2(-20f, 0f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 28;
        tmp.color = new Color(0.96f, 0.95f, 0.9f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.text = "들고 있는 것: 편지 (시계탑 길)";

        var view = rootGO.AddComponent<CarriedItemView>();
        var so = new SerializedObject(view);
        so.FindProperty("box").objectReferenceValue = boxGO;
        so.FindProperty("label").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();
        boxGO.SetActive(false);
    }

    /// <summary>2-5 우체국장 대사를 편지 정리 튜토리얼에 맞게 바꿉니다. (직접 고친 대사면 건드리지 않음)</summary>
    private static void UpdatePostMasterLines()
    {
        var prologue = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/ScriptableObjects/Dialogues/Prologue_PostMaster.asset");
        if (prologue != null && prologue.lines.Count > 0)
        {
            var last = prologue.lines[prologue.lines.Count - 1];
            if (last.text.StartsWith("조각 하나가 우체국 앞"))
            {
                last.text = "우선 바람에 흩어진 편지들부터 분류함에 넣어 주겠니? 다 정리되면 카드 조각 하나를 맡길게.";
                EditorUtility.SetDirty(prologue);
            }
        }
        var repeat = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/ScriptableObjects/Dialogues/PostMaster_Repeat.asset");
        if (repeat != null && repeat.lines.Count > 0 && repeat.lines[0].text.StartsWith("조각은 우체국 앞"))
        {
            repeat.lines[0].text = "편지에 적힌 받는 곳을 보고, 분류함에서 같은 이름의 칸에 넣으면 된단다.";
            EditorUtility.SetDirty(repeat);
        }
        AssetDatabase.SaveAssets();
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
