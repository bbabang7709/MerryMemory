using System;
using UnityEngine;

/// <summary>
/// 지금 손에 들고 있는 물건 하나. (로드맵 4-1: "들고 있는 물건은 CarriedItem 하나로 관리")
/// 한 번에 하나만 들 수 있습니다. 정적 클래스라 문을 지나도 손에 그대로 있습니다.
/// 바뀔 때마다 Changed 이벤트로 알립니다 → 왼쪽 위 표시(CarriedItemView)가 듣습니다.
///
/// GameSession, CardProgress, GameFlags에 이어 네 번째 "장부"입니다. 모양이 거의 같습니다.
/// (저장은 하지 않습니다. 들고 있던 건 이어하기 때 사라지고, 퍼즐도 처음부터)
/// </summary>
public static class CarriedItem
{
    /// <summary>들고 있는 물건이 바뀌면(들기·내려놓기) 호출. 빈손이 되면 null을 넘깁니다.</summary>
    public static event Action<CarryItem> Changed;

    /// <summary>지금 들고 있는 물건. 빈손이면 null.</summary>
    public static CarryItem Current { get; private set; }

    /// <summary>무언가 들고 있는지</summary>
    public static bool IsHolding => Current != null;

    /// <summary>
    /// 물건을 듭니다. 이미 무언가 들고 있거나 item이 null이면 들지 않고 false.
    /// </summary>
    public static bool Hold(CarryItem item)
    {
        if (item == null || IsHolding) return false;

        Current = item;
        Changed?.Invoke(item);
        return true;
    }

    /// <summary>들고 있던 물건을 건네주고 빈손이 됩니다. (분류함에 넣을 때) 빈손이었으면 null.</summary>
    public static CarryItem Take()
    {
        if (!IsHolding) return null;

        CarryItem item = Current;
        Current = null;
        Changed?.Invoke(null);

        return item;
    }

    /// <summary>손을 비웁니다. (새 게임, 퍼즐 다시 하기)</summary>
    public static void Clear()
    {
        if (IsHolding)
        {
            Current = null;
        }

        Changed?.Invoke(null);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }
}
