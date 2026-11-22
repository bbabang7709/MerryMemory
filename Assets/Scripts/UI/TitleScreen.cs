using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면: [시작하기] → 명찰 작성, [끝내기] → 게임 종료.
/// 키보드는 위/아래로 고르고 Enter(또는 Space)로 누릅니다.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // 타이틀로 돌아왔으면 지난 플레이 정보는 지웁니다.
        GameSession.Clear();

        startButton.onClick.AddListener(OnStart);
        quitButton.onClick.AddListener(OnQuit);
        Select(startButton);
    }

    private void Update()
    {
        // 마우스로 빈 곳을 눌러 선택이 풀려도 키보드로 다시 고를 수 있게
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            Select(startButton);
    }

    private void OnStart() => SceneLoader.Load(SceneNames.NameBadge);

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(Selectable s)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
