using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화, 퍼즐 화면 등이 열려 있는 동안 플레이어 조작을 막는 공용 잠금.
/// 여러 시스템이 동시에 잠글 수 있으며, 모두 풀려야 조작이 돌아옵니다.
/// </summary>
public static class GameplayLock
{
    private static readonly HashSet<object> owners = new();
    private static int releasedFrame = -1;

    /// <summary>하나라도 잠그고 있으면 true.</summary>
    public static bool IsLocked => owners.Count > 0;

    /// <summary>이번 프레임에 잠금이 완전히 풀렸으면 true.</summary>
    public static bool WasReleasedThisFrame => releasedFrame == Time.frameCount;

    /// <summary>
    /// 상호작용을 받으면 안 되는 상태인지.
    /// 대화를 끝낸 E키가 같은 프레임에 새 대화를 여는 것을 막기 위해, 풀린 프레임도 막습니다.
    /// </summary>
    public static bool BlocksInteraction => IsLocked || WasReleasedThisFrame;

    public static void Acquire(object owner)
    {
        if (owner == null) return;
        owners.Add(owner);
    }

    public static void Release(object owner)
    {
        if (owner == null) return;

        if (owners.Remove(owner) && owners.Count == 0)
            releasedFrame = Time.frameCount;
    }

    // Enter Play Mode 설정에서 Domain Reload를 꺼도 이전 플레이의 잠금이 남지 않게 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        owners.Clear();
        releasedFrame = -1;
    }
}
