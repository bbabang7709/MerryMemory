using System;
using UnityEngine;

/// <summary>
/// 대화 진행 담당: 몇 번째 줄인지, 넘기기 입력, 시작·종료 알림, 이동 잠금.
/// 화면에 그리는 일은 DialogueView가 합니다. 씬에 하나만 둡니다.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [SerializeField] private DialogueView view;

    [Header("Debug")]
    [Tooltip("플레이 중 Inspector의 ⋮ → Play Test Dialogue로 재생")]
    [SerializeField] private DialogueData testDialogue;

    public event Action<DialogueData> DialogueStarted;
    public event Action<DialogueData> DialogueEnded;

    public bool IsRunning => current != null;

    private PlayerInputActions input;
    private DialogueData current;
    private Action onComplete;
    private int index;
    private int startedFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DialogueRunner가 씬에 두 개 있습니다. 하나만 두세요.", this);
            Destroy(this);
            return;
        }
        Instance = this;
        input = new PlayerInputActions();
    }

    private void OnEnable() => input?.Player.Interact.Enable();

    private void OnDisable()
    {
        input?.Player.Interact.Disable();
        GameplayLock.Release(this); // 대화 도중 꺼져도 잠금이 남지 않게
    }

    private void OnDestroy()
    {
        input?.Dispose();
        if (Instance == this) Instance = null;
    }

    /// <summary>대화를 시작합니다. 이미 대화 중이거나 데이터가 비어 있으면 false.</summary>
    /// <param name="onComplete">대화가 끝난 뒤 한 번 호출됩니다. (예: 조각 지급)</param>
    public bool StartDialogue(DialogueData data, Action onComplete = null)
    {
        if (IsRunning || data == null || data.IsEmpty) return false;

        current = data;
        this.onComplete = onComplete;
        index = 0;
        startedFrame = Time.frameCount; // 대화를 연 키로 첫 줄이 바로 넘어가지 않게

        GameplayLock.Acquire(this);
        view.Open();
        view.ShowLine(current.lines[index]);
        DialogueStarted?.Invoke(current);
        return true;
    }

    private void Update()
    {
        if (!IsRunning || Time.frameCount == startedFrame) return;
        if (!input.Player.Interact.WasPressedThisFrame()) return;

        if (view.IsTyping)
            view.CompleteLine(); // 글자가 나오는 중이면 한 번에 전부 표시
        else
            Advance();           // 다 나왔으면 다음 줄
    }

    private void Advance()
    {
        index++;
        if (index < current.lines.Count)
            view.ShowLine(current.lines[index]);
        else
            End();
    }

    private void End()
    {
        DialogueData finished = current;
        Action callback = onComplete;
        current = null;
        onComplete = null;

        view.Close();
        GameplayLock.Release(this);
        DialogueEnded?.Invoke(finished);
        callback?.Invoke();
    }

    [ContextMenu("Play Test Dialogue")]
    private void PlayTestDialogue()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("플레이 모드에서 실행하세요.");
            return;
        }
        StartDialogue(testDialogue);
    }
}
