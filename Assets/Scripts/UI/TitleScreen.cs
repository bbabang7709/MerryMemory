using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면: [시작하기] → 명찰 작성, [이어하기] → 세이브 불러오기, [끝내기] → 게임 종료.
/// 키보드는 위/아래로 고르고 Enter(또는 Space)로 누릅니다.
/// 엔딩을 본 적이 있으면 "배달 완료" 표시를 띄웁니다. (본격적인 타이틀 변화는 5단계)
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Tooltip("엔딩을 봤을 때만 보이는 \"배달 완료\" 표시")]
    [SerializeField] private GameObject deliveredMark;

    private void Start()
    {
        // 타이틀로 돌아왔으면 지난 플레이 정보는 지웁니다.
        GameSession.Clear();

        startButton.onClick.AddListener(OnStart);
        continueButton.onClick.AddListener(OnContinue);
        quitButton.onClick.AddListener(OnQuit);

        // 세이브가 있을 때만 [이어하기] (interactable = false면 흐리게 보이고 눌리지 않음)
        continueButton.interactable = SaveSystem.HasSave;

        // 엔딩을 본 적이 있으면 "배달 완료" 표시 (새 게임을 해도 남는 기록)
        deliveredMark.SetActive(SaveSystem.EndingCleared);

        // 처음 선택: 이어할 수 있으면 [이어하기], 아니면 [시작하기]
        Select(DefaultButton);
    }

    private Button DefaultButton => continueButton.interactable ? continueButton : startButton;

    private void Update()
    {
        // 마우스로 빈 곳을 눌러 선택이 풀려도 키보드로 다시 고를 수 있게
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            Select(DefaultButton);
    }

    private void OnStart() => SceneLoader.Load(SceneNames.NameBadge);

    private void OnContinue()
    {
        // 불러오기에 성공하면 저장된 씬으로 (씬 이름이 비어 있으면 Village)
        if (SaveSystem.Load())
        {
            string targetSceneName = !string.IsNullOrEmpty(SaveSystem.LoadedSceneName) ? SaveSystem.LoadedSceneName : SceneNames.Village;
            SceneLoader.Load(targetSceneName);
        }
        else
        {
            // 파일이 없거나 깨졌을 때. 자세한 원인은 SaveSystem.Load가 남긴 경고에 있음
            Debug.LogWarning("세이브를 불러오지 못했습니다.");
            continueButton.interactable = false;
        }
    }

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
