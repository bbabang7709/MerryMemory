using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Merry Memory → Setup Card Progress UI  (Village 씬에서 실행, 로드맵 3-2)
/// UICanvas 오른쪽 위에 조각 칸 5개와 "0 / 5" 글자를 만들고,
/// CardProgressView에 칸과 조각 에셋(Assets/ScriptableObjects/CardPieces, number 순)을 연결합니다.
/// </summary>
public static class CardProgressSetupMenu
{
    private const string PieceFolder = "Assets/ScriptableObjects/CardPieces";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const float SlotSize = 64f;
    private const float Gap = 12f;

    [MenuItem("Merry Memory/Setup Card Progress UI")]
    private static void Setup()
    {
        var canvas = GameObject.Find("UICanvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Setup Card Progress UI", "UICanvas가 없습니다. Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (Object.FindFirstObjectByType<CardProgressView>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Setup Card Progress UI", "씬에 이미 CardProgressView가 있습니다.", "확인");
            return;
        }

        var pieces = AssetDatabase.FindAssets("t:CardPieceData", new[] { PieceFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<CardPieceData>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(p => p.number)
            .ToArray();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        Undo.SetCurrentGroupName("Setup Card Progress UI");
        int group = Undo.GetCurrentGroup();

        // ── 오른쪽 위 판 ──
        var root = CreateUI("CardProgressView", canvas.transform);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create CardProgressView");
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        float width = pieces.Length * SlotSize + (pieces.Length - 1) * Gap + 40f;
        root.sizeDelta = new Vector2(width, SlotSize + 76f);
        root.anchoredPosition = new Vector2(-32f, -32f);
        root.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.17f, 0.75f);

        // 엔딩의 어두운 판이 이 UI도 덮도록 EndingView보다 앞 형제로
        var endingView = canvas.transform.Find("EndingView");
        if (endingView != null) root.SetSiblingIndex(endingView.GetSiblingIndex());

        // 제목 + 개수
        var title = Text(root, "Title", "카드 조각", 24, new Vector2(20f, -12f), new Vector2(200f, 32f), font, TextAlignmentOptions.TopLeft);
        title.color = new Color(0.96f, 0.95f, 0.9f, 0.8f);
        var count = Text(root, "Count", $"0 / {CardProgress.TotalPieces}", 26, new Vector2(-20f, -12f), new Vector2(140f, 32f), font, TextAlignmentOptions.TopRight);
        var countRT = count.rectTransform;
        countRT.anchorMin = countRT.anchorMax = countRT.pivot = new Vector2(1f, 1f);

        // ── 칸 5개 ──
        var slots = new Image[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            var slot = CreateUI($"Slot_{pieces[i].number}_{pieces[i].id}", root);
            slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0f, 0f);
            slot.sizeDelta = new Vector2(SlotSize, SlotSize);
            slot.anchoredPosition = new Vector2(20f + i * (SlotSize + Gap), 16f);
            var img = slot.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
            img.preserveAspect = true;
            slots[i] = img;

            var num = Text(slot, "Number", pieces[i].number.ToString(), 22, Vector2.zero, new Vector2(SlotSize, SlotSize), font, TextAlignmentOptions.Center);
            num.color = new Color(1f, 1f, 1f, 0.5f);
        }
        foreach (var g in root.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;

        // ── 연결 ──
        var view = root.gameObject.AddComponent<CardProgressView>();
        var so = new SerializedObject(view);
        var pieceProp = so.FindProperty("pieces");
        var slotProp = so.FindProperty("slots");
        pieceProp.arraySize = slotProp.arraySize = pieces.Length;
        for (int i = 0; i < pieces.Length; i++)
        {
            pieceProp.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            slotProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        so.FindProperty("countText").objectReferenceValue = count;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"조각 현황 UI를 만들었습니다 (칸 {pieces.Length}개). 씬을 저장하세요 (Ctrl+S).");
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Vector2 pos, Vector2 box,
        TMP_FontAsset font, TextAlignmentOptions align)
    {
        var rt = CreateUI(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = new Color(0.96f, 0.95f, 0.9f, 1f);
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }
}
