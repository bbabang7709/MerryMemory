using UnityEngine;

/// <summary>
/// 조사하면 대사가 나오고, 대사가 끝나면 카드 조각을 얻는 물건. (로드맵 2-5, 임시 조각)
/// 3단계에서는 퍼즐을 풀어야 얻게 바뀝니다.
///
/// "[F] 줍기" 안내와 대화 재생은 부모(DialogueInteractable)가 하고,
/// 여기서는 대화가 끝났을 때 조각을 기록하고 물건을 치웁니다.
/// </summary>
public class PiecePickup : DialogueInteractable
{
    [Tooltip("GameSession에 기록할 조각 ID. 트리(EndingTrigger)의 ID와 같아야 합니다.")]
    [SerializeField] private string pieceId = "stamp";

    [Tooltip("주웠을 때 나오는 대사")]
    [SerializeField] private DialogueData foundDialogue;

    protected override string DefaultPrompt => "줍기";
    protected override DialogueData GetDialogue() => foundDialogue;

    protected override void OnDialogueFinished()
    {
        GameSession.CollectPiece(pieceId);

        // Destroy 대신 끄기만 합니다. 3단계의 "다시 하기"에서 되살릴 수 있게.
        gameObject.SetActive(false);
    }
}
