using System;
using UnityEngine;

/// <summary>
/// [F]로 켜고 끄는 스위치. 퍼즐 부품으로 씁니다. (로드맵 3-3 임시 퍼즐)
/// 스스로는 "켜졌다/꺼졌다"만 알리고(Toggled), 정답 판정은 퍼즐(SwitchPuzzle)이 합니다.
/// 퍼즐이 풀리면 Locked가 되어 더 이상 바뀌지 않습니다.
/// </summary>
public class PuzzleSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color offColor = new(0.55f, 0.65f, 0.8f);
    [SerializeField] private Color onColor = new(1f, 0.84f, 0.45f);
    [SerializeField] private Vector2 promptOffset = new(0f, 1f);

    /// <summary>켜져 있는지</summary>
    public bool IsOn { get; private set; }

    /// <summary>true면 눌러도 바뀌지 않음 (퍼즐이 풀린 뒤)</summary>
    public bool Locked { get; set; }

    /// <summary>켜지거나 꺼질 때마다 호출됩니다.</summary>
    public event Action<PuzzleSwitch> Toggled;

    public string Prompt => Locked ? "눌려 있음" : (IsOn ? "끄기" : "켜기");
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake() => UpdateVisual();

    public void Interact(PlayerInteractor interactor)
    {
        if (Locked) return;

        IsOn = !IsOn;
        UpdateVisual();
        Toggled?.Invoke(this);
    }

    /// <summary>켜짐 상태를 직접 정합니다. Toggled 이벤트는 부르지 않습니다. (저장된 상태 복원용)</summary>
    public void SetState(bool on)
    {
        IsOn = on;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (body != null) body.color = IsOn ? onColor : offColor;
    }
}
