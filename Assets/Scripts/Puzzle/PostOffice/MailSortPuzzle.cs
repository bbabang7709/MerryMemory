using UnityEngine;

/// <summary>
/// 우체국 편지 분류 퍼즐 (로드맵 4-1, 튜토리얼). PuzzleBase를 상속해 "규칙"만 씁니다.
///   흩어진 편지(MailLetter)를 모두 맞는 분류함 칸(MailSlot)에 넣으면 풀림.
/// 풀리면 같은 오브젝트의 CardPieceReward가 우표 조각을 주고(3-3),
/// "puzzle_solved:mail_sort" 플래그가 켜져 우체국 출구가 열립니다(4-0 잠긴 문).
/// </summary>
public class MailSortPuzzle : PuzzleBase
{
    [Tooltip("이 퍼즐의 편지들 (Setup 메뉴가 6통을 넣어 둠)")]
    [SerializeField] private MailLetter[] letters;

    private int delivered;

    /// <summary>제자리에 넣은 편지 수</summary>
    public int Delivered => delivered;

    /// <summary>전체 편지 수</summary>
    public int Total => letters.Length;

    /// <summary>분류함이 맞는 편지를 받았을 때 부릅니다. (MailSlot이 부름)</summary>
    public void Deliver(CarryItem item)
    {
        if (IsSolved) return;
        delivered++;
        // 마지막 편지면 진행 대사 대신 바로 풀림 → 보상 대사(CardPieceReward)와 겹치지 않음
        if (delivered >= Total)
        {
            MarkSolved();
        }
        else
        {
            ShowProgress();
        }
    }

    /// <summary>예전에 푼 퍼즐로 복원될 때 (PuzzleBase.Start가 부름): 바닥 편지를 모두 숨김</summary>
    protected override void OnRestoredSolved()
    {
        foreach (var letter in letters)
        {
            if (letter == null) continue;

            letter.gameObject.SetActive(false);
        }
        
        delivered = Total;
    }

    /// <summary>"편지를 넣었다. (n/전체)" 대사를 띄웁니다. (완성된 도우미)</summary>
    private void ShowProgress()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "", text = $"편지를 분류함에 넣었다. ({delivered}/{Total})" });
        DialogueRunner.Instance?.StartDialogue(data);
    }
}
