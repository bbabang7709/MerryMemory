using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Title & NameBadge Scenes  (로드맵 2-2)
/// Title.unity, NameBadge.unity를 만들고 UI와 스크립트 참조를 연결한 뒤,
/// Build Profiles 씬 목록을 Title → NameBadge → Village 순서로 맞춥니다.
/// 열려 있던 씬은 저장 여부를 물은 뒤, 끝나면 다시 엽니다.
/// </summary>
public static class FrontScreensSetupMenu
{
    private const string SceneFolder = "Assets/Scenes";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string FemaleIdle = "Assets/Art/Characters/Player/Player_Female_Idle.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";

    // ── 색 ──
    private static readonly Color Night = Hex("#14213D");     // 배경
    private static readonly Color Cream = Hex("#F3E8D7");     // 명찰 종이, 밝은 글자
    private static readonly Color Ink = Hex("#1B2135");       // 명찰 위 글자
    private static readonly Color Muted = Hex("#8A93AB");     // 보조 글자
    private static readonly Color Red = Hex("#B8322A");       // 우체국 빨강
    private static readonly Color Navy = Hex("#2B3354");      // 보조 버튼
    private static readonly Color PhotoBack = Hex("#DDE6F0");

    private static TMP_FontAsset font;

    [MenuItem("Merry Memory/Setup Title & NameBadge Scenes")]
    private static void Setup()
    {
        string titlePath = $"{SceneFolder}/{SceneNames.Title}.unity";
        string badgePath = $"{SceneFolder}/{SceneNames.NameBadge}.unity";
        if ((File.Exists(titlePath) || File.Exists(badgePath)) &&
            !EditorUtility.DisplayDialog("Setup Title & NameBadge Scenes",
                "Title 또는 NameBadge 씬이 이미 있습니다. 새로 만들어 덮어쓸까요?", "덮어쓰기", "취소"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = SceneManager.GetActiveScene().path;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"폰트를 찾지 못했습니다: {FontPath}");

        BuildTitle(titlePath);
        BuildNameBadge(badgePath);
        UpdateBuildScenes();

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
        Debug.Log("Title, NameBadge 씬을 만들고 Build Profiles 씬 순서를 맞췄습니다.");
    }

    // ───────────────────────── Title ─────────────────────────

    private static void BuildTitle(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas();

        Text(canvas, "Logo", "MERRY MEMORY", 150, Cream, new Vector2(0, 220), new Vector2(1600, 200));
        Text(canvas, "Subtitle", "크리스마스이브, 작은 눈 마을의 배달", 40, Muted, new Vector2(0, 100), new Vector2(1200, 60));

        var start = MakeButton(canvas, "StartButton", "시작하기", new Vector2(0, -110), new Vector2(420, 100), Red);
        var quit = MakeButton(canvas, "QuitButton", "끝내기", new Vector2(0, -350), new Vector2(420, 100), Navy);
        Text(canvas, "Hint", "방향키로 고르고 Enter로 선택", 28, Muted, new Vector2(0, -460), new Vector2(1200, 50));

        var screen = canvas.gameObject.AddComponent<TitleScreen>();
        var so = new SerializedObject(screen);
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddSaveUI(canvas, screen);
        EditorSceneManager.SaveScene(scene, path);
    }

    /// <summary>
    /// 메뉴 Merry Memory → Add Continue to Title  (로드맵 3-5)
    /// 이미 있는 Title 씬에 [이어하기] 버튼과 "배달 완료" 표시를 더합니다.
    /// </summary>
    [MenuItem("Merry Memory/Add Continue to Title")]
    private static void AddContinueToTitle()
    {
        string path = $"{SceneFolder}/{SceneNames.Title}.unity";
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = SceneManager.GetActiveScene().path;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var screen = Object.FindFirstObjectByType<TitleScreen>();
        if (screen == null)
        {
            Debug.LogWarning("Title 씬에 TitleScreen이 없습니다.");
            return;
        }
        if (screen.transform.Find("ContinueButton") == null)
        {
            var quit = screen.transform.Find("QuitButton") as RectTransform;
            if (quit != null) quit.anchoredPosition = new Vector2(0, -350); // 이어하기 자리 비우기
            AddSaveUI((RectTransform)screen.transform, screen);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Title에 [이어하기]와 \"배달 완료\" 표시를 추가했습니다.");
        }
        if (!string.IsNullOrEmpty(previous) && previous != path && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
    }

    /// <summary>[이어하기] 버튼(시작하기 아래)과 로고 옆 "배달 완료" 표시를 만들고 TitleScreen에 연결합니다.</summary>
    private static void AddSaveUI(RectTransform canvas, TitleScreen screen)
    {
        var cont = MakeButton(canvas, "ContinueButton", "이어하기", new Vector2(0, -230), new Vector2(420, 100), Navy);
        cont.transform.SetSiblingIndex(canvas.Find("StartButton").GetSiblingIndex() + 1);

        // "배달 완료" 도장 (엔딩을 봤을 때만 켜짐)
        var mark = CreateUI("DeliveredMark", canvas);
        mark.anchoredPosition = new Vector2(640, 300);
        mark.sizeDelta = new Vector2(260, 90);
        mark.localEulerAngles = new Vector3(0, 0, -12f);
        var border = Box(mark, "Border", Vector2.zero, new Vector2(260, 90), Red);
        Box(border, "Inner", Vector2.zero, new Vector2(246, 76), Night);
        Text(mark, "Label", "배달 완료", 44, Red, Vector2.zero, new Vector2(240, 70));
        foreach (var g in mark.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        mark.gameObject.SetActive(false);

        var so = new SerializedObject(screen);
        so.FindProperty("continueButton").objectReferenceValue = cont;
        so.FindProperty("deliveredMark").objectReferenceValue = mark.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────────────────────── NameBadge ─────────────────────────

    private static void BuildNameBadge(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        CreateEventSystem();
        var canvas = CreateCanvas();

        Text(canvas, "Heading", "임시 집배원 명찰을 써 주세요", 48, Cream, new Vector2(0, 440), new Vector2(1400, 70));

        // ── 명찰 ──
        var badge = Box(canvas, "Badge", new Vector2(0, -20), new Vector2(1100, 640), Cream);
        var header = Box(badge, "Header", new Vector2(0, 270), new Vector2(1100, 100), Red);
        Text(header, "HeaderText", "MERRY MEMORY 우체국   임시 집배원", 40, Cream, Vector2.zero, new Vector2(1000, 80));

        // 사진 칸
        var frame = Box(badge, "PhotoFrame", new Vector2(-300, 0), new Vector2(300, 400), Ink);
        var photoBack = Box(frame, "PhotoBack", Vector2.zero, new Vector2(288, 388), PhotoBack);
        var photoRT = CreateUI("PhotoSlot", photoBack);
        photoRT.sizeDelta = new Vector2(240, 360);
        var photoImage = photoRT.gameObject.AddComponent<Image>();
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;
        var flipbook = photoRT.gameObject.AddComponent<SpriteFlipbook>();
        var prev = MakeButton(badge, "GenderPrev", "<", new Vector2(-400, -250), new Vector2(90, 70), Navy);
        var next = MakeButton(badge, "GenderNext", ">", new Vector2(-200, -250), new Vector2(90, 70), Navy);
        Text(badge, "PhotoLabel", "사진", 28, Muted, new Vector2(-300, -250), new Vector2(100, 50));

        // 이름 칸
        Text(badge, "NameLabel", "이름", 36, Ink, new Vector2(180, 140), new Vector2(520, 50), TextAlignmentOptions.Left);
        var input = MakeInputField(badge, new Vector2(180, 60), new Vector2(520, 100));
        Text(badge, "NameHint", $"{PlayerProfile.MaxNameLength}글자까지 쓸 수 있어요", 26, Muted, new Vector2(180, -15), new Vector2(520, 40), TextAlignmentOptions.Left);
        var error = Text(badge, "Error", "", 28, Red, new Vector2(180, -65), new Vector2(520, 44), TextAlignmentOptions.Left);
        var confirm = MakeButton(badge, "ConfirmButton", "확인", new Vector2(310, -220), new Vector2(260, 90), Red);

        // 도장 (처음엔 숨김)
        var stamp = CreateUI("Stamp", badge);
        stamp.anchoredPosition = new Vector2(330, -150);
        stamp.sizeDelta = new Vector2(300, 150);
        stamp.localEulerAngles = new Vector3(0, 0, -12f);
        stamp.gameObject.AddComponent<CanvasGroup>();
        var border = Box(stamp, "Border", Vector2.zero, new Vector2(300, 150), Red);
        var inner = Box(border, "Inner", Vector2.zero, new Vector2(284, 134), Cream);
        Text(inner, "Small", "임시 집배원", 26, Red, new Vector2(0, 38), new Vector2(260, 40));
        Text(inner, "Big", "승인", 64, Red, new Vector2(0, -16), new Vector2(260, 80));
        foreach (var g in stamp.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

        // ── 확인 창 ──
        var dim = Box(canvas, "ConfirmPanel", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.6f));
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
        var dialog = Box(dim, "Dialog", Vector2.zero, new Vector2(820, 320), Night);
        var question = Text(dialog, "Question", "OO(으)로 할까요?", 48, Cream, new Vector2(0, 60), new Vector2(760, 80));
        var yes = MakeButton(dialog, "YesButton", "네", new Vector2(-150, -80), new Vector2(240, 90), Red);
        var no = MakeButton(dialog, "NoButton", "아니요", new Vector2(150, -80), new Vector2(240, 90), Navy);

        // ── 스크립트 연결 ──
        var screen = canvas.gameObject.AddComponent<NameBadgeScreen>();
        var so = new SerializedObject(screen);
        so.FindProperty("photo").objectReferenceValue = flipbook;
        SetSprites(so.FindProperty("femaleFrames"), FrontIdle(FemaleIdle));
        SetSprites(so.FindProperty("maleFrames"), FrontIdle(MaleIdle));
        so.FindProperty("prevButton").objectReferenceValue = prev;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("nameInput").objectReferenceValue = input;
        so.FindProperty("errorText").objectReferenceValue = error;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        so.FindProperty("confirmPanel").objectReferenceValue = dim.gameObject;
        so.FindProperty("confirmText").objectReferenceValue = question;
        so.FindProperty("yesButton").objectReferenceValue = yes;
        so.FindProperty("noButton").objectReferenceValue = no;
        so.FindProperty("stamp").objectReferenceValue = stamp;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 편집할 때 보기 좋게 첫 장을 미리 넣어 둠
        var firstFrames = FrontIdle(FemaleIdle);
        if (firstFrames.Length > 0) photoImage.sprite = firstFrames[0];

        dim.gameObject.SetActive(false);
        stamp.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, path);
    }

    private static Sprite[] FrontIdle(string sheet) =>
        AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>()
            .Where(s => s.name.Contains("_Front_"))
            .OrderBy(s => s.name)
            .ToArray();

    private static void SetSprites(SerializedProperty prop, Sprite[] sprites)
    {
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    // ───────────────────────── Build Profiles ─────────────────────────

    private static void UpdateBuildScenes()
    {
        var wanted = new[] { SceneNames.Title, SceneNames.NameBadge, SceneNames.Village }
            .Select(n => $"{SceneFolder}/{n}.unity").ToList();
        var list = new List<EditorBuildSettingsScene>();
        foreach (var p in wanted)
            if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        // 그 밖에 이미 등록돼 있던 씬은 뒤에 그대로 둠
        foreach (var s in EditorBuildSettings.scenes)
            if (!wanted.Contains(s.path) && File.Exists(s.path)) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ───────────────────────── 만들기 도우미 ─────────────────────────

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Night;
        go.transform.position = new Vector3(0, 0, -10);
    }

    private static void CreateEventSystem()
    {
        // 입력 모듈은 액션을 비워 두면 실행할 때 기본 UI 액션(방향키·Enter·마우스)을 스스로 씁니다.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static RectTransform CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var rt = CreateUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
        Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = CreateUI(name, parent);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var rt = Box(parent, name, pos, size, color);
        var button = rt.gameObject.AddComponent<Button>();
        // 키보드로 고른 버튼이 눈에 띄도록: 평소엔 살짝 어둡게, 고르거나 마우스를 올리면 밝게
        var colors = button.colors;
        colors.normalColor = new Color(0.78f, 0.78f, 0.78f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        button.colors = colors;
        Text(rt, "Label", label, Mathf.Min(44, size.y * 0.5f), Cream, Vector2.zero, size);
        return button;
    }

    private static TMP_InputField MakeInputField(Transform parent, Vector2 pos, Vector2 size)
    {
        var resources = new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        };
        var go = TMP_DefaultControls.CreateInputField(resources);
        go.name = "NameInput";
        go.layer = LayerMask.NameToLayer("UI");
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = go.layer;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var input = go.GetComponent<TMP_InputField>();
        input.characterLimit = PlayerProfile.MaxNameLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.pointSize = 48;
        input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        input.textComponent.color = Ink;
        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "이름";
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(Ink.r, Ink.g, Ink.b, 0.35f);
        }
        if (font != null) input.fontAsset = font;
        var textArea = input.textViewport;
        textArea.offsetMin = new Vector2(24, 8);
        textArea.offsetMax = new Vector2(-24, -8);
        return input;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
