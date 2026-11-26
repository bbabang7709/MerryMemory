using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 가로등·벽난로·오븐 불빛이 은은하게 일렁이게 합니다. (로드맵 5-1)
/// Light 2D와 같은 오브젝트에 붙입니다. 밝기를 "원래 밝기 ± amount" 안에서 부드럽게 흔듭니다.
///
/// Random.value를 매 프레임 쓰면 지지직 깜빡이고, Perlin Noise를 쓰면 부드럽게 오르내립니다.
///   Mathf.PerlinNoise(x, y): 0~1 사이 값. 입력이 조금씩 바뀌면 출력도 조금씩 바뀜
///   → y에 시간을 넣으면 시간에 따라 부드럽게 변하는 값
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LampFlicker : MonoBehaviour
{
    [Tooltip("흔들리는 정도 (0.15 = 원래 밝기의 ±15%)")]
    [SerializeField, Range(0f, 1f)] private float amount = 0.12f;
    [Tooltip("흔들리는 빠르기")]
    [SerializeField, Min(0f)] private float speed = 1.5f;

    private Light2D light2D;
    private float baseIntensity;
    private float seed; // 가로등마다 다른 박자로 흔들리게 하는 출발점

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // Perlin 0~1 → -1~1 → 원래 밝기의 ±amount 안에서 부드럽게
        float noise = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        light2D.intensity = baseIntensity * (1f + noise * amount);
    }
}
