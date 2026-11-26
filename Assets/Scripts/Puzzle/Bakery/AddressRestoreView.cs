using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 번진 봉투 주소 맞추기 화면 (로드맵 4-5). 그리기만 하고 규칙은 AddressRestorePuzzle이 판단합니다.
///   - 칸 버튼 3개: 누르면 puzzle.CycleSlot(칸) → 글자가 다음 스티커로 바뀜
///   - [확인]: 빈칸이 있으면 안내, 맞으면 성공 메시지 후 puzzle.Finish, 틀리면 다시 맞춰 보라는 메시지
///   - Esc: 닫기
/// 키보드: 방향키로 고르고 Enter. 마우스 클릭도 됩니다. (씬에 EventSystem 필요)
/// </summary>
public class AddressRestoreView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TMP_Text[] slotTexts;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text lettersText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0f)] private float successHold = 2.2f;

    private AddressRestorePuzzle puzzle;
    private bool finishing;

    private void Awake()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i; // 람다가 i를 그대로 잡지 않게 복사
            slotButtons[i].onClick.AddListener(() => OnSlotPressed(index));
        }
        submitButton.onClick.AddListener(OnSubmit);
        panel.SetActive(false);
    }

    public void Open(AddressRestorePuzzle target)
    {
        puzzle = target;
        finishing = false;
        panel.SetActive(true);
        lettersText.text = $"가진 글자 스티커:   {puzzle.LettersSummary}";
        messageText.text = "칸을 누를 때마다 글자가 바뀐다. 맞춰 보고 [확인]!";
        SetInteractable(true);
        Refresh();
        Select(slotButtons[0]);
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
            Select(slotButtons[0]);
    }

    private void OnSlotPressed(int slot)
    {
        if (puzzle == null || finishing) return;
        puzzle.CycleSlot(slot);
        Refresh();
    }

    private void OnSubmit()
    {
        if (puzzle == null || finishing) return;

        if (puzzle.HasEmptySlot())
        {
            messageText.text = "아직 빈칸이 있다.";
        }
        else if (puzzle.IsCorrect())
        {
            messageText.text = "주소 일부가 읽힌다... \"To. ?? 너머의 ?에게\"";
            StartCoroutine(FinishAfterDelay());
        }
        else
        {
            messageText.text = "뭔가 어색하다. 글자 순서를 바꿔 보자.";
        }
    }

    private IEnumerator FinishAfterDelay()
    {
        finishing = true;
        SetInteractable(false);
        yield return new WaitForSecondsRealtime(successHold);
        puzzle.Finish();
    }

    private void Refresh()
    {
        for (int i = 0; i < slotTexts.Length; i++) slotTexts[i].text = puzzle.LetterAt(i);
    }

    private void SetInteractable(bool on)
    {
        foreach (var b in slotButtons) b.interactable = on;
        submitButton.interactable = on;
    }

    private static void Select(Selectable s)
    {
        if (s != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
