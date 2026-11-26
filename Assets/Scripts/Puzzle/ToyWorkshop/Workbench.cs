using UnityEngine;

/// <summary>
/// 공방 작업대 (로드맵 4-2). 위에 망가진 팝업 트리 장치가 놓여 있습니다.
///   - 부품을 들고 오면 → 작업대에 올려놓기 (n/3)
///   - 빈손 + 부품이 다 모였으면 → 조립 화면 열기 (PopupAssemblyPuzzle.OpenPuzzle)
///   - 빈손 + 모자라면 → "부품이 아직 n개 모자라."
///   - 다 고쳤으면 → "팝업 트리 장치가 다시 움직인다."
/// </summary>
public class Workbench : MonoBehaviour, IInteractable
{
    [SerializeField] private PopupAssemblyPuzzle puzzle;
    [Tooltip("부품이 아닌 걸 들고 왔을 때")]
    [SerializeField] private DialogueData wrongItemDialogue;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.9f);

    public string Prompt =>
        puzzle.IsSolved ? "살펴보기" :
        CarriedItem.IsHolding ? "올려놓기" :
        puzzle.HasAllParts ? "조립하기" : "살펴보기";

    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (puzzle.IsSolved)
        {
            Say("팝업 트리 장치가 다시 잘 움직인다.");
            return;
        }

        if (CarriedItem.IsHolding)
        {
            if (!puzzle.IsPart(CarriedItem.Current.tag))
            {
                DialogueRunner.Instance?.StartDialogue(wrongItemDialogue);
                return;
            }

            var part = CarriedItem.Take();
            puzzle.AddPart(part);
            Say(puzzle.HasAllParts
                ? $"{part.label}{{을/를}} 작업대에 올려 두었다. ({puzzle.PartsOnBench}/{puzzle.PartsNeeded}) 이제 조립할 수 있겠다!"
                : $"{part.label}{{을/를}} 작업대에 올려 두었다. ({puzzle.PartsOnBench}/{puzzle.PartsNeeded})");
            return;
        }

        if (puzzle.HasAllParts)
        {
            puzzle.OpenPuzzle();
            return;
        }

        int missing = puzzle.PartsNeeded - puzzle.PartsOnBench;
        Say($"망가진 팝업 트리 장치다. 부품이 아직 {missing}개 모자라.");
    }

    private static void Say(string text)
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "", text = text });
        DialogueRunner.Instance?.StartDialogue(data);
    }
}
