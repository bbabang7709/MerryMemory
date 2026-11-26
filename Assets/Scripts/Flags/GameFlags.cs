using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 진행 플래그: "우체국장과 첫 대화를 했다" 같은 예/아니오 상태를 문자열 키로 기억합니다. (로드맵 3-4)
/// 키가 들어 있으면 "예", 없으면 "아니오". 정적 클래스라 씬이 바뀌어도 남습니다.
/// 키 이름은 FlagKeys에 모아 둔 상수를 쓰세요. (오타 방지)
///
/// CardProgress와 같은 모양입니다 (HashSet + 정적 + Play마다 초기화).
/// 저장(3-5): SaveSystem이 AllKeys를 파일에 쓰고, 불러올 때 RestoreKeys로 되돌립니다.
/// </summary>
public static class GameFlags
{
    // 켜져 있는 플래그 키들
    private static readonly HashSet<string> AcquiredFlags = new();

    /// <summary>플래그를 켭니다(기록). 빈 키는 무시.</summary>
    public static void Set(string key)
    {
        if (!string.IsNullOrEmpty(key))
            AcquiredFlags.Add(key);
    }

    /// <summary>플래그가 켜져 있는지. 빈 키는 false.</summary>
    public static bool Has(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        return AcquiredFlags.Contains(key);
    }

    /// <summary>플래그를 끕니다. (퍼즐 "다시 하기" 등에서 씀)</summary>
    public static void Unset(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        AcquiredFlags.Remove(key);
    }

    /// <summary>켜져 있는 모든 플래그 키 (3-5 저장용). 고치지 말고 읽기만 하세요.</summary>
    public static IEnumerable<string> AllKeys => AcquiredFlags;

    /// <summary>세이브에서 불러온 키들로 되돌립니다. (3-5) 먼저 비우고, 빈 키는 뺍니다.</summary>
    public static void RestoreKeys(IEnumerable<string> keys)
    {
        AcquiredFlags.Clear();
        if (keys == null) return;

        foreach (string key in keys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                AcquiredFlags.Add(key);
            }
        }
    }

    /// <summary>모든 플래그를 지웁니다. (새 게임, GameSession.Clear가 부름)</summary>
    public static void Clear()
    {
        AcquiredFlags.Clear();
    }

    // 도메인 리로드가 꺼져 있어서 Play를 다시 시작해도 정적 값이 남습니다. 시작할 때마다 비웁니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AcquiredFlags.Clear();
    }
}
