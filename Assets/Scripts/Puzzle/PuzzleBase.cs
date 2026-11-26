using System;
using UnityEngine;

/// <summary>
/// 모든 퍼즐의 공통 부모. (로드맵 3-3)
/// 퍼즐마다 이 클래스를 상속해 "규칙"만 구현하고, 다 풀렸을 때 MarkSolved()를 부릅니다.
///
/// 퍼즐은 보상(조각, 대사, UI)을 모릅니다. Solved 이벤트로 "풀렸다"고 알리기만 하면
/// CardPieceReward 같은 쪽이 듣고 알아서 처리합니다.
/// 3-4에서 GameFlags가 생기면 "이 퍼즐을 풀었다"는 기록도 여기서 남깁니다.
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
        Solved?.Invoke(this);
    }

    /// <summary>퍼즐 화면(UI)이 따로 있는 퍼즐이면 자식이 덮어써서 엽니다. (예: 우체국 편지 분류)</summary>
    public virtual void OpenPuzzle() { }
}
