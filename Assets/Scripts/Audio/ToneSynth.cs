using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 효과음·음악 파일이 아직 없어서, 소리를 코드로 즉석에서 만들어 쓰는 임시 합성기. (로드맵 4-3, 5-2)
///   - Bell / Hum / Clunk : 4-3 시계탑 종, 콧노래, 틀림
///   - Carol(스타일)      : 「징글벨」 후렴 16마디 반복 음악 (타이틀 오르골 / 마을 / 엔딩)
///   - Wind               : 바람 소리 반복 (이음새 없이 이어지게)
///   - Footstep, Pickup, PieceGet, Door, Blip, Click, Stamp : 효과음
/// AudioLibrary에 진짜 파일을 넣으면 그쪽이 우선이고, 비어 있는 칸만 여기서 만든 소리로 채웁니다.
/// 한 번 만든 소리는 캐시해 두고 다시 씁니다. (「징글벨」 멜로디는 저작권이 끝난 곡)
/// 캐럴 한 곡을 만드는 데 약 0.5~1초가 걸려서, PrewarmMusic()으로 미리 다른 스레드에서 계산해 둡니다.
///   (계산은 순수 숫자 연산이라 다른 스레드에서 해도 되고, AudioClip 만들기만 메인 스레드에서)
/// </summary>
public static class ToneSynth
{
    private const int SampleRate = 44100;
    private static readonly Dictionary<string, AudioClip> cache = new();
    // 다른 스레드에서 미리 계산해 둔 소리 데이터 (key → 샘플)
    private static readonly ConcurrentDictionary<string, float[]> prepared = new();
    private static bool prewarmStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        prepared.Clear();
        prewarmStarted = false;
    }

    /// <summary>캐럴 3곡과 바람 소리를 다른 스레드에서 미리 계산합니다. (AudioManager가 처음 생길 때 부름)</summary>
    public static void PrewarmMusic()
    {
        if (prewarmStarted) return;
        prewarmStarted = true;
        Task.Run(() =>
        {
            prepared.TryAdd("wind", WindData());
            foreach (var style in new[] { CarolStyle.Village, CarolStyle.Ending, CarolStyle.MusicBox })
                prepared.TryAdd(CarolKey(style), CarolData(style));
        });
    }

    /// <summary>캐럴 편곡 스타일</summary>
    public enum CarolStyle { MusicBox, Village, Ending }

    // ───────────────────────── 4-3 ─────────────────────────

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
            Mathf.Exp(-t * 14f) * (0.55f * Sin(98f, t) + 0.25f * Sin(147f, t) + 0.2f * Noise(rng)));
    }

    // ───────────────────────── 5-2 효과음 ─────────────────────────

    /// <summary>눈 밟는 소리. variant(0~3)마다 조금씩 다른 소리</summary>
    public static AudioClip Footstep(int variant)
    {
        return Build($"footstep_{variant}", () =>
        {
            var rng = new System.Random(100 + variant);
            int n = Samples(0.11f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (Noise(rng) - lp) * 0.18f;                 // 거친 소리를 깎아 "사각"
                data[i] = lp * Mathf.Exp(-t * 38f) * Mathf.Min(1f, t * 600f) * 1.6f;
            }
            return data;
        });
    }

    /// <summary>물건을 집었을 때: 짧게 두 음 올라감</summary>
    public static AudioClip Pickup() => Build("pickup", () =>
    {
        var data = new float[Samples(0.5f)];
        AddBell(data, 0f, 783.99f, 0.35f, 9f);
        AddBell(data, 0.09f, 1046.5f, 0.35f, 7f);
        return data;
    });

    /// <summary>카드 조각을 얻었을 때: 도-미-솔-도 펼침화음</summary>
    public static AudioClip PieceGet() => Build("piece_get", () =>
    {
        var data = new float[Samples(1.6f)];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        for (int i = 0; i < notes.Length; i++) AddBell(data, i * 0.11f, notes[i], 0.32f, 2.6f);
        return data;
    });

    /// <summary>문 여닫는 쿵 소리</summary>
    public static AudioClip Door()
    {
        var rng = new System.Random(11);
        return Get("door", 0.4f, t =>
            Mathf.Exp(-t * 11f) * (0.6f * Sin(72f, t) + 0.25f * Sin(108f, t)) + 0.15f * Mathf.Exp(-t * 40f) * Noise(rng));
    }

    /// <summary>대사 한 줄이 나올 때 "톡"</summary>
    public static AudioClip Blip() =>
        Get("blip", 0.06f, t => 0.35f * Sin(1180f, t) * Mathf.Exp(-t * 70f) * Mathf.Min(1f, t * 2000f));

    /// <summary>버튼 누를 때 "틱"</summary>
    public static AudioClip Click() =>
        Get("click", 0.03f, t => 0.3f * Sin(2100f, t) * Mathf.Exp(-t * 160f));

    /// <summary>도장·분류함에 넣을 때 "쿵탁"</summary>
    public static AudioClip Stamp()
    {
        var rng = new System.Random(5);
        return Get("stamp", 0.25f, t =>
            Mathf.Exp(-t * 22f) * 0.6f * Sin(92f, t) + Mathf.Exp(-t * 90f) * 0.3f * Noise(rng));
    }

    // ───────────────────────── 5-2 반복 소리 ─────────────────────────

    /// <summary>바람 소리 (약 10초 반복, 끝과 처음이 자연스럽게 이어짐)</summary>
    public static AudioClip Wind() => Build("wind", WindData);

    private static float[] WindData()
    {
        var rng = new System.Random(3);
        int n = Samples(10.6f), fade = Samples(1.5f);
        var raw = new float[n + fade];
        float brown = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            float t = i / (float)SampleRate;
            brown = brown * 0.985f + Noise(rng) * 0.06f;        // 낮게 웅웅거리는 갈색 잡음
            float gust = 0.55f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 5.3f) + 0.15f * Mathf.Sin(2f * Mathf.PI * t / 2.1f + 1f);
            raw[i] = brown * gust * 1.8f;
        }
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = raw[i];
            if (i < fade) // 끝부분을 처음에 겹쳐서 이음새를 지움
            {
                float k = i / (float)fade;
                data[i] = raw[i] * k + raw[n + i] * (1f - k);
            }
        }
        return data;
    }

    // 「징글벨」 후렴: (음 이름, 박) — 16마디
    private static readonly (string note, float beats)[] Melody =
    {
        ("E",1),("E",1),("E",2), ("E",1),("E",1),("E",2), ("E",1),("G",1),("C",1.5f),("D",0.5f), ("E",4),
        ("F",1),("F",1),("F",1.5f),("F",0.5f), ("F",1),("E",1),("E",1),("E",0.5f),("E",0.5f), ("E",1),("D",1),("D",1),("E",1), ("D",2),("G",2),
        ("E",1),("E",1),("E",2), ("E",1),("E",1),("E",2), ("E",1),("G",1),("C",1.5f),("D",0.5f), ("E",4),
        ("F",1),("F",1),("F",1.5f),("F",0.5f), ("F",1),("E",1),("E",1),("E",0.5f),("E",0.5f), ("G",1),("G",1),("F",1),("D",1), ("C",4),
    };

    // 마디마다 베이스 화음의 뿌리음 (C, F, G)
    private static readonly string[] BassRoots =
    {
        "C","C","C","C","F","C","G","G",
        "C","C","C","C","F","C","G","C",
    };

    private static float Pitch(string note) => note switch
    {
        "C" => 523.25f, "D" => 587.33f, "E" => 659.25f, "F" => 698.46f, "G" => 783.99f, _ => 523.25f,
    };

    /// <summary>「징글벨」 후렴 16마디를 스타일에 맞게 편곡한 반복 음악</summary>
    public static AudioClip Carol(CarolStyle style) => Build(CarolKey(style), () => CarolData(style));

    private static string CarolKey(CarolStyle style) => $"carol_{style}";

    private static float[] CarolData(CarolStyle style)
    {
        float beat = style switch { CarolStyle.MusicBox => 0.52f, CarolStyle.Village => 0.42f, _ => 0.36f };
        int n = Samples(64 * beat);                               // 16마디 × 4박
        var data = new float[n];

        // 멜로디
        float pos = 0f;
        float decay = style == CarolStyle.MusicBox ? 3.2f : 2.4f;
        float octave = style == CarolStyle.MusicBox ? 2f : 1f;    // 오르골은 한 옥타브 위
        foreach (var (note, beats) in Melody)
        {
            AddBell(data, pos * beat, Pitch(note) * octave, 0.34f, decay, wrap: true);
            pos += beats;
        }

        // 베이스 (오르골은 생략): 1박 뿌리음, 3박 5도
        if (style != CarolStyle.MusicBox)
        {
            for (int bar = 0; bar < BassRoots.Length; bar++)
            {
                float root = Pitch(BassRoots[bar]) / 4f;              // 두 옥타브 아래
                AddBass(data, (bar * 4) * beat, root, beat * 1.8f, 0.22f);
                AddBass(data, (bar * 4 + 2) * beat, root * 1.5f, beat * 1.8f, 0.16f);
            }
        }

        // 엔딩: 마디 첫 박마다 반짝이는 높은 종
        if (style == CarolStyle.Ending)
        {
            for (int bar = 0; bar < BassRoots.Length; bar++)
                AddBell(data, bar * 4 * beat, Pitch(BassRoots[bar]) * 2f, 0.12f, 1.8f, wrap: true);
        }

        Normalize(data, 0.8f);
        return data;
    }

    // ───────────────────────── 도우미 ─────────────────────────

    private static int Samples(float seconds) => Mathf.CeilToInt(seconds * SampleRate);
    private static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
    private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2 - 1);

    /// <summary>종소리 한 음을 data의 start초 위치에 더합니다. wrap이면 끝을 넘친 꼬리를 앞으로 돌려 반복 이음새를 매끄럽게</summary>
    private static void AddBell(float[] data, float start, float f, float volume, float decay, bool wrap = false)
    {
        int s = Samples(start);
        int len = Samples(Mathf.Min(2f, 5f / decay)); // 이 뒤로는 거의 안 들림 (exp(-5) ≈ 0.7%)
        for (int i = 0; i < len; i++)
        {
            int idx = s + i;
            if (idx >= data.Length) { if (!wrap) break; idx -= data.Length; }
            float t = i / (float)SampleRate;
            float v = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 300f) *
                      (Sin(f, t) + 0.3f * Sin(f * 2f, t) * Mathf.Exp(-t * 3f) + 0.1f * Sin(f * 3f, t) * Mathf.Exp(-t * 6f));
            data[idx] += v * volume;
        }
    }

    private static void AddBass(float[] data, float start, float f, float length, float volume)
    {
        int s = Samples(start), len = Samples(length);
        for (int i = 0; i < len && s + i < data.Length; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Min(1f, t * 80f) * Mathf.Min(1f, (length - t) * 12f);
            data[s + i] += volume * env * (Sin(f, t) + 0.25f * Sin(f * 2f, t));
        }
    }

    private static void Normalize(float[] data, float peak)
    {
        float max = 0f;
        foreach (var v in data) max = Mathf.Max(max, Mathf.Abs(v));
        if (max <= 0f) return;
        float k = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= k;
    }

    private static AudioClip Get(string key, float seconds, Func<float, float> wave) => Build(key, () =>
    {
        var data = new float[Samples(seconds)];
        for (int i = 0; i < data.Length; i++) data[i] = wave(i / (float)SampleRate);
        return data;
    });

    private static AudioClip Build(string key, Func<float[]> make)
    {
        // 플레이 모드를 나가면 만든 클립이 사라질 수 있어서 "Unity식 null"도 다시 만듭니다.
        if (cache.TryGetValue(key, out var clip) && clip != null) return clip;

        // 미리 계산해 둔 게 있으면 그걸, 없으면 지금 계산
        if (!prepared.TryRemove(key, out var data)) data = make();
        clip = AudioClip.Create(key, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        cache[key] = clip;
        return clip;
    }
}
