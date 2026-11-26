using UnityEngine;

/// <summary>
/// 분류함 한 칸 (위에서부터 시계탑 길 · 공방 길 · 빵집 길 · 눈 언덕). (로드맵 4-1 우체국)
/// 들고 있는 편지의 받는 곳(tag)이 이 칸과 같으면 넣고, 다르면 넣지 않고 대사.
/// 넣은 편지는 퍼즐(MailSortPuzzle)에 알립니다. 정답 개수 세기는 퍼즐이 합니다.
/// </summary>
public class MailSlot : MonoBehaviour, IInteractable
{
    [Tooltip("이 칸이 받는 편지 꼬리표 (예: clocktower)")]
    [SerializeField] private string destinationTag;
    [SerializeField] private MailSortPuzzle puzzle;

    [Header("대사")]
    [Tooltip("빈손으로 조사했을 때")]
    [SerializeField] private DialogueData emptyHandDialogue;
    [Tooltip("다른 칸의 편지를 넣으려 할 때")]
    [SerializeField] private DialogueData wrongDialogue;

    [SerializeField] private Vector2 promptOffset = new(0.9f, 0.3f);

    public string Prompt => CarriedItem.IsHolding ? "넣기" : "살펴보기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    public void Interact(PlayerInteractor interactor)
    {
        // 세 갈래: 빈손 → 안내 / 다른 칸 편지 → 대사만(편지는 손에 그대로) / 맞는 편지 → 꺼내서 퍼즐에 알림
        if (!CarriedItem.IsHolding)
        {
            DialogueRunner.Instance?.StartDialogue(emptyHandDialogue);
            return;
        }

        if (CarriedItem.Current.tag != destinationTag)
        {
            AudioManager.PlaySfx(Sfx.Wrong); // (5-2)
            DialogueRunner.Instance?.StartDialogue(wrongDialogue);
        }
        else
        {
            var item = CarriedItem.Take();
            AudioManager.PlaySfx(Sfx.Stamp); // 분류함에 쏙 (5-2)
            puzzle.Deliver(item);
        }
    }
}
