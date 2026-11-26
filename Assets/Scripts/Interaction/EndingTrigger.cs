using UnityEngine;

/// <summary>
/// 광장 크리스마스트리. 조사하면 (로드맵 2-5, 3-2)
///   - 필요한 조각(requiredPieces)을 모두 모았으면 → 엔딩 시작
///   - 없으면 → "조각이 아직 모자라." 대사
///
/// 경우에 따라 대화 대신 엔딩을 틀어야 해서 DialogueInteractable이 아니라
/// IInteractable을 직접 구현합니다.
/// </summary>
public class EndingTrigger : MonoBehaviour, IInteractable
{
    // 지금 마을에는 우표 조각만 있어서 1개(Piece_1_Stamp)만 넣어 둠. 4단계에서 조각이 다 생기면 5개로.
    [Tooltip("엔딩에 필요한 조각들. 모두 모아야 엔딩이 열립니다.")]
    [SerializeField] private CardPieceData[] requiredPieces;

    [SerializeField] private DialogueData notYetDialogue;
    [SerializeField] private EndingSequence ending;

    [Tooltip("안내 UI 위치 (이 오브젝트 기준)")]
    [SerializeField] private Vector2 promptOffset = new(0f, 3f);

    public string Prompt => "조사하기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (HasAllRequiredPieces())
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

    /// <summary>requiredPieces를 모두 모았는지.</summary>
    private bool HasAllRequiredPieces()
    {
        // 목록이 비어 있으면 설정 실수로 보고 false. (조각 0개로 엔딩이 열리는 사고 방지)
        if (requiredPieces == null || requiredPieces.Length <= 0) return false;

        foreach (var piece in requiredPieces)
        {
            if (!CardProgress.Has(piece)) return false;
        }

        return true;
    }
}
