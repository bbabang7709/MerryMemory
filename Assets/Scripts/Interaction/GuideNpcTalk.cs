using System;
using UnityEngine;

/// <summary>
/// 진행 상황에 맞춰 말이 바뀌는 안내역 (로드맵 4-6, 우체국장).
///   - 처음 → firstDialogue (프롤로그)
///   - 튜토리얼(guideAfterFlag)을 끝내기 전 → repeatDialogue (분류 요령)
///   - 그 뒤 → BuildGuide(): 모은 조각 수에 따른 한마디 + 아직 못 찾은 조각마다 힌트 한 줄
///     힌트는 hints 배열 순서대로, 아직 못 모은 조각 중 첫 번째 하나만 알려 줌 (한 번에 다 말하지 않게)
///     "막히면 우체국장에게 물어보면 된다" = 로드맵 4단계 완료 기준 "막히면 힌트가 나온다"
/// </summary>
public class GuideNpcTalk : NpcTalk
{
    [Serializable]
    public class PieceHint
    {
        public CardPieceData piece;
        [TextArea(1, 3)] public string line;
    }

    [Header("안내 (4-6)")]
    [Tooltip("이 플래그가 켜진 뒤부터 안내 모드. 예: puzzle_solved:mail_sort")]
    [SerializeField] private string guideAfterFlag;
    [SerializeField] private string speaker = "우체국장";
    [Tooltip("아직 못 찾은 조각마다 말해 줄 힌트")]
    [SerializeField] private PieceHint[] hints;

    protected override DialogueData GetDialogue()
    {
        // 처음 대화(프롤로그)와 튜토리얼 중에는 원래 NpcTalk 규칙 그대로
        if (!TalkedBefore || string.IsNullOrEmpty(guideAfterFlag) || !GameFlags.Has(guideAfterFlag))
            return base.GetDialogue();

        return BuildGuide();
    }

    /// <summary>지금 진행 상황으로 안내 대사를 즉석에서 만듭니다.</summary>
    private DialogueData BuildGuide()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        int count = CardProgress.Count;

        // 다 모았으면 트리로 안내
        if (count >= CardProgress.TotalPieces)
        {
            Say(data, "조각 다섯 개를 다 모았구나! 광장의 크리스마스트리로 가 보렴.");
            return data;
        }
        else
        {
            // 첫 줄: 모은 수에 따라
            if (count >= 2) Say(data, $"벌써 {count}개나 모았구나!");
            else Say(data, "조각은 마을 곳곳에 흩어진 것 같아.");

            // 아직 못 모은 조각 중 첫 번째 힌트 하나
            foreach (var hint in hints)
            {
                if (hint == null || hint.piece == null) continue;

                if (!CardProgress.Has(hint.piece))
                {
                    Say(data, hint.line);
                    break;
                }
            }
        }

        return data;
    }

    private void Say(DialogueData data, string text) =>
        data.lines.Add(new DialogueLine { speaker = speaker, text = text });
}
