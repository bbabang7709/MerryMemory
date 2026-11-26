using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엔딩의 카드 화면 (로드맵 5-4, GDD 3.4). 그리기만 하고 순서는 EndingSequence가 정합니다.
///   ① 조립: 빈 칸 5개 → Place(i)로 조각을 하나씩 붙임 (칸 색·이름이 채워지고 톡 튀어 오름)
///   ② 주소: RevealAddress → "To. 화면 너머의 너에게"가 한 글자씩
///   ③ 펼침: Open → 칸은 사라지고 주소는 위로, ShowMessage(i)로 마을 사람들의 한마디가 하나씩 떠오름
/// 조각 그림(icon)이 아직 없어서 칸은 조각마다 다른 색 + 이름으로 그립니다.
/// </summary>
public class EndingCardView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;

    [Header("조립")]
    [SerializeField] private CanvasGroup slotsGroup;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private TMP_Text[] slotLabels;
    [SerializeField] private TMP_Text hintText;
    [Tooltip("조각 번호(1~5) 순서의 칸 색 (조각 그림이 생기기 전 임시)")]
    [SerializeField] private Color[] pieceColors;

    [Header("주소")]
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private Vector2 addressOpenedPosition = new(0f, 250f);

    [Header("펼친 카드")]
    [SerializeField] private CanvasGroup messagesGroup;
    [SerializeField] private TMP_Text[] messageTexts;
    [SerializeField] private string makerColor = "#B03A2E";

    private void Awake()
    {
        group.alpha = 0f;
        messagesGroup.alpha = 0f;
        foreach (var t in messageTexts) t.alpha = 0f;
        addressText.maxVisibleCharacters = 0;
        hintText.text = "";
    }

    public IEnumerator Show(float seconds) => Fade(group, 1f, seconds);
    public IEnumerator Hide(float seconds) => Fade(group, 0f, seconds);

    public void SetHint(string text) => hintText.text = text;

    /// <summary>i번째 칸(0부터)에 조각을 붙입니다.</summary>
    public void Place(int index, CardPieceData piece)
    {
        if (index < 0 || index >= slotImages.Length) return;
        slotImages[index].color = index < pieceColors.Length ? pieceColors[index] : Color.white;
        slotLabels[index].text = piece != null ? piece.displayName : "";
        StartCoroutine(Pop(slotImages[index].rectTransform));
    }

    /// <summary>주소를 한 글자씩 보여 줍니다.</summary>
    public IEnumerator RevealAddress(string text, float charsPerSecond)
    {
        addressText.text = text;
        addressText.maxVisibleCharacters = 0;
        addressText.ForceMeshUpdate();
        int total = addressText.textInfo.characterCount;
        for (float shown = 0f; shown < total; shown += Time.unscaledDeltaTime * charsPerSecond)
        {
            addressText.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        addressText.maxVisibleCharacters = total;
    }

    /// <summary>카드를 펼침: 조립 칸은 사라지고 주소는 위로 올라감 (한마디가 들어갈 자리)</summary>
    public IEnumerator Open(float seconds)
    {
        hintText.text = "";
        var address = addressText.rectTransform;
        Vector2 from = address.anchoredPosition;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            slotsGroup.alpha = 1f - k;
            address.anchoredPosition = Vector2.Lerp(from, addressOpenedPosition, k);
            yield return null;
        }
        slotsGroup.alpha = 0f;
        address.anchoredPosition = addressOpenedPosition;
        messagesGroup.alpha = 1f;
    }

    /// <summary>i번째 한마디를 서서히 띄웁니다. "만든 사람   한마디"</summary>
    public IEnumerator ShowMessage(int index, CardPieceData piece, float seconds)
    {
        if (index < 0 || index >= messageTexts.Length || piece == null) yield break;
        var text = messageTexts[index];
        text.text = $"<color={makerColor}>{piece.makerName}</color>   {piece.message}";
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            text.alpha = t / seconds;
            yield return null;
        }
        text.alpha = 1f;
    }

    private static IEnumerator Pop(RectTransform rt)
    {
        const float duration = 0.25f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float s = 1f + Mathf.Sin(t / duration * Mathf.PI) * 0.15f; // 1 → 1.15 → 1
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private static IEnumerator Fade(CanvasGroup g, float target, float seconds)
    {
        float start = g.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            g.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        g.alpha = target;
    }
}
