using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 팝업 트리 장치 조립 화면 (로드맵 4-2). 그리기만 하고 규칙은 PopupAssemblyPuzzle이 판단합니다.
///   - 부품 버튼 3개: 누르면 puzzle.TryInsert(그 부품) → 결과에 따라 칸·메시지·버튼 갱신
///   - 칸 3개: 지금까지 맞게 끼운 부품 이름
///   - Esc: 닫기 (puzzle.ClosePuzzle)
/// 키보드: 방향키로 버튼 고르고 Enter. 마우스 클릭도 됩니다.
/// </summary>
public class PopupAssemblyView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] partButtons;      // 버튼 순서 = partTags 순서
    [SerializeField] private string[] partTags = { "popup_gear", "popup_spring", "popup_board" };
    [SerializeField] private string[] partLabels = { "톱니바퀴", "태엽 스프링", "종이 트리 판" };
    [SerializeField] private TMP_Text[] slotTexts;      // 칸 3개
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0f)] private float successHold = 1.2f;

    private PopupAssemblyPuzzle puzzle;
    private bool finishing;

    private void Awake()
    {
        for (int i = 0; i < partButtons.Length; i++)
        {
            int index = i; // 람다가 i를 그대로 잡으면 마지막 값이 되므로 복사
            partButtons[i].onClick.AddListener(() => OnPartPressed(index));
        }
        panel.SetActive(false);
    }

    public void Open(PopupAssemblyPuzzle target)
    {
        puzzle = target;
        finishing = false;
        panel.SetActive(true);
        messageText.text = "부품을 맞는 순서대로 끼워 보자.";
        Refresh();
        Select(partButtons[0]);
    }

    public void Close()
    {
        panel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (!panel.activeSelf || finishing || puzzle == null) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            puzzle.ClosePuzzle();
            return;
        }

        // 마우스로 빈 곳을 눌러 선택이 풀리면 다시 고를 수 있게
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            Select(FirstInteractable());
    }

    private void OnPartPressed(int index)
    {
        if (puzzle == null || finishing) return;

        switch (puzzle.TryInsert(partTags[index]))
        {
            case PopupAssemblyPuzzle.InsertResult.Correct:
                messageText.text = $"{TextFormatter.AttachParticle(partLabels[index], "을/를")} 끼웠다. 딸깍!";
                break;
            case PopupAssemblyPuzzle.InsertResult.Wrong:
                messageText.text = "덜컥! 순서가 맞지 않는 것 같다. 처음부터 다시.";
                break;
            case PopupAssemblyPuzzle.InsertResult.Completed:
                messageText.text = "팝업 트리가 솟아올랐다!";
                StartCoroutine(FinishAfterDelay());
                break;
        }
        Refresh();
        Select(FirstInteractable());
    }

    private IEnumerator FinishAfterDelay()
    {
        finishing = true;
        foreach (var b in partButtons) b.interactable = false;
        yield return new WaitForSecondsRealtime(successHold);
        puzzle.Finish();
    }

    /// <summary>칸과 버튼을 puzzle.Inserted 상태대로 그립니다.</summary>
    private void Refresh()
    {
        var inserted = puzzle.Inserted;
        for (int i = 0; i < slotTexts.Length; i++)
            slotTexts[i].text = i < inserted.Count ? LabelOf(inserted[i]) : $"{i + 1}";

        // 이미 끼운 부품 버튼은 못 누르게
        for (int i = 0; i < partButtons.Length; i++)
            partButtons[i].interactable = !finishing && !Contains(inserted, partTags[i]);
    }

    private string LabelOf(string tag)
    {
        int i = System.Array.IndexOf(partTags, tag);
        return i >= 0 ? partLabels[i] : tag;
    }

    private static bool Contains(System.Collections.Generic.IReadOnlyList<string> list, string tag)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == tag) return true;
        return false;
    }

    private Selectable FirstInteractable()
    {
        foreach (var b in partButtons) if (b.interactable) return b;
        return partButtons.Length > 0 ? partButtons[0] : null;
    }

    private static void Select(Selectable s)
    {
        if (s != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
