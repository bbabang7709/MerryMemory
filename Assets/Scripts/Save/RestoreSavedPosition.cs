using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 이어하기로 들어왔으면 플레이어를 저장된 위치로 옮깁니다. (로드맵 3-5)
/// Village의 Player에 붙어 있습니다. 새 게임이면 저장된 위치가 없으니 아무것도 안 합니다.
/// </summary>
public class RestoreSavedPosition : MonoBehaviour
{
    private void Start()
    {
        if (!SaveSystem.TryTakePendingPosition(out Vector2 position)) return;

        Vector3 from = transform.position;
        Vector3 to = new(position.x, position.y, from.z);
        transform.position = to;
        if (TryGetComponent(out Rigidbody2D rb)) rb.position = position;

        // 따라가는 카메라가 원래 자리에서 천천히 미끄러져 오지 않고 바로 붙도록 알림
        CinemachineCore.OnTargetObjectWarped(transform, to - from);
    }
}
