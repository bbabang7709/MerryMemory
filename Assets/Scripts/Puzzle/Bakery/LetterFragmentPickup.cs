using UnityEngine;

/// <summary>
/// 마을 곳곳에 날아간 글자 스티커 한 장 (로드맵 4-5, MAP F1~F3).
/// 손에 드는 물건(CarriedItem)이 아니라 "주웠다" 플래그로 기억합니다.
///   → 세 장을 한 번에 모아 다닐 수 있고, 저장(SaveData.flags)에도 자동으로 들어감
/// 이미 주운 스티커는 씬이 열릴 때 스스로 숨습니다.
/// </summary>
public class LetterFragmentPickup : MonoBehaviour, IInteractable
{
    [Tooltip("저장용 고유 키. 예: f1")]
    [SerializeField] private string fragmentId;
    [Tooltip("스티커에 적힌 글자 한 자")]
    [SerializeField] private string letter;
    [Tooltip("주울 때 첫 줄. 예: 게시판 전단 귀퉁이에 글자 스티커가 붙어 있다.")]
    [SerializeField] private string foundText;
    [SerializeField] private AddressRestorePuzzle puzzle;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.7f);

    public string Letter => letter;
    public string FlagKey => FlagKeys.LetterFragment(fragmentId);
    public bool IsCollected => GameFlags.Has(FlagKey);

    public string Prompt => "줍기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Start()
    {
        if (IsCollected) gameObject.SetActive(false);
    }

    public void Interact(PlayerInteractor interactor)
    {
        GameFlags.Set(FlagKey);
        gameObject.SetActive(false);
        AudioManager.PlaySfx(Sfx.Pickup); // (5-2)

        var data = ScriptableObject.CreateInstance<DialogueData>();
        if (!string.IsNullOrEmpty(foundText)) data.lines.Add(new DialogueLine { speaker = "", text = foundText });
        data.lines.Add(new DialogueLine
        {
            speaker = "",
            text = $"글자 스티커를 주웠다. '{letter}'라고 적혀 있다. ({puzzle.CollectedCount}/{puzzle.FragmentCount})",
        });
        DialogueRunner.Instance?.StartDialogue(data);
    }
}
