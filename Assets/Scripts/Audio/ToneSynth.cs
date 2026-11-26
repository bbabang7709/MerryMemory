using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 파일이 아직 없어서, 소리를 코드로 즉석에서 만들어 쓰는 임시 합성기. (로드맵 4-3)
///   - Bell(note): 종소리 (맑게 울리고 천천히 사라짐)
///   - Hum(note, 길이): 콧노래 (부드러운 사인파)
///   - Clunk(): 틀렸을 때 "덜컥" 낮은 소리
/// 5단계에서 진짜 SFX 파일이 생기면 AudioClip 필드로 바꿔 끼우면 됩니다.
/// 한 번 만든 소리는 캐시해 두고 다시 씁니다.
/// </summary>
public static class ToneSynth
{
    private const int SampleRate = 44100;
    private static readonly Dictionary<string, AudioClip> cache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    public static AudioClip Bell(BellNote note)
    {
        float f = BellNotes.Frequency(note);
        // 종은 배음이 정수배가 아니라서(2.0, 2.76, 5.4배…) "땡~" 하는 금속 느낌이 납니다.
        return Get($"bell_{note}", 2.2f, t =>
            Mathf.Exp(-t * 2.2f) * (
                0.60f * Sin(f, t) +
                0.25f * Sin(f * 2.0f, t) * Mathf.Exp(-t * 2f) +
                0.18f * Sin(f * 2.76f, t) * Mathf.Exp(-t * 3f) +
                0.10f * Sin(f * 5.4f, t) * Mathf.Exp(-t * 6f)) *
            Mathf.Min(1f, t * 400f)); // 시작 딸깍 소리 방지
    }

    public static AudioClip Hum(BellNote note, float seconds)
    {
        float f = BellNotes.Frequency(note) * 0.5f; // 사람 목소리 높이로 한 옥타브 아래
        return Get($"hum_{note}_{seconds:0.00}", seconds, t =>
        {
            float env = Mathf.Min(1f, t / 0.06f) * Mathf.Min(1f, (seconds - t) / 0.08f);
            return env * 0.35f * (Sin(f, t) + 0.3f * Sin(f * 2f, t) + 0.1f * Sin(f * 3f, t));
        });
    }

    public static AudioClip Clunk()
    {
        var rng = new System.Random(7);
        return Get("clunk", 0.35f, t =>
            Mathf.Exp(-t * 14f) * (0.55f * Sin(98f, t) + 0.25f * Sin(147f, t) + 0.2f * (float)(rng.NextDouble() * 2 - 1)));
    }

    private static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

    private static AudioClip Get(string key, float seconds, System.Func<float, float> wave)
    {
        // 플레이 모드를 나가면 만든 클립이 사라질 수 있어서 "Unity식 null"도 다시 만듭니다.
        if (cache.TryGetValue(key, out var clip) && clip != null) return clip;

        int n = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = wave(i / (float)SampleRate);

        clip = AudioClip.Create(key, n, 1, SampleRate, false);
        clip.SetData(data, 0);
        cache[key] = clip;
        return clip;
    }
}
