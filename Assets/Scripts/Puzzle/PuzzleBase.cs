using System;
using UnityEngine;

/// <summary>
/// 모든 퍼즐의 공통 부모. (로드맵 3-3)
/// 퍼즐마다 이 클래스를 상속해 "규칙"만 구현하고, 다 풀렸을 때 MarkSolved()를 부릅니다.
///
/// 퍼즐은 보상(조각, 대사, UI)을 모릅니다. Solved 이벤트로 "풀렸다"고 알리기만 하면
/// CardPieceReward 같은 쪽이 듣고 알아서 처리합니다.
/// 풀면 GameFlags에 "풀었다"를 남기고, 씬이 다시 열릴 때 그 기록으로 풀린 상태를 복원합니다. (3-4)
/// </summary>
public abstract class PuzzleBase : MonoBehaviour
{
    [Tooltip("퍼즐 고유 키 (저장·플래그용). 예: switch_test, mail_sort")]
    [SerializeField] private string puzzleId;

    public string PuzzleId => puzzleId;

    /// <summary>이미 풀었는지</summary>
    public bool IsSolved { get; private set; }

    /// <summary>퍼즐이 풀린 순간 한 번 호출됩니다.</summary>
    public event Action<PuzzleBase> Solved;

    /// <summary>
    /// 자식 퍼즐이 "다 풀렸다"고 판단했을 때 부릅니다. (protected: 자식만 부를 수 있음)
    /// 이미 풀린 퍼즐이면 무시해서 보상이 두 번 나가지 않습니다.
    /// </summary>
    protected void MarkSolved()
    {
        if (IsSolved) return;

        IsSolved = true;
        // "풀었다"를 장부에 기록 → 씬이 다시 열려도 Start에서 복원됨 (puzzleId가 없으면 기록 안 함)
        if (!string.IsNullOrEmpty(puzzleId))
        {
            GameFlags.Set(SolvedFlag);
        }
        Solved?.Invoke(this);
    }

    // ── 3-4: 플래그로 "풀었다" 기억하기 ──

    /// <summary>이 퍼즐의 "풀었다" 플래그 키. 예: puzzle_solved:switch_test</summary>
    private string SolvedFlag => FlagKeys.PuzzleSolved(puzzleId);

    /// <summary>
    /// 씬이 열릴 때, 예전에 이미 푼 퍼즐이면 "풀린 상태"로 되돌려 놓습니다.
    /// (자식이 Start를 따로 쓰면 맨 앞에 base.Start(); 를 꼭 부르세요)
    /// </summary>
    protected virtual void Start()
    {
        // 복원할 때는 Solved 이벤트를 부르지 않습니다. 부르면 보상(조각·대사)이 또 나옵니다.
        if (!string.IsNullOrEmpty(puzzleId) && GameFlags.Has(SolvedFlag))
        {
            IsSolved = true;
            OnRestoredSolved();
        }
    }

    /// <summary>예전에 푼 퍼즐로 복원될 때 자식이 덮어써서 모양을 맞춥니다. (스위치 켜기 등)</summary>
    protected virtual void OnRestoredSolved() { }

    /// <summary>퍼즐 화면(UI)이 따로 있는 퍼즐이면 자식이 덮어써서 엽니다. (예: 우체국 편지 분류)</summary>
    public virtual void OpenPuzzle() { }
}
