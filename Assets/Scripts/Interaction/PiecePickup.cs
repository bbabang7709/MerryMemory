using UnityEngine;

/// <summary>
/// 조사하면 대사가 나오고, 대사가 끝나면 카드 조각을 얻는 물건. (로드맵 2-5, 임시 조각)
/// 4단계에서 조각마다 퍼즐(PuzzleBase + CardPieceReward)로 바뀔 수 있습니다.
///
/// "[F] 줍기" 안내와 대화 재생은 부모(DialogueInteractable)가 하고,
/// 여기서는 대화가 끝났을 때 조각을 CardProgress에 기록하고 물건을 치웁니다.
/// 이미 가진 조각이면(이어하기, 씬 다시 열기) 시작할 때 스스로 꺼집니다. (3-4 퍼즐 복원과 같은 개념)
/// </summary>
public class PiecePickup : DialogueInteractable
{
    [Tooltip("주우면 얻는 조각 (CardPieceData 에셋)")]
    [SerializeField] private CardPieceData piece;

    [Tooltip("주웠을 때 나오는 대사")]
    [SerializeField] private DialogueData foundDialogue;

    protected override string DefaultPrompt => "줍기";
    protected override DialogueData GetDialogue() => foundDialogue;

    // 씬이 열릴 때 장부에 물어보고, 이미 가진 조각이면 바닥에 다시 놓이지 않게 끔
    private void Start()
    {
        if (CardProgress.Has(piece)) gameObject.SetActive(false);
    }

    protected override void OnDialogueFinished()
    {
        CardProgress.Collect(piece);

        // Destroy 대신 끄기만 합니다. 나중에 "다시 하기" 같은 기능으로 되살릴 수 있게.
        gameObject.SetActive(false);
    }
}
