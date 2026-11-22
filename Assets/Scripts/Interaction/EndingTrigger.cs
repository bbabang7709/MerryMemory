using UnityEngine;

/// <summary>
/// 광장 크리스마스트리. 조사하면 (로드맵 2-5)
///   - 필요한 조각이 있으면 → 엔딩 시작
///   - 없으면 → "조각이 아직 모자라." 대사
///
/// 경우에 따라 대화 대신 엔딩을 틀어야 해서 DialogueInteractable이 아니라
/// IInteractable을 직접 구현합니다.
/// </summary>
public class EndingTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("엔딩에 필요한 조각 ID (2단계는 1개. 3단계에서 5개로)")]
    [SerializeField] private string requiredPieceId = "stamp";

    [SerializeField] private DialogueData notYetDialogue;
    [SerializeField] private EndingSequence ending;

    [Tooltip("안내 UI 위치 (이 오브젝트 기준)")]
    [SerializeField] private Vector2 promptOffset = new(0f, 3f);

    public string Prompt => "조사하기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (GameSession.HasPiece(requiredPieceId))
        {
            // Inspector 연결을 빠뜨렸으면 경고만 남기고 멈춤 (클릭하면 이 오브젝트가 선택됨)
            if (ending == null)
            {
                Debug.LogWarning("EndingSequence가 연결되어 있지 않습니다.", this);
                return;
            }

            ending.Play();
        }
        else
        {
            DialogueRunner.Instance?.StartDialogue(notYetDialogue);
        }
    }
}
