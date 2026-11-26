using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 카메라를 따라다니는 눈 (로드맵 5-1). Main Camera의 자식 Snow 파티클에 붙어 있습니다.
/// 마을(바깥)에서만 내리고, 실내로 들어가면 멈춥니다.
///   - CameraAreaSwitcher.AreaChanged 이벤트를 듣고 → 바깥이면 Play, 실내면 Stop
///   - 문 이동은 화면이 어두운(페이드) 동안 일어나므로, 실내로 갈 때는 남은 눈송이까지 한 번에 지워도 티가 안 남
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class OutdoorSnow : MonoBehaviour
{
    private ParticleSystem snow;

    private void Awake() => snow = GetComponent<ParticleSystem>();

    private void OnEnable()
    {
        CameraAreaSwitcher.AreaChanged += OnAreaChanged;
    }

    private void OnDisable()
    {
        // 해제를 빠뜨리면 씬을 나간 뒤에도 사라진 오브젝트를 부르다 에러가 남
        CameraAreaSwitcher.AreaChanged -= OnAreaChanged;
    }

    private void Start()
    {
        // 씬 시작 시점: 바깥에서 시작하면 그대로(자동 재생), 실내에서 시작(이어하기)하면 끔
        // (실내 시작이면 CameraAreaSwitcher가 한 프레임 뒤 SetArea를 부르며 AreaChanged도 와서 한 번 더 맞춰짐)
        var switcher = CameraAreaSwitcher.Instance;
        if (switcher != null && switcher.CurrentArea != null) Apply(switcher.IsOutdoors);
    }

    /// <summary>구역이 바뀌면: 새 구역이 바깥(기본 구역)인지에 따라 눈 켜기/끄기</summary>
    private void OnAreaChanged(Collider2D area)
    {
        if (CameraAreaSwitcher.Instance != null)
        {
            Apply(area == CameraAreaSwitcher.Instance.DefaultArea);
        }
    }

    /// <summary>바깥이면 눈 내리기, 실내면 멈추고 남은 눈송이까지 지우기</summary>
    private void Apply(bool outdoors)
    {
        if (outdoors)
        {
            if (!snow.isPlaying) snow.Play();
        }
        else
        {
            snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
