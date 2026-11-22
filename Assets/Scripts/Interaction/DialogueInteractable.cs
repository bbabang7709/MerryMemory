using UnityEngine;

/// <summary>
/// 상호작용하면 대화를 재생하는 대상의 공통 부분.
/// Inspectable(사물), NpcTalk(사람)이 이 클래스를 상속합니다.
/// </summary>
public abstract class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt;
    [Tooltip("안내 UI 위치 (이 오브젝트 기준, 보통 머리 위)")]
    [SerializeField] private Vector2 promptOffset = new(0f, 1.6f);

    public string Prompt => string.IsNullOrEmpty(prompt) ? DefaultPrompt : prompt;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    /// <summary>Inspector에서 문구를 비워 두면 쓰는 기본 문구</summary>
    protected abstract string DefaultPrompt { get; }

    /// <summary>이번에 재생할 대화</summary>
    protected abstract DialogueData GetDialogue();

    /// <summary>대화가 끝난 뒤 호출됩니다.</summary>
    protected virtual void OnDialogueFinished() { }

    public void Interact(PlayerInteractor interactor)
    {
        DialogueRunner runner = DialogueRunner.Instance;
        if (runner == null)
        {
            Debug.LogWarning("씬에 DialogueRunner가 없습니다.", this);
            return;
        }

        DialogueData dialogue = GetDialogue();
        if (dialogue == null)
        {
            Debug.LogWarning($"{name}: 재생할 대화가 지정되지 않았습니다.", this);
            return;
        }

        runner.StartDialogue(dialogue, OnDialogueFinished);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PromptPosition, 0.1f);
    }
}
