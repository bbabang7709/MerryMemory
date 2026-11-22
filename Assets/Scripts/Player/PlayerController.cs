using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f; 
    public Vector2 MoveDirection { get; private set; } = Vector2.zero;

    /// <summary>마지막으로 움직인 방향(상/하/좌/우). 멈춰 있어도 유지됩니다. 시작은 정면.</summary>
    public Vector2 Facing { get; private set; } = Vector2.down;


    // ====== References ====== //
    private PlayerInputReader playerInput;
    private Rigidbody2D rb;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInputReader>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 대화·퍼즐 중에는 입력을 0으로 처리 → 멈추고 마지막 방향의 Idle 유지
        Vector2 input = GameplayLock.IsLocked ? Vector2.zero : playerInput.MoveInput;
        MoveDirection = SnapToFourDirections(input);
        if (MoveDirection != Vector2.zero) Facing = MoveDirection;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = MoveDirection * moveSpeed;
    }
    private Vector2 SnapToFourDirections(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return Vector2.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        } 
        else
        {
            return new Vector2(0f, Mathf.Sign(input.y));    
        }
    }
}
