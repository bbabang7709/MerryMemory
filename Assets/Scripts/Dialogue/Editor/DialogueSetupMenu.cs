using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Dialogue UI
/// 현재 씬에 대화창 UI와 DialogueRunner를 만들고 참조까지 연결합니다. 실행 후 씬을 저장하세요.
/// </summary>
public static class DialogueSetupMenu
{
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string TestDialoguePath = "Assets/ScriptableObjects/Dialogues/Test_Dialogue.asset";

    private static readonly Color PanelColor   = new(0.07f, 0.09f, 0.17f, 0.92f);
    private static readonly Color SpeakerColor = new(0.72f, 0.18f, 0.22f, 1f);
    private static readonly Color TextColor    = new(0.96f, 0.95f, 0.90f, 1f);

    [MenuItem("Merry Memory/Setup Dialogue UI")]
    private static void Setup()
    {
        if (Object.FindFirstObjectByType<DialogueView>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Setup Dialogue UI", "씬에 이미 DialogueView가 있습니다.", "확인");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"폰트를 찾지 못했습니다: {FontPath}. TMP 기본 폰트를 씁니다.");

        Undo.SetCurrentGroupName("Setup Dialogue UI");
        int group = Undo.GetCurrentGroup();

        // ── Canvas ──
        var canvasGO = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create UICanvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ── DialogueView (항상 켜져 있는 부모) ──
        var viewRT = CreateUI("DialogueView", canvasGO.transform);
        Stretch(viewRT, Vector2.zero, Vector2.zero);
        var view = viewRT.gameObject.AddComponent<DialogueView>();

        // ── DialoguePanel (화면 아래쪽) ──
        var panelRT = CreateUI("DialoguePanel", viewRT);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.sizeDelta = new Vector2(1600, 300);
        panelRT.anchoredPosition = new Vector2(0, 48);
        panelRT.gameObject.AddComponent<Image>().color = PanelColor;

        // ── SpeakerBox + SpeakerName (패널 왼쪽 위에 걸침) ──
        var speakerRT = CreateUI("SpeakerBox", panelRT);
        speakerRT.anchorMin = speakerRT.anchorMax = new Vector2(0f, 1f);
        speakerRT.pivot = new Vector2(0f, 0.5f);
        speakerRT.sizeDelta = new Vector2(320, 64);
        speakerRT.anchoredPosition = new Vector2(48, 0);
        speakerRT.gameObject.AddComponent<Image>().color = SpeakerColor;

        var speakerText = CreateText("SpeakerName", speakerRT, font, 34, TextAlignmentOptions.Center);
        Stretch(speakerText.rectTransform, Vector2.zero, Vector2.zero);
        speakerText.text = "화자";

        // ── Body ──
        var body = CreateText("Body", panelRT, font, 40, TextAlignmentOptions.TopLeft);
        Stretch(body.rectTransform, new Vector2(64, 48), new Vector2(64, 60)); // 좌우, 아래·위 여백
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 12;
        body.text = "대사가 여기에 표시됩니다.";

        // ── NextArrow (마름모, 오른쪽 아래) ──
        var arrowRT = CreateUI("NextArrow", panelRT);
        arrowRT.anchorMin = arrowRT.anchorMax = new Vector2(1f, 0f);
        arrowRT.pivot = new Vector2(0.5f, 0.5f);
        arrowRT.sizeDelta = new Vector2(20, 20);
        arrowRT.anchoredPosition = new Vector2(-56, 44);
        arrowRT.localRotation = Quaternion.Euler(0, 0, 45);
        arrowRT.gameObject.AddComponent<Image>().color = TextColor;

        // ── DialogueView 참조 연결 ──
        var so = new SerializedObject(view);
        so.FindProperty("panel").objectReferenceValue = panelRT.gameObject;
        so.FindProperty("speakerBox").objectReferenceValue = speakerRT.gameObject;
        so.FindProperty("speakerText").objectReferenceValue = speakerText;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("nextArrow").objectReferenceValue = arrowRT;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── [Systems] + DialogueRunner ──
        var systems = GameObject.Find("[Systems]");
        if (systems == null)
        {
            systems = new GameObject("[Systems]");
            Undo.RegisterCreatedObjectUndo(systems, "Create [Systems]");
        }
        // UnityEngine.Object에는 ?? 연산자를 쓰면 안 됩니다(가짜 null 문제). 명시적으로 비교합니다.
        var runner = systems.GetComponent<DialogueRunner>();
        if (runner == null) runner = Undo.AddComponent<DialogueRunner>(systems);
        var runnerSO = new SerializedObject(runner);
        runnerSO.FindProperty("view").objectReferenceValue = view;
        runnerSO.FindProperty("testDialogue").objectReferenceValue = GetOrCreateTestDialogue();
        runnerSO.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Selection.activeGameObject = systems;
        Debug.Log("대화 UI를 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions align)
    {
        var text = CreateUI(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = TextColor;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>부모를 꽉 채우되 여백을 줍니다. min = (왼쪽, 아래), max = (오른쪽, 위)</summary>
    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = -max;
    }

    private static DialogueData GetOrCreateTestDialogue()
    {
        var data = AssetDatabase.LoadAssetAtPath<DialogueData>(TestDialoguePath);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "우체국장", text = "어서 와, 임시 집배원! 기다리고 있었단다." });
        data.lines.Add(new DialogueLine { speaker = "우체국장", text = "오늘은 크리스마스이브라서 배달할 게 아주 많아. 글자가 다 나오기 전에 키를 누르면 한 번에 표시돼." });
        data.lines.Add(new DialogueLine { speaker = "", text = "(화자가 비어 있으면 이름표 없이 표시된다.)" });
        AssetDatabase.CreateAsset(data, TestDialoguePath);
        AssetDatabase.SaveAssets();
        return data;
    }
}
