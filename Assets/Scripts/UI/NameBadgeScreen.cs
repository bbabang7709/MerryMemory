using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// "임시 집배원 명찰" 작성 화면 (GDD 6장).
///  - 사진 칸: 고른 성별의 캐릭터가 서 있는 모습. [&lt;] [&gt;] 버튼으로 바꿈
///  - 이름 칸: 최대 6글자. 규칙은 PlayerProfile.IsValidName
///  - [확인] 또는 이름 칸에서 Enter → "○○(으)로 할까요?" → [네] → 도장 → Village
/// </summary>
public class NameBadgeScreen : MonoBehaviour
{
    [Header("사진 칸")]
    [SerializeField] private SpriteFlipbook photo;
    [SerializeField] private Sprite[] femaleFrames;
    [SerializeField] private Sprite[] maleFrames;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("이름 칸")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button confirmButton;

    [Header("확인 창")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("도장")]
    [SerializeField] private RectTransform stamp;
    [SerializeField, Min(0f)] private float stampHold = 0.9f;

    private Gender gender;
    private bool finished;

    private void Start()
    {
        gender = GameSession.Profile.Gender;
        nameInput.characterLimit = PlayerProfile.MaxNameLength;
        nameInput.text = "";

        prevButton.onClick.AddListener(ToggleGender);
        nextButton.onClick.AddListener(ToggleGender);
        confirmButton.onClick.AddListener(OnConfirm);
        nameInput.onSubmit.AddListener(_ => OnConfirm()); // 이름 칸에서 Enter
        nameInput.onValueChanged.AddListener(_ => ShowError(null));
        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(CloseConfirm);

        confirmPanel.SetActive(false);
        stamp.gameObject.SetActive(false);
        ShowError(null);
        RefreshPhoto();
        Select(nameInput);
    }

    private void Update()
    {
        if (finished) return;

        // 확인 창에서 Esc → 아니요
        if (confirmPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseConfirm();

        // 선택이 풀리면 지금 화면의 기본 칸을 다시 선택
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            Select(confirmPanel.activeSelf ? yesButton : nameInput);
    }

    private void ToggleGender()
    {
        gender = gender == Gender.Female ? Gender.Male : Gender.Female;
        RefreshPhoto();
        Select(nameInput); // 버튼에 선택이 남으면 Enter가 성별 버튼을 다시 누르게 되므로 이름 칸으로
    }

    private void RefreshPhoto() => photo.SetFrames(gender == Gender.Female ? femaleFrames : maleFrames);

    private void OnConfirm()
    {
        if (finished || confirmPanel.activeSelf) return;

        string name = nameInput.text;
        if (!PlayerProfile.IsValidName(name, out string reason))
        {
            ShowError(reason);
            Select(nameInput);
            return;
        }

        confirmText.text = $"{WithEuroParticle(name.Trim())} 할까요?";
        confirmPanel.SetActive(true);
        Select(yesButton);
    }

    private void CloseConfirm()
    {
        confirmPanel.SetActive(false);
        Select(nameInput);
    }

    private void OnYes()
    {
        if (finished) return;
        finished = true;
        GameSession.Begin(nameInput.text, gender);
        StartCoroutine(StampAndGo());
    }

    private IEnumerator StampAndGo()
    {
        confirmPanel.SetActive(false);
        foreach (var s in GetComponentsInChildren<Selectable>()) s.interactable = false;

        // 도장: 크게 나타났다가 "쾅" 하고 제자리에 찍힘
        stamp.gameObject.SetActive(true);
        var group = stamp.GetComponent<CanvasGroup>();
        const float duration = 0.18f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            stamp.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, k * k);
            if (group != null) group.alpha = k;
            yield return null;
        }
        stamp.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;

        yield return new WaitForSecondsRealtime(stampHold);
        SceneLoader.Load(SceneNames.Village);
    }

    private void ShowError(string message)
    {
        errorText.text = message ?? "";
        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private static void Select(Selectable s)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    /// <summary>
    /// 이름 뒤에 "로/으로"를 붙입니다. 받침이 없거나 ㄹ 받침이면 "로", 그 밖의 받침은 "으로".
    /// 임시: 2-3에서 TextFormatter를 만들면 TextFormatter로 바꿉니다.
    /// </summary>
    private static string WithEuroParticle(string name)
    {
        char last = name[name.Length - 1];
        if (last < '가' || last > '힣') return name + "로"; // 한글이 아니면 받침 없음으로 처리
        int final = (last - '가') % 28;                      // 0 = 받침 없음, 8 = ㄹ 받침
        return name + (final == 0 || final == 8 ? "로" : "으로");
    }
}
