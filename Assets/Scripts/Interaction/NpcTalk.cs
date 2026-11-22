using UnityEngine;

/// <summary>말을 걸면 대화가 시작되는 사람.</summary>
public class NpcTalk : DialogueInteractable
{
    [Tooltip("처음 말을 걸 때의 대화")]
    [SerializeField] private DialogueData firstDialogue;
    [Tooltip("두 번째부터의 대화. 비워 두면 처음 대화를 반복합니다.")]
    [SerializeField] private DialogueData repeatDialogue;

    private bool talkedOnce;

    protected override string DefaultPrompt => "말 걸기";

    protected override DialogueData GetDialogue() =>
        talkedOnce && repeatDialogue != null ? repeatDialogue : firstDialogue;

    protected override void OnDialogueFinished() => talkedOnce = true;
}
