using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 대화창 표시 담당: 패널 열고 닫기, 타자기 효과, 다음 줄 화살표.
/// 다음에 어떤 줄을 보여 줄지는 DialogueRunner가 정합니다.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject speakerBox;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private RectTransform nextArrow;

    [Header("Typing")]
    [SerializeField, Min(1f)] private float charsPerSecond = 30f;

    [Header("Next Arrow")]
    [SerializeField] private float arrowBobHeight = 6f;
    [SerializeField] private float arrowBobSpeed = 6f;

    public bool IsTyping => typing != null;

    private Coroutine typing;
    private Vector2 arrowBasePos;

    private void Awake()
    {
        if (nextArrow != null) arrowBasePos = nextArrow.anchoredPosition;
        panel.SetActive(false);
    }

    private void Update()
    {
        // 화살표 통통 튀기기 (픽셀 느낌을 위해 정수 단위로)
        if (nextArrow != null && nextArrow.gameObject.activeSelf)
        {
            float offset = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * arrowBobSpeed)) * arrowBobHeight);
            nextArrow.anchoredPosition = arrowBasePos + Vector2.up * offset;
        }
    }

    public void Open() => panel.SetActive(true);

    public void Close()
    {
        StopTyping();
        panel.SetActive(false);
    }

    public void ShowLine(DialogueLine line)
    {
        StopTyping();
        AudioManager.PlaySfx(Sfx.Blip, 0.6f, 0.05f); // 줄마다 "톡" (5-2)

        // {이름}, {이/가} 같은 표기를 화면에 보여 주기 직전에 바꿉니다. (화자 칸에 {이름}을 써도 됨)
        string name = GameSession.Profile.Name;

        bool hasSpeaker = !string.IsNullOrWhiteSpace(line.speaker);
        speakerBox.SetActive(hasSpeaker);
        speakerText.text = hasSpeaker ? TextFormatter.Format(line.speaker, name) : string.Empty;

        bodyText.text = TextFormatter.Format(line.text, name);
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate(); // characterCount를 바로 얻기 위해
        SetArrow(false);

        typing = StartCoroutine(TypeRoutine(bodyText.textInfo.characterCount));
    }

    /// <summary>타자기 효과를 건너뛰고 남은 글자를 한 번에 표시합니다.</summary>
    public void CompleteLine()
    {
        StopTyping();
        FinishLine();
    }

    private IEnumerator TypeRoutine(int total)
    {
        float shown = 0f;
        while (bodyText.maxVisibleCharacters < total)
        {
            shown += Time.unscaledDeltaTime * charsPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        typing = null;
        FinishLine();
    }

    private void FinishLine()
    {
        bodyText.maxVisibleCharacters = int.MaxValue;
        SetArrow(true);
    }

    private void StopTyping()
    {
        if (typing == null) return;
        StopCoroutine(typing);
        typing = null;
    }

    private void SetArrow(bool visible)
    {
        if (nextArrow == null) return;
        nextArrow.gameObject.SetActive(visible);
        nextArrow.anchoredPosition = arrowBasePos;
    }
}
