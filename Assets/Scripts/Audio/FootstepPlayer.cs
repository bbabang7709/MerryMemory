using UnityEngine;

/// <summary>
/// 걸을 때 일정한 간격으로 발소리 (로드맵 5-2). Player에 붙입니다.
///   - 움직이는 동안: interval초마다 Sfx.Footstep (조금 작게, 높낮이를 살짝 무작위로)
///   - 멈추면 타이머를 0으로 → 다시 걷기 시작하는 순간 바로 첫 발소리
/// 대화·퍼즐 중에는 PlayerController.MoveDirection이 0이라 저절로 조용해집니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class FootstepPlayer : MonoBehaviour
{
    [Tooltip("발소리 간격(초). 걷는 애니메이션 박자에 맞추면 자연스러움")]
    [SerializeField, Min(0.05f)] private float interval = 0.3f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float pitchJitter = 0.08f;

    private PlayerController controller;
    private float timer;

    private void Awake() => controller = GetComponent<PlayerController>();

    private void Update()
    {
        // 멈추면 0으로 → 다시 걷는 순간 첫 발소리
        if (controller.MoveDirection == Vector2.zero)
        {
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            AudioManager.PlaySfx(Sfx.Footstep, volume, pitchJitter);
            timer = interval;
        }
    }
}
