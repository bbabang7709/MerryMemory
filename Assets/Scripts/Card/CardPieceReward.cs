using System.Collections;
using UnityEngine;

/// <summary>
/// 퍼즐이 풀리면 카드 조각을 줍니다. (로드맵 3-3, GDD 5.2 조각 획득 연출)
///   ① "○○ 조각을 얻었다!" + 만든 사람이 카드에 한마디를 적음 (내용은 엔딩까지 비밀)
///   ② 받는 사람 단서 대사 (piece.clueDialogue)
///   ③ CardProgress에 기록 → 현황 UI 칸이 채워짐
///
/// 퍼즐은 이 스크립트를 모릅니다. 이 스크립트가 퍼즐의 Solved 이벤트를 듣습니다.
/// 같은 오브젝트(또는 아무 곳)에 붙이고 Inspector에서 puzzle과 piece를 연결합니다.
/// </summary>
public class CardPieceReward : MonoBehaviour
{
    [SerializeField] private PuzzleBase puzzle;
    [SerializeField] private CardPieceData piece;

    private void OnEnable()
    {
        if (puzzle != null)
            puzzle.Solved += OnSolved;
    }

    private void OnDisable()
    {
        if (puzzle != null)
            puzzle.Solved -= OnSolved;
    }

    private void OnSolved(PuzzleBase solved)
    {
        if (piece == null)
        {
            Debug.LogWarning($"{name}: 줄 조각(piece)이 연결되어 있지 않습니다.", this);
            return;
        }
        StartCoroutine(GiveReward());
    }

    private IEnumerator GiveReward()
    {
        // 보상 연출 동안은 대사와 대사 사이에도 움직이지 못하게 잠급니다.
        GameplayLock.Acquire(this);

        yield return PlayDialogue(BuildFoundDialogue()); // ① 획득 알림 + 한마디
        yield return PlayDialogue(piece.clueDialogue);   // ② 받는 사람 단서
        CardProgress.Collect(piece);                     // ③ 기록 → 현황 UI

        GameplayLock.Release(this);
    }

    /// <summary>
    /// 대사를 재생하고 끝날 때까지 기다립니다.
    /// 시작하지 못하면(Runner 없음, 대사 없음·비어 있음, 다른 대화 중) 기다리지 않고 바로 끝냅니다.
    /// → 콜백이 영영 안 불려서 잠긴 채로 멈추는 일을 막음
    /// </summary>
    private IEnumerator PlayDialogue(DialogueData dialogue)
    {
        var runner = DialogueRunner.Instance;
        if (runner == null || dialogue == null) yield break;

        bool done = false;
        if (!runner.StartDialogue(dialogue, () => done = true)) yield break;

        yield return new WaitUntil(() => done);
    }

    /// <summary>"○○ 조각을 얻었다!" 대사를 즉석에서 만듭니다.</summary>
    private DialogueData BuildFoundDialogue()
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();
        // 조사({을/를})는 바로 앞 글자를 보므로 이름 뒤에 괄호를 붙이지 않음
        data.lines.Add(new DialogueLine { speaker = "", text = $"카드 조각 {piece.displayName}{{을/를}} 얻었다!" });
        if (!string.IsNullOrEmpty(piece.makerName))
            data.lines.Add(new DialogueLine { speaker = "", text = $"{piece.makerName}{{이/가}} 카드에 무언가를 적어 넣었다. (엔딩에서 볼 수 있다)" });
        return data;
    }
}
