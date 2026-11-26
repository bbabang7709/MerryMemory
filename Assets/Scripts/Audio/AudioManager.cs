using System.Collections;
using UnityEngine;

/// <summary>
/// 소리 담당 (로드맵 5-2). 씬에 놓을 필요 없이, 처음 부르는 순간 스스로 생겨서 씬이 바뀌어도 남습니다. (SceneLoader와 같은 방식)
///   - AudioManager.PlayBgm(Bgm.Village)  : 배경 음악을 부드럽게 바꿈 (지금 곡은 줄어들고 새 곡은 커짐 = 크로스페이드)
///   - AudioManager.PlaySfx(Sfx.Pickup)   : 효과음 한 번
///   - AudioManager.SetAmbience(0.2f)     : 바람 소리 크기 (바깥 1, 실내 0.2 등)
/// 소리 목록은 Resources/AudioLibrary.asset (비어 있는 칸은 ToneSynth의 즉석 소리)
///
/// 왜 AudioSource가 BGM용으로 두 개인가?
///   하나로는 "줄어드는 곡"과 "커지는 곡"을 동시에 틀 수 없어서, 번갈아 쓰는 두 개(bgm[0], bgm[1])를 둡니다.
///   active = 지금 주로 들리는 쪽 번호. 새 곡은 반대쪽(1 - active)에서 시작합니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private AudioLibrary library;
    private readonly AudioSource[] bgm = new AudioSource[2];
    private int active;
    private AudioSource ambience;
    private AudioSource sfx;

    private Bgm currentBgm = Bgm.None;
    private Coroutine bgmFade;
    private Coroutine ambienceFade;

    // ───────────────────────── 바깥에서 부르는 곳 ─────────────────────────

    public static void PlayBgm(Bgm id, float fadeSeconds = 1.5f) => GetOrCreate().ChangeBgm(id, fadeSeconds);

    /// <param name="volumeScale">이 효과음만 작게/크게 (1 = 기본)</param>
    /// <param name="pitchJitter">높낮이를 ±이만큼 무작위로 (발소리처럼 매번 조금씩 다르게)</param>
    public static void PlaySfx(Sfx id, float volumeScale = 1f, float pitchJitter = 0f) =>
        GetOrCreate().Play(id, volumeScale, pitchJitter);

    /// <param name="level">0 = 끔, 1 = AudioLibrary.ambienceVolume 그대로</param>
    public static void SetAmbience(float level, float fadeSeconds = 1f) => GetOrCreate().FadeAmbience(level, fadeSeconds);

    // ───────────────────────── 만들기 ─────────────────────────

    private static AudioManager GetOrCreate()
    {
        if (instance != null) return instance;

        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioManager>();
        instance.Init();
        return instance;
    }

    private void Init()
    {
        library = Resources.Load<AudioLibrary>("AudioLibrary");
        if (library == null) library = ScriptableObject.CreateInstance<AudioLibrary>(); // 에셋이 없어도 즉석 소리로 동작
        ToneSynth.PrewarmMusic(); // 즉석 캐럴은 만드는 데 시간이 걸려서 미리 (진짜 파일이 있으면 안 쓰임)

        bgm[0] = NewSource(true);
        bgm[1] = NewSource(true);
        ambience = NewSource(true);
        sfx = NewSource(false);

        ambience.clip = library.Wind;
        ambience.volume = 0f;
        ambience.Play();
    }

    private AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f; // 2D: 어디서든 같은 크기
        return s;
    }

    // ───────────────────────── 배경 음악 ─────────────────────────

    private void ChangeBgm(Bgm id, float fadeSeconds)
    {
        // 같은 곡이면 그대로 → 씬이 바뀌어도 끊기지 않고 이어짐
        if (id == currentBgm) return;

        currentBgm = id;

        // 새 곡은 반대쪽 소스에서 볼륨 0으로 시작 (Bgm.None이면 clip이 null → 조용해짐)
        var next = bgm[1 - active];
        next.clip = library.GetBgm(id);
        next.volume = 0f;
        if (next.clip) next.Play();

        var prev = bgm[active];
        active = 1 - active; // 이제 next가 주인공

        if (bgmFade != null) StopCoroutine(bgmFade);
        bgmFade = StartCoroutine(Crossfade(prev, next, fadeSeconds));
    }

    /// <summary>prev는 0으로 줄이고 멈춤, next는 0에서 bgmVolume까지 키움</summary>
    private IEnumerator Crossfade(AudioSource prev, AudioSource next, float seconds)
    {
        // unscaledDeltaTime: 일시정지·슬로모션에도 같은 속도. seconds가 0이면 반복 없이 즉시 바뀜
        float prevStart = prev.volume;
        float target = library.bgmVolume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds; // 진행률 0~1
            prev.volume = Mathf.Lerp(prevStart, 0f, k);
            next.volume = Mathf.Lerp(0f, target, k);
            yield return null;
        }
        prev.Stop();
        prev.volume = 0f;
        next.volume = target;
    }

    // ───────────────────────── 효과음 ─────────────────────────

    private void Play(Sfx id, float volumeScale, float pitchJitter)
    {
        // PlayOneShot은 앞 소리를 끊지 않고 겹쳐서 냄. pitch는 소스 전체에 걸리므로 겹친 소리도 같이 바뀜
        var clip = library.GetSfx(id);
        if (clip == null) return;
        sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfx.PlayOneShot(clip, library.sfxVolume * volumeScale);
    }

    // ───────────────────────── 환경음 (완성) ─────────────────────────

    private void FadeAmbience(float level, float seconds)
    {
        if (ambienceFade != null) StopCoroutine(ambienceFade);
        ambienceFade = StartCoroutine(FadeVolume(ambience, library.ambienceVolume * Mathf.Clamp01(level), seconds));
    }

    private static IEnumerator FadeVolume(AudioSource source, float target, float seconds)
    {
        float start = source.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        source.volume = target;
    }
}
