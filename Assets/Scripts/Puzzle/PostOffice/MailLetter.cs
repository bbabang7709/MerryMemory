using UnityEngine;

/// <summary>
/// 바닥에 흩어진 편지. [F]로 집어 듭니다. (로드맵 4-1 우체국)
/// 이미 무언가 들고 있으면 집지 않고 alreadyHoldingDialogue를 보여 줍니다.
/// 집으면 이 오브젝트는 꺼지고, 손(CarriedItem)에 편지 정보(받는 곳 tag)가 들립니다.
/// </summary>
public class MailLetter : MonoBehaviour, IInteractable
{
    [SerializeField] private CarryItem item = new();
    [SerializeField] private DialogueData alreadyHoldingDialogue;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.7f);

    public CarryItem Item => item;
    public string Prompt => "줍기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (!CarriedItem.Hold(item))
        {
            DialogueRunner.Instance?.StartDialogue(alreadyHoldingDialogue);
            return;
        }

        gameObject.SetActive(false);

        // "편지를 집어 들었다. 받는 곳: 시계탑 길" (즉석 대사)
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "", text = $"편지를 집어 들었다. 받는 곳: {ReceiverOf(item)}" });
        DialogueRunner.Instance?.StartDialogue(data);
    }

    // label "편지 (시계탑 길)"에서 괄호 안만 꺼냄
    private static string ReceiverOf(CarryItem item)
    {
        int open = item.label.IndexOf('(');
        int close = item.label.LastIndexOf(')');
        return open >= 0 && close > open ? item.label.Substring(open + 1, close - open - 1) : item.label;
    }
}
