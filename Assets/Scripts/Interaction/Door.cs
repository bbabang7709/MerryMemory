using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 문(또는 계단). 조사하면 같은 씬 안의 다른 곳으로 순간 이동합니다. (로드맵 4-0)
///   화면 어둡게 → 플레이어를 도착점으로 → 바라보는 방향 맞추기 → 카메라 경계를 도착 구역으로 → 화면 밝게
/// 잠긴 문: requiredFlag가 켜져 있지 않으면 lockedDialogue를 보여 주고 열리지 않습니다. (예: 튜토리얼 전 우체국 문)
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Tooltip("[F] 뒤에 보일 말 (예: 들어가기, 나가기, 올라가기)")]
    [SerializeField] private string prompt = "들어가기";
    [SerializeField] private Vector2 promptOffset = new(0f, 0.8f);

    [Header("도착")]
    [Tooltip("도착할 자리 (발 위치)")]
    [SerializeField] private Transform destination;
    [Tooltip("도착하면 바라볼 방향 (실내로 들어가면 위, 밖으로 나오면 아래)")]
    [SerializeField] private Vector2 faceOnArrive = Vector2.down;
    [Tooltip("도착 구역의 카메라 경계 (Camera Bounds 아래)")]
    [SerializeField] private Collider2D destinationArea;

    [Header("잠금 (선택)")]
    [Tooltip("이 플래그가 켜져 있어야 열림. 비워 두면 항상 열림 (FlagKeys 참고)")]
    [SerializeField] private string requiredFlag;
    [SerializeField] private DialogueData lockedDialogue;

    private bool travelling;

    public string Prompt => prompt;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    /// <summary>지금 열려 있는지. 잠금 플래그가 없으면 항상 열림.</summary>
    public bool IsOpen => string.IsNullOrEmpty(requiredFlag) ? true : GameFlags.Has(requiredFlag);

    public void Interact(PlayerInteractor interactor)
    {
        if (travelling || destination == null) return;

        if (!IsOpen)
        {
            DialogueRunner.Instance?.StartDialogue(lockedDialogue);
            return;
        }

        StartCoroutine(Travel(interactor.GetComponent<PlayerController>()));
    }

    private IEnumerator Travel(PlayerController player)
    {
        travelling = true;
        GameplayLock.Acquire(this);

        yield return SceneLoader.FadeScreen(1f);

        // 플레이어 옮기기. Rigidbody2D(보간 사용)도 같이 옮겨야 물리 갱신 때 원래 자리로 되돌아가지 않음
        Vector3 from = player.transform.position;
        Vector3 to = destination.position;
        player.transform.position = new Vector3(to.x, to.y, from.z);
        if (player.TryGetComponent(out Rigidbody2D rb)) rb.position = (Vector2)to;
        player.Face(faceOnArrive);

        // 카메라: 경계를 도착 구역으로 바꾸고, 순간 이동이라고 알려 미끄러지지 않고 바로 붙게
        CameraAreaSwitcher.Instance?.SetArea(destinationArea);
        CinemachineCore.OnTargetObjectWarped(player.transform, to - from);

        yield return SceneLoader.FadeScreen(0f);

        GameplayLock.Release(this);
        travelling = false;
    }

    private void OnDisable()
    {
        // 이동 도중 꺼지면 잠금이 남지 않게
        GameplayLock.Release(this);
        travelling = false;
    }
}
