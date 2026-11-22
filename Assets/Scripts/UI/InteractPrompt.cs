using TMPro;
using UnityEngine;

/// <summary>
/// 상호작용 대상의 머리 위에 "[F] 말 걸기" 안내를 띄웁니다.
/// PlayerInteractor.TargetChanged를 듣고, 매 프레임 대상 위치를 화면 좌표로 바꿔 따라갑니다.
/// Screen Space - Overlay 캔버스 기준입니다.
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private RectTransform box;
    [SerializeField] private TMP_Text label;

    private IInteractable target;
    private Camera cam;

    private void Awake()
    {
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        box.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (interactor != null) interactor.TargetChanged += OnTargetChanged;
    }

    private void OnDisable()
    {
        if (interactor != null) interactor.TargetChanged -= OnTargetChanged;
        OnTargetChanged(null);
    }

    private void OnTargetChanged(IInteractable next)
    {
        target = next;
        box.gameObject.SetActive(target != null);
        if (target == null) return;

        label.text = $"[{interactor.InteractKeyLabel}] {target.Prompt}";
        Follow();
    }

    private void LateUpdate()
    {
        if (target != null) Follow();
    }

    private void Follow()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(target.PromptPosition);
        box.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y), 0f); // 픽셀 떨림 방지
    }
}
