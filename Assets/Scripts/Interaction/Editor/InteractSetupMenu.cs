using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Interact Prompt
/// Player에 PlayerInteractor를 붙이고, UICanvas에 "[F] 말 걸기" 안내 UI를 만들어 연결합니다.
/// (먼저 Setup Dialogue UI로 UICanvas를 만들어 두세요.) 실행 후 씬을 저장하세요.
/// </summary>
public static class InteractSetupMenu
{
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";

    private static readonly Color BoxColor  = new(0.07f, 0.09f, 0.17f, 0.88f);
    private static readonly Color TextColor = new(0.96f, 0.95f, 0.90f, 1f);

    [MenuItem("Merry Memory/Setup Interact Prompt")]
    private static void Setup()
    {
        var controller = Object.FindFirstObjectByType<PlayerController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Setup Interact Prompt", "씬에 PlayerController가 있는 오브젝트가 없습니다.", "확인");
            return;
        }
        if (Object.FindFirstObjectByType<InteractPrompt>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Setup Interact Prompt", "씬에 이미 InteractPrompt가 있습니다.", "확인");
            return;
        }

        var canvas = GameObject.Find("UICanvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Interact Prompt", "UICanvas가 없습니다. 먼저 Merry Memory → Setup Dialogue UI를 실행하세요.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Setup Interact Prompt");
        int group = Undo.GetCurrentGroup();

        // ── Player에 PlayerInteractor ──
        var interactor = controller.GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = Undo.AddComponent<PlayerInteractor>(controller.gameObject);

        // ── InteractPrompt (꽉 찬 부모, 대화창보다 뒤에 그려지도록 맨 앞 형제로) ──
        var rootRT = CreateUI("InteractPrompt", canvas.transform);
        rootRT.SetAsFirstSibling();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;
        var prompt = rootRT.gameObject.AddComponent<InteractPrompt>();

        // ── Box: 글자 길이에 맞춰 늘어나는 말풍선 ──
        var boxRT = CreateUI("Box", rootRT);
        boxRT.anchorMin = boxRT.anchorMax = Vector2.zero; // 화면 좌표를 그대로 position에 넣기 위해 좌하단 기준
        boxRT.pivot = new Vector2(0.5f, 0f);              // 아래 가운데가 대상 머리 위에 오도록
        var image = boxRT.gameObject.AddComponent<Image>();
        image.color = BoxColor;
        image.raycastTarget = false;
        var layout = boxRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 8, 8);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var fitter = boxRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var label = CreateUI("Label", boxRT).gameObject.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) label.font = font;
        label.fontSize = 30;
        label.color = TextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = "[F] 말 걸기";

        // ── 참조 연결 ──
        var so = new SerializedObject(prompt);
        so.FindProperty("interactor").objectReferenceValue = interactor;
        so.FindProperty("box").objectReferenceValue = boxRT;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = rootRT.gameObject;
        Debug.Log("상호작용 안내 UI를 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
