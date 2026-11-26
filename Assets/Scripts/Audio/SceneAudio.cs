using UnityEngine;

/// <summary>
/// 씬마다 하나: 이 씬의 배경 음악과 바람 소리 크기를 정합니다. (로드맵 5-2)
///   - 씬이 시작되면 AudioManager.PlayBgm(bgm) → 같은 곡이면 끊기지 않고 이어짐
///   - 바람: 바깥이면 outdoorAmbience, 실내면 indoorAmbience (카메라 구역이 바뀔 때마다 맞춤)
///     카메라 구역이 없는 씬(타이틀 등)은 늘 바깥 값
/// </summary>
public class SceneAudio : MonoBehaviour
{
    [SerializeField] private Bgm bgm = Bgm.Village;
    [Tooltip("바깥 바람 소리 크기 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float outdoorAmbience = 1f;
    [Tooltip("실내에서 들리는 바람 소리 크기 (창밖 바람)")]
    [SerializeField, Range(0f, 1f)] private float indoorAmbience = 0.2f;

    private void OnEnable() => CameraAreaSwitcher.AreaChanged += OnAreaChanged;
    private void OnDisable() => CameraAreaSwitcher.AreaChanged -= OnAreaChanged;

    private void Start()
    {
        AudioManager.PlayBgm(bgm);
        var switcher = CameraAreaSwitcher.Instance;
        bool outdoors = switcher == null || switcher.CurrentArea == null || switcher.IsOutdoors;
        AudioManager.SetAmbience(outdoors ? outdoorAmbience : indoorAmbience, 2f);
    }

    private void OnAreaChanged(Collider2D area)
    {
        var switcher = CameraAreaSwitcher.Instance;
        if (switcher == null) return;
        AudioManager.SetAmbience(area == switcher.DefaultArea ? outdoorAmbience : indoorAmbience, 0.8f);
    }
}
