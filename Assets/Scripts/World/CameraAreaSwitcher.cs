using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 따라가는 카메라의 경계(Confiner 2D)를 "지금 있는 구역"으로 바꿔 끼웁니다. (로드맵 4-0)
/// 마을, 우체국 실내, 공방 실내 … 구역마다 Camera Bounds 아래에 경계 모양이 하나씩 있습니다.
///   - 문을 지나면 Door가 SetArea(도착 구역)을 부릅니다.
///   - 씬이 시작되면(이어하기로 실내에서 시작해도) 플레이어가 들어 있는 구역을 스스로 찾습니다.
///   - (5-1) 구역이 바뀔 때마다 AreaChanged 이벤트를 알립니다. (눈은 마을에서만 내리게 등)
/// CM Follow Camera에 붙어 있습니다. 씬에 하나만 둡니다.
/// </summary>
[RequireComponent(typeof(CinemachineConfiner2D))]
public class CameraAreaSwitcher : MonoBehaviour
{
    public static CameraAreaSwitcher Instance { get; private set; }

    /// <summary>카메라 구역이 바뀐 순간 (새 구역). 구독은 OnEnable, 해제는 OnDisable에서 (5-1)</summary>
    public static event Action<Collider2D> AreaChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AreaChanged = null;

    [Tooltip("구역 경계들 (Camera Bounds 아래 PolygonCollider2D). 첫 번째가 기본(마을)")]
    [SerializeField] private Collider2D[] areas;
    [SerializeField] private Transform player;

    private CinemachineConfiner2D confiner;

    /// <summary>지금 카메라가 갇혀 있는 구역</summary>
    public Collider2D CurrentArea => confiner != null ? confiner.BoundingShape2D : null;

    /// <summary>기본 구역(마을 바깥). areas의 첫 번째</summary>
    public Collider2D DefaultArea => areas.Length > 0 ? areas[0] : null;

    /// <summary>지금 바깥(마을)에 있는지</summary>
    public bool IsOutdoors => CurrentArea == DefaultArea;

    private void Awake()
    {
        Instance = this;
        confiner = GetComponent<CinemachineConfiner2D>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 이어하기로 저장 위치에 옮겨지는 것(RestoreSavedPosition.Start)을 기다린 뒤 한 프레임 늦게 찾습니다.
    private IEnumerator Start()
    {
        yield return null;
        if (player != null) SetArea(AreaAt(player.position));
    }

    /// <summary>카메라 경계를 이 구역으로 바꿉니다. null이면 무시.</summary>
    public void SetArea(Collider2D area)
    {
        if (area == null || confiner.BoundingShape2D == area) return;
        confiner.BoundingShape2D = area;
        confiner.InvalidateBoundingShapeCache(); // 모양이 바뀌었으니 다시 계산하라고 알림
        AreaChanged?.Invoke(area);
    }

    /// <summary>그 위치가 들어 있는 구역. 어디에도 없으면 첫 번째(마을).</summary>
    public Collider2D AreaAt(Vector2 position)
    {
        foreach (var area in areas)
        {
            if (area != null && area.OverlapPoint(position)) return area;
        }
        return areas.Length > 0 ? areas[0] : null;
    }
}
