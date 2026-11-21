using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputActions Actions { get; private set; }

    public Vector2 MoveInput { get; private set; }

    public bool IsInteractPressed => Actions.Player.Interact.WasPressedThisFrame();

    private void Awake()
    {
        Actions = new PlayerInputActions();
    }

    private void OnEnable() => Actions.Enable();
    private void OnDisable() => Actions.Disable();
    private void OnDestroy() => Actions.Disable();

    private void Update()
    {
        MoveInput = Actions.Player.Move.ReadValue<Vector2>();
    }
}
