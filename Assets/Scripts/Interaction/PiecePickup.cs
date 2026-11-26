using UnityEngine;

/// <summary>
/// 조사하면 대사가 나오고, 대사가 끝나면 카드 조각을 얻는 물건. (로드맵 2-5, 임시 조각)
/// 4단계에서 조각마다 퍼즐(PuzzleBase + CardPieceReward)로 바뀔 수 있습니다.
///
/// "[F] 줍기" 안내와 대화 재생은 부모(DialogueInteractable)가 하고,
/// 여기서는 대화가 끝났을 때 조각을 CardProgress에 기록하고 물건을 치웁니다.
/// </summary>
public class PiecePickup : DialogueInteractable
{
    [Tooltip("주우면 얻는 조각 (CardPieceData 에셋)")]
    [SerializeField] private CardPieceData piece;

    [Tooltip("주웠을 때 나오는 대사")]
    [SerializeField] private DialogueData foundDialogue;

    protected override string DefaultPrompt => "줍기";
    protected override DialogueData GetDialogue() => foundDialogue;

    protected override void OnDialogueFinished()
    {
        CardProgress.Collect(piece);

        // Destroy 대신 끄기만 합니다. 나중에 "다시 하기" 같은 기능으로 되살릴 수 있게.
        gameObject.SetActive(false);
    }
}
