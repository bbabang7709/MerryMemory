using UnityEngine;

/// <summary>
/// 말을 걸면 대화가 시작되는 사람.
/// 처음엔 firstDialogue, 한 번 말을 건 뒤에는 repeatDialogue를 재생합니다.
/// (4-6) solvedFlag가 켜져 있으면(예: 그 사람의 퍼즐을 풀었으면) solvedDialogue가 가장 먼저입니다.
///   → 조각 2~5를 어떤 순서로 풀어도, 이미 도와준 사람이 "도와줘"라고 말하지 않게
///
/// "말을 걸었나"는 talkedFlag 키가 있으면 GameFlags(씬 밖 장부)에 기억해서 씬이 바뀌어도 남고,
/// 키가 없으면 이 오브젝트의 변수(talkedOnce)에만 기억합니다. (씬이 다시 열리면 잊음)
/// </summary>
public class NpcTalk : DialogueInteractable
{
    [Tooltip("처음 말을 걸 때의 대화")]
    [SerializeField] private DialogueData firstDialogue;
    [Tooltip("두 번째부터의 대화. 비워 두면 처음 대화를 반복합니다.")]
    [SerializeField] private DialogueData repeatDialogue;

    [Tooltip("\"말을 걸었다\"를 기억할 플래그 키 (FlagKeys 참고). 비워 두면 씬이 바뀌면 잊어버립니다.")]
    [SerializeField] private string talkedFlag;

    [Header("퍼즐을 푼 뒤 (4-6)")]
    [Tooltip("이 플래그가 켜져 있으면 solvedDialogue를 재생. 예: puzzle_solved:bell_sequence")]
    [SerializeField] private string solvedFlag;
    [Tooltip("solvedFlag가 켜진 뒤의 대화 (고마움 등)")]
    [SerializeField] private DialogueData solvedDialogue;

    private bool talkedOnce;

    protected override string DefaultPrompt => "말 걸기";

    /// <summary>전에 말을 걸었는지. 플래그 키가 있으면 플래그, 없으면 talkedOnce</summary>
    protected bool TalkedBefore => !string.IsNullOrEmpty(talkedFlag) ? GameFlags.Has(talkedFlag) : talkedOnce;

    protected override DialogueData GetDialogue()
    {
        // 우선순위: 퍼즐을 푼 뒤 > 두 번째부터 > 처음
        if (!string.IsNullOrEmpty(solvedFlag) && GameFlags.Has(solvedFlag) && solvedDialogue != null)
            return solvedDialogue;

        return TalkedBefore && repeatDialogue != null ? repeatDialogue : firstDialogue;
    }

    protected override void OnDialogueFinished()
    {
        talkedOnce = true;
        if (!string.IsNullOrEmpty(talkedFlag))
        {
            GameFlags.Set(talkedFlag);
        }
    }
}
