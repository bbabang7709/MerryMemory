using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시계탑 종탑: 종을 올바른 순서로 쳐서 캐럴을 완성하는 퍼즐 (로드맵 4-3).
///   - 종(Bell)을 [F]로 치면 Ring(음) → 지금까지 친 음(played)이 정답 멜로디의 앞부분과 같은지 확인
///   - 틀리면 처음부터 (Bell이 "덜컥" 소리를 냄)
///   - 멜로디를 끝까지 치면 MarkSolved → CardPieceReward가 멜로디 칩 조각을 줌
/// 정답: 「징글벨」 후렴 미미미 미미미 미솔도레미 (힌트는 광장의 콧노래 주민 HummingNpc)
/// </summary>
public class BellSequencePuzzle : PuzzleBase
{
    /// <summary>Ring 결과. 종(Bell)이 이걸 보고 소리·연출을 고릅니다.</summary>
    public enum RingResult { Correct, Wrong, Completed }

    [Tooltip("정답 멜로디. 미미미 미미미 미솔도레미")]
    [SerializeField] private BellNote[] melody =
    {
        BellNote.Mi, BellNote.Mi, BellNote.Mi,
        BellNote.Mi, BellNote.Mi, BellNote.Mi,
        BellNote.Mi, BellNote.Sol, BellNote.Do, BellNote.Re, BellNote.Mi,
    };

    // 지금까지 맞게 친 음 (순서대로)
    private readonly List<BellNote> played = new();

    /// <summary>정답 멜로디 (콧노래 주민도 이걸 읽어서 흥얼거림 → 정답이 한 곳에만 있음)</summary>
    public IReadOnlyList<BellNote> Melody => melody;

    /// <summary>지금까지 맞게 친 음 수 (디버그·연출용)</summary>
    public int Progress => played.Count;

    /// <summary>
    /// 종 하나를 쳤을 때 부릅니다. (Bell이 부름)
    /// 다음 차례(melody[played.Count])와 같으면 Correct, 끝까지 쳤으면 MarkSolved 후 Completed,
    /// 틀리면 처음부터 Wrong. 단, 틀린 음이 첫 음과 같으면 그 음을 새 시도의 시작으로 쳐 줍니다.
    /// </summary>
    public RingResult Ring(BellNote note)
    {
        if (IsSolved) return RingResult.Completed;

        BellNote nextNote = melody[played.Count];
        if (note == nextNote)
        {
            played.Add(note);
            if (played.Count >= melody.Length)
            {
                MarkSolved();
                return RingResult.Completed;
            }

            return RingResult.Correct;
        }

        // 틀림 → 처음부터. 이 곡은 "미"가 일곱 번 이어지므로, 틀린 음이 첫 음(미)이면
        // 그 음을 새 시도의 첫 음으로 남겨 둠 (예: 미×8 → 여덟 번째 미부터 다시 시작)
        played.Clear();
        if (note == melody[0])
        {
            played.Add(note);
        }

        return RingResult.Wrong;
    }

    // 풀린 뒤 복원할 때 따로 숨길 것이 없으므로 OnRestoredSolved는 덮어쓰지 않습니다.
    // (종은 풀린 뒤에도 자유롭게 칠 수 있음 → Bell이 IsSolved를 보고 소리만 냄)
}
