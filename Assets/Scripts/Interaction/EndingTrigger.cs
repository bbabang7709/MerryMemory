using UnityEngine;

/// <summary>
/// 광장 크리스마스트리. 조사하면 (로드맵 2-5, 3-2)
///   - 필요한 조각(requiredPieces)을 모두 모았으면 → 엔딩 시작
///   - 없으면 → notYetDialogue + "카드 조각이 아직 n개 모자라." (4-6: 남은 수를 알려 줌)
///
/// 경우에 따라 대화 대신 엔딩을 틀어야 해서 DialogueInteractable이 아니라
/// IInteractable을 직접 구현합니다.
/// </summary>
public class EndingTrigger : MonoBehaviour, IInteractable
{
    // 4-6부터 조각 5개 전부 (Setup Stage 4 Finish 메뉴가 넣음)
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
            DialogueRunner.Instance?.StartDialogue(BuildNotYetDialogue());
        }
    }

    /// <summary>notYetDialogue 뒤에 남은 조각 수 한 줄을 붙인 대사</summary>
    private DialogueData BuildNotYetDialogue()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        if (notYetDialogue != null) data.lines.AddRange(notYetDialogue.lines);

        int missing = 0;
        if (requiredPieces != null)
            foreach (var piece in requiredPieces)
                if (piece != null && !CardProgress.Has(piece)) missing++;

        data.lines.Add(new DialogueLine { speaker = "", text = $"카드 조각이 아직 {missing}개 모자라." });
        return data;
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
