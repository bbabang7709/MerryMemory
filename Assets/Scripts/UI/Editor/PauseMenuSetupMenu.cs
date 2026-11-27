using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Pause Menu (6)  (Village 씬에서 실행, 다시 실행하면 새로 만듦)
/// UICanvas 맨 위에 Esc 일시정지 메뉴(PauseMenu)를 만듭니다: 계속하기 / 저장하고 타이틀로 / 저장하고 끝내기
/// </summary>
public static class PauseMenuSetupMenu
{
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private static readonly Color Night = new(0.08f, 0.13f, 0.24f, 1f);
    private static readonly Color Cream = new(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color Red = new(0.72f, 0.2f, 0.16f, 1f);

    [MenuItem("Merry Memory/Setup Pause Menu (6)")]
    private static void Setup()
    {
        var canvas = GameObject.Find("UICanvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Pause Menu", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var old = canvas.transform.Find("PauseMenu");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var root = UI("PauseMenu", canvas.transform);
        Stretch(root);
        root.SetAsLastSibling(); // 대사창·엔딩보다도 위

        var panel = UI("Panel", root);
        Stretch(panel);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // 뒤쪽 클릭도 막음

        var box = Box(panel, "Box", Vector2.zero, new Vector2(620, 520), Night);
        Text(box, "Title", "일시정지", 48, Cream, new Vector2(0, 190), new Vector2(560, 70), font);
        var resume = MakeButton(box, "Resume", "계속하기", new Vector2(0, 70), font);
        var title = MakeButton(box, "SaveAndTitle", "저장하고 타이틀로", new Vector2(0, -40), font);
        var quit = MakeButton(box, "SaveAndQuit", "저장하고 끝내기", new Vector2(0, -150), font);
        Text(box, "Hint", "Esc: 계속하기", 26, new Color(1f, 1f, 1f, 0.55f), new Vector2(0, -225), new Vector2(560, 40), font);

        var menu = root.gameObject.AddComponent<PauseMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("titleButton").objectReferenceValue = title;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create PauseMenu");
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("일시정지 메뉴(6)를 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
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

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font)
    {
        var rt = Box(parent, name, pos, new Vector2(440, 86), Red);
        var button = rt.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.78f, 0.78f, 0.78f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        button.colors = colors;
        Text(rt, "Label", label, 34, Cream, Vector2.zero, new Vector2(440, 86), font);
        return button;
    }
}
