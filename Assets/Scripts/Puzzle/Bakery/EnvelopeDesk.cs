using UnityEngine;

/// <summary>
/// 빵집 작업대: 주소가 번진 봉투가 놓여 있습니다. (로드맵 4-5)
///   - 스티커가 모자라면 → "글자 스티커가 n장 더 필요해."
///   - 다 모았으면 → 주소 맞추기 화면 (AddressRestorePuzzle.OpenPuzzle)
///   - 풀었으면 → 살펴보기 한 줄
/// </summary>
public class EnvelopeDesk : MonoBehaviour, IInteractable
{
    [SerializeField] private AddressRestorePuzzle puzzle;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.9f);

    public string Prompt => !puzzle.IsSolved && puzzle.HasAllFragments ? "맞춰 보기" : "살펴보기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (puzzle.IsSolved)
        {
            Say("봉투는 카드와 함께 잘 챙겼다. 작업대에 밀가루 자국만 남아 있다.");
            return;
        }

        if (puzzle.HasAllFragments)
        {
            puzzle.OpenPuzzle();
            return;
        }

        int missing = puzzle.FragmentCount - puzzle.CollectedCount;
        Say($"주소가 번진 봉투다. \"To. ?? ??의 ?에?\"... 글자 스티커가 {missing}장 더 필요해.");
    }

    private static void Say(string text)
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "", text = text });
        DialogueRunner.Instance?.StartDialogue(data);
    }
}
