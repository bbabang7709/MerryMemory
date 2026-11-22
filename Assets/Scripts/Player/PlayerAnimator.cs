using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int DirXHash     = Animator.StringToHash("DirX");
    private static readonly int DirYHash     = Animator.StringToHash("DirY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerController controller;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        bool isMoving = controller.MoveDirection != Vector2.zero;
        Vector2 facing = controller.Facing; // 움직일 때만 갱신되는 값 → 멈추면 마지막 방향 유지

        animator.SetFloat(DirXHash, facing.x);
        animator.SetFloat(DirYHash, facing.y);
        animator.SetBool(IsMovingHash, isMoving);

        spriteRenderer.flipX = facing.x < 0f;
    }

    public void ApplySkin(RuntimeAnimatorController skin)
    {
        animator.runtimeAnimatorController = skin;
    }
}