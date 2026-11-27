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
        // 폰트에 없는 글자(이모지, 드문 기호 등)는 대사창에서 네모로 나오므로 막습니다. (6단계)
        if (!FontHasAll(name.Trim()))
        {
            ShowError("화면에 나오지 않는 글자(이모지 등)는 쓸 수 없어요.");
            Select(nameInput);
            return;
        }

        confirmText.text = TextFormatter.Format("{이름}{으로/로} 할까요?", name.Trim());
        confirmPanel.SetActive(true);
        Select(yesButton);
    }

    /// <summary>이름 칸 폰트(대사창과 같은 폰트)에 모든 글자가 있는지. 공백은 괜찮음</summary>
    private bool FontHasAll(string text)
    {
        var font = nameInput.textComponent != null ? nameInput.textComponent.font : null;
        if (font == null) return true;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (!font.HasCharacter(c, true, false)) return false;
        }
        return true;
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
        // 새 게임 세이브로 덮어쓰기 (3-5). 지금 씬은 NameBadge라서 "Village에서 시작"을 직접 넘김.
        // 덮어써야 예전 판의 세이브가 [이어하기]로 다시 열리지 않습니다.
        SaveSystem.Save(SceneNames.Village);
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
}
