using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f; 
    public Vector2 MoveDirection { get; private set; } = Vector2.zero;


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
        MoveDirection = SnapToFourDirections(playerInput.MoveInput);
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
