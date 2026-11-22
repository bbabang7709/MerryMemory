using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // GetBindingDisplayString 확장 메서드

/// <summary>
/// 캐릭터 앞쪽(바라보는 방향)에 원을 그려 가장 가까운 IInteractable을 찾고,
/// 상호작용 키를 누르면 Interact를 호출합니다.
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(PlayerInputReader))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Probe")]
    [Tooltip("내 콜라이더 가장자리에서 바라보는 방향으로 얼마나 앞에 원을 둘지")]
    [SerializeField, Min(0f)] private float reach = 0.25f;
    [SerializeField, Min(0.05f)] private float probeRadius = 0.35f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    /// <summary>현재 상호작용 대상. 없으면 null.</summary>
    public IInteractable Target { get; private set; }

    /// <summary>대상이 바뀌면 호출됩니다(없어지면 null). 안내 UI가 듣습니다.</summary>
    public event Action<IInteractable> TargetChanged;

    /// <summary>상호작용 키 이름 (예: "F"). 안내 문구에 씁니다.</summary>
    public string InteractKeyLabel => input.Actions.Player.Interact.GetBindingDisplayString();

    private PlayerController controller;
    private PlayerInputReader input;
    private Collider2D body;
    private readonly List<Collider2D> hits = new();
    private ContactFilter2D filter;

    /// <summary>
    /// 감지 원의 중심: 내 콜라이더 중심에서 바라보는 방향으로 (콜라이더 반 크기 + reach)만큼.
    /// 콜라이더가 발만 덮든 몸 전체를 덮든 항상 몸 바로 앞을 감지합니다.
    /// </summary>
    private Vector2 ProbeCenter
    {
        get
        {
            Vector2 facing = controller != null ? controller.Facing : Vector2.down;
            Collider2D col = body != null ? body : GetComponent<Collider2D>();
            if (col == null) return (Vector2)transform.position + facing * reach;

            Bounds b = col.bounds;
            float halfSize = Mathf.Abs(facing.x) * b.extents.x + Mathf.Abs(facing.y) * b.extents.y;
            return (Vector2)b.center + facing * (halfSize + reach);
        }
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputReader>();
        body = GetComponent<Collider2D>();

        filter = new ContactFilter2D();
        filter.SetLayerMask(interactableLayers);
        filter.useTriggers = true; // 범위용 Trigger 콜라이더도 감지
    }

    private void OnDisable() => SetTarget(null);

    private void Update()
    {
        // 대화·퍼즐 중에는 대상을 비워서 안내 UI도 숨깁니다.
        SetTarget(GameplayLock.IsLocked ? null : FindTarget());

        if (Target == null || GameplayLock.BlocksInteraction) return;
        if (input.IsInteractPressed) Target.Interact(this);
    }

    private IInteractable FindTarget()
    {
        Vector2 center = ProbeCenter;
        Physics2D.OverlapCircle(center, probeRadius, filter, hits);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // 자기 자신 제외

            var candidate = hit.GetComponentInParent<IInteractable>();
            if (candidate == null || !IsAlive(candidate)) continue;

            float dist = (hit.ClosestPoint(center) - center).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }
        return best;
    }

    private void SetTarget(IInteractable next)
    {
        // 대상 오브젝트가 파괴되었으면 없는 것으로 취급
        if (Target != null && !IsAlive(Target)) Target = null;
        if (ReferenceEquals(next, Target)) return;

        Target = next;
        TargetChanged?.Invoke(Target);
    }

    // 인터페이스 참조는 Unity의 "파괴됨" 판정을 못 하므로 UnityEngine.Object로 바꿔서 확인
    private static bool IsAlive(IInteractable target) =>
        target is not UnityEngine.Object obj || obj != null;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Target != null ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(ProbeCenter, probeRadius);
    }
}
