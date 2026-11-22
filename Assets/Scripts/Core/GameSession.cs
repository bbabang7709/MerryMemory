using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이번 플레이의 정보를 보관하는 정적 클래스. 씬이 바뀌어도 값이 남습니다.
/// 씬에 오브젝트를 두지 않아도 어디서든 GameSession.Profile로 읽을 수 있습니다.
///
/// 담는 것: 주인공 프로필(이름·성별), 모은 카드 조각
/// 흐름: 명찰 화면에서 Begin(이름, 성별) → Village에서 사용 → 엔딩 후 타이틀에서 Clear()
/// 명찰을 거치지 않고 Village를 바로 실행하면 기본 프로필(PlayerProfile.CreateDefault)로 동작합니다.
/// </summary>
public static class GameSession
{
    private static PlayerProfile profile;

    /// <summary>현재 주인공 정보. 명찰을 아직 안 거쳤으면 기본값을 돌려줍니다.</summary>
    public static PlayerProfile Profile => profile ??= PlayerProfile.CreateDefault();

    /// <summary>명찰 작성으로 이번 플레이가 정식으로 시작됐는지.</summary>
    public static bool HasStarted { get; private set; }

    /// <summary>프로필이 바뀌면 호출됩니다. (명찰 미리보기, 스킨 교체 등)</summary>
    public static event Action<PlayerProfile> ProfileChanged;

    // 모은 카드 조각의 ID("stamp" 등). HashSet이라 같은 조각을 두 번 넣어도 하나로 칩니다.
    // 바깥에서는 CollectPiece / HasPiece / PieceCount로만 다룹니다. (3단계에서 CardProgress로 옮길 예정)
    private static readonly HashSet<string> Pieces = new();

    /// <summary>명찰 작성을 마치면 호출. 이름은 앞뒤 공백을 잘라 저장합니다.</summary>
    public static void Begin(string name, Gender gender)
    {
        if (!PlayerProfile.IsValidName(name, out string reason))
            throw new ArgumentException(reason, nameof(name));

        profile = new PlayerProfile(name.Trim(), gender);
        HasStarted = true;
        ProfileChanged?.Invoke(profile);
    }

    // ───────────── 카드 조각 ─────────────

    /// <summary>조각을 모았다고 기록합니다. 빈 ID는 무시합니다.</summary>
    public static void CollectPiece(string pieceId)
    {
        if (!string.IsNullOrEmpty(pieceId))
        {
            Pieces.Add(pieceId);
        }
    }

    /// <summary>그 조각을 모았는지.</summary>
    public static bool HasPiece(string pieceId)
    {
        return Pieces.Contains(pieceId);
    }

    /// <summary>지금까지 모은 조각 수.</summary>
    public static int PieceCount => Pieces.Count;

    /// <summary>이번 플레이 정보(프로필, 조각)를 모두 지웁니다. 타이틀로 돌아올 때 호출됩니다.</summary>
    public static void Clear()
    {
        profile = null;
        HasStarted = false;
        Pieces.Clear(); // 안 비우면 두 번째 플레이가 조각을 든 채로 시작됩니다
        ProfileChanged?.Invoke(Profile);
    }

    // 이 프로젝트는 Play 진입 시 도메인 리로드를 끄고 있어서(Enter Play Mode Options),
    // 정적 값이 지난번 Play의 것으로 남습니다. Play를 시작할 때마다 비웁니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        profile = null;
        HasStarted = false;
        ProfileChanged = null;
        Pieces.Clear();
    }
}
