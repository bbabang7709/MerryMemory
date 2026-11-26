using UnityEngine;

/// <summary>
/// 바닥(선반, 서랍…)에서 [F]로 집어 드는 물건. (로드맵 4-2: 공방 부품, 4-5: 글자 조각)
/// 4-1 MailLetter와 같은 일을 하는 범용판입니다.
///   - 이미 무언가 들고 있으면 집지 않고 alreadyHoldingDialogue
///   - 집으면 이 오브젝트는 꺼지고 손(CarriedItem)에 item이 들림, "○○을/를 집어 들었다."
/// 퍼즐이 풀린 뒤 복원할 때는 퍼즐이 이 오브젝트를 꺼 둡니다.
/// </summary>
public class CarryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private CarryItem item = new();
    [SerializeField] private string prompt = "줍기";
    [SerializeField] private DialogueData alreadyHoldingDialogue;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.7f);

    public CarryItem Item => item;
    public string Prompt => prompt;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        if (!CarriedItem.Hold(item))
        {
            DialogueRunner.Instance?.StartDialogue(alreadyHoldingDialogue);
            return;
        }

        gameObject.SetActive(false);

        // "톱니바퀴를 집어 들었다." — 조사는 TextFormatter가 앞 글자 받침에 맞춤
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines.Add(new DialogueLine { speaker = "", text = $"{item.label}{{을/를}} 집어 들었다." });
        DialogueRunner.Instance?.StartDialogue(data);
    }
}
