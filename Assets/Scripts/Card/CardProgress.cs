using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모은 카드 조각을 관리합니다. (로드맵 3-2)
/// GameSession처럼 정적 클래스라 씬이 바뀌어도 남고, 어디서든 CardProgress.Has(...)로 씁니다.
///
/// 핵심 원칙: CardProgress는 UI가 있는지 모릅니다.
///   조각을 새로 얻으면 PieceCollected 이벤트로 "얻었다"고 알리기만 하고,
///   현황 UI·저장·마을 변화는 각자 이 이벤트를 듣습니다. (2-4의 ProfileChanged와 같은 방식)
///
/// 저장소에는 CardPieceData 자체가 아니라 id 문자열만 담습니다.
/// </summary>
public static class CardProgress
{
    /// <summary>카드 조각 총 개수 (GDD 5.1)</summary>
    public const int TotalPieces = 5;

    /// <summary>조각을 "새로" 얻었을 때 한 번 호출됩니다. 이미 가진 조각을 또 얻으면 호출되지 않습니다.</summary>
    public static event Action<CardPieceData> PieceCollected;

    // 모은 조각의 id. HashSet이라 같은 id는 하나만 남습니다.
    private static readonly HashSet<string> CollectedIds = new();

    /// <summary>조각을 얻었다고 기록합니다. 처음 얻은 조각이면 PieceCollected를 알립니다.</summary>
    public static void Collect(CardPieceData piece)
    {
        if (piece == null || string.IsNullOrEmpty(piece.id)) return;

        // HashSet.Add는 새로 들어갔을 때만 true → 처음 얻은 조각만 이벤트를 알림
        if (CollectedIds.Add(piece.id))
        {
            PieceCollected?.Invoke(piece);
        }
    }

    /// <summary>그 조각을 모았는지. null이면 false.</summary>
    public static bool Has(CardPieceData piece)
    {
        if (piece == null) return false;

        return CollectedIds.Contains(piece.id);
    }

    /// <summary>모은 조각 수</summary>
    public static int Count => CollectedIds.Count;

    /// <summary>5개를 다 모았는지</summary>
    public static bool IsComplete => Count >= TotalPieces;

    /// <summary>모은 조각을 모두 지웁니다. (새 게임, 타이틀로 돌아갈 때 GameSession.Clear가 부름)</summary>
    public static void Clear()
    {
        CollectedIds.Clear();
    }

    // 도메인 리로드가 꺼져 있어서 Play를 다시 시작해도 정적 값이 남습니다. 시작할 때마다 비웁니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PieceCollected = null;
        CollectedIds.Clear();
    }
}
