using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 크레딧 (로드맵 5-5, GDD 3.5). 카드의 "From." 칸처럼, 보낸 사람들의 이름이 한 줄씩 적힙니다.
///   From.
///   (조각을 만든 마을 사람들: CardPieceData.makerName)
///   그리고 마을 사람들 모두
///   임시 집배원 {이름}
///   이 게임을 만든 사람 (creatorName)
///   + 마지막 인사
/// 다 적히면 아무 키나 누르거나 잠시 기다리면 끝납니다. EndingSequence가 MERRY CHRISTMAS 뒤에 부릅니다.
/// </summary>
public class EndingCredits : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text header;
    [Tooltip("이름 줄들 (위에서부터). 줄 수보다 적게 쓰면 남는 줄은 비워 둠")]
    [SerializeField] private TMP_Text[] lines;
    [SerializeField] private TMP_Text farewell;

    [Header("내용")]
    [Tooltip("크레딧에 적힐 제작자 이름")]
    [SerializeField] private string creatorName = "bbabang7709";
    [SerializeField] private string farewellText = "함께 배달해 줘서 고마워요. 메리 크리스마스!";

    [Header("시간 (초)")]
    [SerializeField, Min(0f)] private float fade = 0.8f;
    [SerializeField, Min(0f)] private float lineInterval = 0.9f;
    [SerializeField, Min(0f)] private float autoClose = 8f;

    private void Awake()
    {
        group.alpha = 0f;
        header.alpha = 0f;
        farewell.alpha = 0f;
        foreach (var t in lines) t.alpha = 0f;
    }

    /// <summary>크레딧을 보여 주고, 끝까지 본 뒤(키 입력 또는 autoClose초) 돌아옵니다.</summary>
    public IEnumerator Play(IEnumerable<CardPieceData> pieces)
    {
        var names = new List<string>();
        foreach (var p in pieces)
            if (p != null && !string.IsNullOrEmpty(p.makerName) && !names.Contains(p.makerName)) names.Add(p.makerName);
        names.Add("그리고 마을 사람들 모두");
        names.Add(TextFormatter.Format("임시 집배원 {이름}", GameSession.Profile.Name));
        names.Add($"이 게임을 만든 {creatorName}");

        for (int i = 0; i < lines.Length; i++) lines[i].text = i < names.Count ? names[i] : "";
        farewell.text = farewellText;

        yield return Fade(group, 1f, fade);
        yield return Fade(header, 1f, fade);
        for (int i = 0; i < lines.Length && i < names.Count; i++)
        {
            yield return new WaitForSeconds(lineInterval);
            yield return Fade(lines[i], 1f, fade * 0.6f);
        }
        yield return new WaitForSeconds(lineInterval);
        yield return Fade(farewell, 1f, fade);

        // 아무 키나 누르거나 autoClose초가 지나면 끝
        yield return null;
        for (float t = 0f; t < autoClose; t += Time.unscaledDeltaTime)
        {
            if (AnyPressed()) break;
            yield return null;
        }
    }

    private static bool AnyPressed() =>
        (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

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

    private static IEnumerator Fade(TMP_Text text, float target, float seconds)
    {
        float start = text.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            text.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        text.alpha = target;
    }
}
