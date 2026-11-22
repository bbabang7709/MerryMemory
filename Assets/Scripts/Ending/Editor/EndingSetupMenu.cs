using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup 2-5 Slice  (Village 씬을 연 상태에서 실행)
/// 로드맵 2-5의 임시 흐름에 필요한 것을 만들고 연결합니다.
///  - 대사 5개 (Assets/ScriptableObjects/Dialogues)
///  - Gameplay 오브젝트: 우체국장(NpcTalk), 우표 조각(PiecePickup), 광장 트리(EndingTrigger), 엔딩(EndingSequence)
///  - UICanvas/EndingView: 어둡게 덮는 판, 정면 클로즈업, MERRY CHRISTMAS!!
///  - 플레이어 시작 위치: 우체국 앞마당
/// 우체국 실내·문 이동은 4단계에서 만들므로, 지금은 우체국장과 우표를 앞마당에 둡니다.
/// </summary>
public static class EndingSetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string FemaleIdle = "Assets/Art/Characters/Player/Player_Female_Idle.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";

    // MAP.md 좌표 → 월드 (셀 = (x, 47 − y), 캐릭터는 발밑 피벗)
    private static Vector3 Feet(int mapX, int mapY) => new(mapX + 0.5f, 47 - mapY + 0.1f, 0f);

    [MenuItem("Merry Memory/Setup 2-5 Slice")]
    private static void Setup()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        var canvas = GameObject.Find("UICanvas");
        if (player == null || canvas == null || Object.FindFirstObjectByType<DialogueRunner>() == null)
        {
            EditorUtility.DisplayDialog("Setup 2-5 Slice", "Village 씬을 열고 실행하세요. (Player, UICanvas, DialogueRunner 필요)", "확인");
            return;
        }
        if (GameObject.Find("Gameplay") != null)
        {
            EditorUtility.DisplayDialog("Setup 2-5 Slice", "씬에 이미 Gameplay 오브젝트가 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var prologue = Dialogue("Prologue_PostMaster",
            ("우체국장", "{이름}{아/야}, 오늘 배달 수고 많았어. 이제 딱 한 장만 남았단다."),
            ("", "그때, 거센 눈바람이 휘몰아쳐 커다란 카드가 조각조각 흩어졌다!"),
            ("우체국장", "자정 전에 배달해야 하는 카드인데...!"),
            ("{이름}", "제가 찾아올게요. 근데 이거, 누구한테 가는 카드예요?"),
            ("우체국장", "그게... 봉투 주소가 젖어서 번져 버렸지 뭐야."),
            ("우체국장", "조각 하나가 우체국 앞 어딘가로 떨어진 것 같았어. 찾으면 광장 트리로 가져가 보렴."));
        var repeat = Dialogue("PostMaster_Repeat",
            ("우체국장", "조각은 우체국 앞 어딘가에 떨어졌을 거야. 찾으면 광장 트리로!"));
        var found = Dialogue("Piece_Stamp_Found",
            ("", "눈 속에서 반짝이는 우표 조각을 찾았다!"),
            ("{이름}", "카드 조각 하나 찾았다. 광장 트리로 가 보자."));
        var notYet = Dialogue("Tree_NotYet",
            ("{이름}", "조각이 아직 모자라."));
        var endingLines = Dialogue("Ending",
            ("", "트리 앞에 우표 조각을 대어 보자, 젖어 있던 봉투의 주소가 천천히 떠올랐다."),
            ("", "To. 화면 너머의 너에게"),
            ("{이름}", "...아. 그래서 다들 그렇게 말한 거구나."),
            ("{이름}", "날 여기까지 데려와 준 거, 너였구나."),
            ("{이름}", "배달 왔습니다! 우리 마을 모두가 너한테 보내는 카드야."),
            ("{이름}", "같이 와 줘서 고마워. 그럼, 다 같이..."));

        Undo.SetCurrentGroupName("Setup 2-5 Slice");
        int group = Undo.GetCurrentGroup();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var root = new GameObject("Gameplay");
        Undo.RegisterCreatedObjectUndo(root, "Create Gameplay");

        // ── 우체국장 (우체국 앞마당) ──
        var master = new GameObject("PostMaster");
        master.transform.SetParent(root.transform);
        master.transform.position = Feet(27, 39);
        var masterSprite = master.AddComponent<SpriteRenderer>();
        masterSprite.sprite = Front(MaleIdle).FirstOrDefault();
        masterSprite.color = new Color(1f, 0.78f, 0.55f); // 임시: 주인공과 구분되게 따뜻한 색
        masterSprite.spriteSortPoint = SpriteSortPoint.Pivot;
        var masterCol = master.AddComponent<CapsuleCollider2D>(); // 발만 막힘 (플레이어와 같은 규칙)
        masterCol.direction = CapsuleDirection2D.Horizontal;
        masterCol.size = new Vector2(0.6f, 0.3f);
        masterCol.offset = new Vector2(0f, 0.15f);
        var npc = master.AddComponent<NpcTalk>();
        var npcSO = new SerializedObject(npc);
        npcSO.FindProperty("firstDialogue").objectReferenceValue = prologue;
        npcSO.FindProperty("repeatDialogue").objectReferenceValue = repeat;
        npcSO.ApplyModifiedPropertiesWithoutUndo();

        // ── 우표 조각 (앞마당 바닥, 지나갈 수 있음) ──
        var stamp = new GameObject("Piece_Stamp");
        stamp.transform.SetParent(root.transform);
        stamp.transform.position = new Vector3(22.5f, 7.5f, 0f);
        var stampSprite = stamp.AddComponent<SpriteRenderer>();
        stampSprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);
        stampSprite.drawMode = SpriteDrawMode.Sliced;
        stampSprite.size = new Vector2(0.45f, 0.55f);
        stampSprite.color = new Color(1f, 0.45f, 0.45f);
        var stampCol = stamp.AddComponent<BoxCollider2D>();
        stampCol.isTrigger = true;
        stampCol.size = new Vector2(0.6f, 0.6f);
        var pickup = stamp.AddComponent<PiecePickup>();
        var pickSO = new SerializedObject(pickup);
        pickSO.FindProperty("foundDialogue").objectReferenceValue = found;
        pickSO.FindProperty("promptOffset").vector2Value = new Vector2(0f, 0.8f);
        pickSO.ApplyModifiedPropertiesWithoutUndo();

        // ── 엔딩 UI (대화창보다 아래에 그려지도록 DialogueView 앞 형제로) ──
        var view = CreateUI("EndingView", canvas.transform);
        Stretch(view);
        var dialogueView = canvas.transform.Find("DialogueView");
        view.SetSiblingIndex(dialogueView != null ? dialogueView.GetSiblingIndex() : canvas.transform.childCount - 1);

        var dim = CreateUI("Dim", view);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.78f);
        var dimGroup = Hidden(dim);

        var closeup = CreateUI("Closeup", view);
        closeup.anchoredPosition = new Vector2(0, 120);
        closeup.sizeDelta = new Vector2(480, 720);
        var closeupImage = closeup.gameObject.AddComponent<Image>();
        closeupImage.preserveAspect = true;
        closeupImage.sprite = Front(FemaleIdle).FirstOrDefault();
        var closeupGroup = Hidden(closeup);

        var merry = CreateUI("Merry", view);
        Stretch(merry);
        var merryText = merry.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) merryText.font = font;
        merryText.text = "MERRY CHRISTMAS!!";
        merryText.fontSize = 170;
        merryText.color = new Color(1f, 0.84f, 0.45f);
        merryText.alignment = TextAlignmentOptions.Center;
        merryText.textWrappingMode = TextWrappingModes.NoWrap;
        var merryGroup = Hidden(merry);
        foreach (var g in view.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

        // ── 엔딩 진행 ──
        var endingGO = new GameObject("Ending");
        endingGO.transform.SetParent(root.transform);
        var ending = endingGO.AddComponent<EndingSequence>();
        var endSO = new SerializedObject(ending);
        endSO.FindProperty("player").objectReferenceValue = player;
        endSO.FindProperty("dim").objectReferenceValue = dimGroup;
        endSO.FindProperty("closeupGroup").objectReferenceValue = closeupGroup;
        endSO.FindProperty("closeupImage").objectReferenceValue = closeupImage;
        endSO.FindProperty("femaleCloseup").objectReferenceValue = Front(FemaleIdle).FirstOrDefault();
        endSO.FindProperty("maleCloseup").objectReferenceValue = Front(MaleIdle).FirstOrDefault();
        endSO.FindProperty("merry").objectReferenceValue = merryGroup;
        endSO.FindProperty("endingDialogue").objectReferenceValue = endingLines;
        endSO.ApplyModifiedPropertiesWithoutUndo();

        // ── 광장 트리 (MAP P0: (30, 21) 4×5) ──
        var tree = new GameObject("EndingTree");
        tree.transform.SetParent(root.transform);
        tree.transform.position = new Vector3(32f, 24.5f, 0f);
        var treeCol = tree.AddComponent<BoxCollider2D>();
        treeCol.isTrigger = true; // 막는 건 청사진 블록이 하고, 이건 조사 감지용
        treeCol.size = new Vector2(4f, 5f);
        var trigger = tree.AddComponent<EndingTrigger>();
        var trigSO = new SerializedObject(trigger);
        trigSO.FindProperty("notYetDialogue").objectReferenceValue = notYet;
        trigSO.FindProperty("ending").objectReferenceValue = ending;
        trigSO.ApplyModifiedPropertiesWithoutUndo();

        // ── 시작 위치: 우체국 문 앞 ──
        Undo.RecordObject(player.transform, "Move Player");
        player.transform.position = Feet(24, 39) + new Vector3(0.5f, 0f, 0f);
        var cm = GameObject.Find("CM Follow Camera");
        if (cm != null)
        {
            Undo.RecordObject(cm.transform, "Move Camera");
            cm.transform.position = player.transform.position + new Vector3(0f, 0.75f, -10f);
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("2-5 임시 흐름을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    // ───────────────────────── 도우미 ─────────────────────────

    /// <summary>대사 에셋을 만들거나(이미 있으면 그대로) 돌려줍니다.</summary>
    private static DialogueData Dialogue(string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(DialogueFolder);
        string path = $"{DialogueFolder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing; // 직접 고친 대사는 덮어쓰지 않음

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static Sprite[] Front(string sheet) =>
        AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>()
            .Where(s => s.name.Contains("_Front_")).OrderBy(s => s.name).ToArray();

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static CanvasGroup Hidden(RectTransform rt)
    {
        var g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.interactable = false;
        g.blocksRaycasts = false;
        return g;
    }
}
