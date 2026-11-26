using UnityEngine;

/// <summary>
/// 임시 퍼즐: 스위치가 전부 켜지면 풀립니다. (로드맵 3-3, 4단계에서 진짜 퍼즐로 교체)
/// PuzzleBase를 상속해서 "규칙"만 씁니다. 조각·대사는 전혀 모릅니다.
///   스위치가 바뀔 때마다(Toggled) → 전부 켜졌으면 MarkSolved + 스위치 잠금
///   예전에 푼 퍼즐이면(3-4 플래그) 씬이 열릴 때 켜진 채로 잠겨 있음
/// </summary>
public class SwitchPuzzle : PuzzleBase
{
    [Tooltip("이 퍼즐의 스위치들 (Setup 메뉴가 A, B를 넣어 둠)")]
    [SerializeField] private PuzzleSwitch[] switches;

    // 스위치 칸이 비어 있어도(null) 오류 없이 건너뜁니다.
    private void OnEnable()
    {
        foreach (var s in switches)
        {
            if (s != null) s.Toggled += OnSwitchToggled;
        }
    }

    private void OnDisable()
    {
        foreach (var s in switches)
        {
            if (s != null) s.Toggled -= OnSwitchToggled;
        }
    }

    private void OnSwitchToggled(PuzzleSwitch changed)
    {
        if (CheckAllSwitchesOn())
        {
            MarkSolved();
            LockAllSwitches(); // 푼 뒤에는 다시 끌 수 없게
        }
    }

    /// <summary>
    /// 예전에 푼 퍼즐로 복원될 때 (PuzzleBase.Start가 부름): 모든 스위치를 켜진 모양으로 잠급니다.
    /// Interact 대신 SetState를 쓰는 이유: Interact는 Toggled 이벤트를 다시 부르기 때문.
    /// </summary>
    protected override void OnRestoredSolved()
    {
        foreach (var s in switches)
        {
            if (s == null) continue;
            
            s.SetState(true);
            s.Locked = true;
        }
    }

    /// <summary>모든 스위치가 켜졌는지. 빈 칸이 있으면 false.</summary>
    private bool CheckAllSwitchesOn()
    {
        foreach (var s in switches)
        {
            if (s == null || !s.IsOn)
                return false;
        }

        return true;
    }

    private void LockAllSwitches()
    {
        foreach (var s in switches)
        {
            if (s != null) s.Locked = true;
        }
    }
}
