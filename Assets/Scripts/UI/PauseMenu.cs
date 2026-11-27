using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Esc 일시정지 메뉴 (로드맵 6단계). Village의 UICanvas/PauseMenu에 붙어 있습니다.
///   [계속하기] [저장하고 타이틀로] [저장하고 끝내기]
/// 전체 화면 빌드에서 게임 도중 나갈 길이 없던 문제를 막습니다.
///
/// 다른 Esc와 부딪히지 않게, "평소에 걸어 다닐 때"만 열립니다.
///   대화 중·퍼즐 화면·얼음 위·조각 보상·엔딩은 모두 GameplayLock을 잡고 있으므로 열리지 않음
///   (퍼즐 화면을 Esc로 닫은 바로 그 프레임도 BlocksInteraction이 막아 줌 → Esc 한 번에 두 가지가 일어나지 않음)
/// 열려 있는 동안은 Time.timeScale = 0 으로 세상을 멈춥니다. (소리·대사 글자 효과는 unscaled라 영향 없음)
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;

    private bool open;

    private void Awake()
    {
        resumeButton.onClick.AddListener(Close);
        titleButton.onClick.AddListener(SaveAndTitle);
        quitButton.onClick.AddListener(SaveAndQuit);
        panel.SetActive(false);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (open) Close();
        else if (!GameplayLock.BlocksInteraction && !SceneLoader.IsLoading) Open();
    }

    private void Open()
    {
        open = true;
        panel.SetActive(true);
        GameplayLock.Acquire(this);
        Time.timeScale = 0f;
        AudioManager.PlaySfx(Sfx.Click);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void Close()
    {
        if (!open) return;
        open = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
        GameplayLock.Release(this);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>지금 상태(이름, 조각, 플래그, 위치)를 저장하고 타이틀로. 타이틀에서 [이어하기]로 돌아옴</summary>
    private void SaveAndTitle()
    {
        SaveSystem.Save();
        Close(); // timeScale을 1로 되돌려야 화면 전환(페이드)이 움직임
        SceneLoader.Load(SceneNames.Title);
    }

    private void SaveAndQuit()
    {
        SaveSystem.Save();
        Close();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 열린 채로 씬이 바뀌거나 꺼져도 세상이 멈춘 채로 남지 않게
    private void OnDisable()
    {
        if (!open) return;
        open = false;
        Time.timeScale = 1f;
        GameplayLock.Release(this);
    }
}
