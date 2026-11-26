/// <summary>
/// 진행 플래그 이름 모음. (로드맵 3-4)
/// 플래그 이름을 코드 여기저기에 "postmaster_talked"처럼 직접 쓰면 오타가 나도 모릅니다.
/// 여기 상수로 모아 두고 FlagKeys.PostMasterTalked 처럼 씁니다.
/// (Inspector에 문자열로 적는 칸에는 아래 값을 그대로 복사해 넣으세요)
/// </summary>
public static class FlagKeys
{
    // ── 대화 ──
    /// <summary>우체국장과 첫 대화(프롤로그)를 했다</summary>
    public const string PostMasterTalked = "postmaster_talked";

    // ── 퍼즐 ──
    /// <summary>퍼즐을 풀었다. 예: PuzzleSolved("switch_test") → "puzzle_solved:switch_test"</summary>
    public static string PuzzleSolved(string puzzleId) => "puzzle_solved:" + puzzleId;

    /// <summary>우체국 편지 분류 퍼즐 id (4-1). 풀면 우체국 출구가 열림 → PuzzleSolved(MailSort)</summary>
    public const string MailSort = "mail_sort";

    /// <summary>장난감 공방 팝업 장치 재조립 퍼즐 id (4-2)</summary>
    public const string PopupAssembly = "popup_assembly";

    /// <summary>공방 주인과 첫 대화를 했다 (4-2)</summary>
    public const string WorkshopOwnerTalked = "workshop_owner_talked";

    /// <summary>시계탑 종 순서 퍼즐 id (4-3)</summary>
    public const string BellSequence = "bell_sequence";

    /// <summary>종지기와 첫 대화를 했다 (4-3)</summary>
    public const string BellRingerTalked = "bell_ringer_talked";
}
