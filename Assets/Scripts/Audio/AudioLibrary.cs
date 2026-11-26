using UnityEngine;

/// <summary>배경 음악 종류</summary>
public enum Bgm { None, Title, Village, Ending }

/// <summary>효과음 종류</summary>
public enum Sfx { Footstep, Pickup, PieceGet, Door, Blip, Click, Stamp, Wrong, MidnightBell }

/// <summary>
/// 게임에서 쓰는 소리 목록과 기본 볼륨 (로드맵 5-2). Assets/Resources/AudioLibrary.asset 하나만 둡니다.
/// 칸에 진짜 소리 파일을 넣으면 그 파일을, 비워 두면 ToneSynth가 코드로 만든 임시 소리를 씁니다.
///   → 지금은 전부 비워 둔 채로도 "소리 없는 구간"이 없고, 파일이 생기는 대로 하나씩 갈아 끼우면 됨
/// </summary>
[CreateAssetMenu(menuName = "Merry Memory/Audio Library", fileName = "AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    [Header("배경 음악 (반복). 비우면 「징글벨」 즉석 편곡")]
    public AudioClip titleBgm;
    public AudioClip villageBgm;
    public AudioClip endingBgm;

    [Header("환경음 (반복). 비우면 즉석 바람 소리")]
    public AudioClip wind;

    [Header("효과음. 비우면 즉석 효과음")]
    [Tooltip("여러 개 넣으면 걸을 때마다 무작위로 하나")]
    public AudioClip[] footsteps;
    public AudioClip pickup;
    public AudioClip pieceGet;
    public AudioClip door;
    public AudioClip blip;
    public AudioClip click;
    public AudioClip stamp;
    public AudioClip wrong;
    [Tooltip("엔딩: 자정을 알리는 큰 종")]
    public AudioClip midnightBell;

    [Header("볼륨")]
    [Range(0f, 1f)] public float bgmVolume = 0.45f;
    [Range(0f, 1f)] public float ambienceVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    public AudioClip GetBgm(Bgm id) => id switch
    {
        Bgm.Title => titleBgm != null ? titleBgm : ToneSynth.Carol(ToneSynth.CarolStyle.MusicBox),
        Bgm.Village => villageBgm != null ? villageBgm : ToneSynth.Carol(ToneSynth.CarolStyle.Village),
        Bgm.Ending => endingBgm != null ? endingBgm : ToneSynth.Carol(ToneSynth.CarolStyle.Ending),
        _ => null,
    };

    public AudioClip Wind => wind != null ? wind : ToneSynth.Wind();

    public AudioClip GetSfx(Sfx id) => id switch
    {
        Sfx.Footstep => footsteps != null && footsteps.Length > 0
            ? footsteps[Random.Range(0, footsteps.Length)]
            : ToneSynth.Footstep(Random.Range(0, 4)),
        Sfx.Pickup => pickup != null ? pickup : ToneSynth.Pickup(),
        Sfx.PieceGet => pieceGet != null ? pieceGet : ToneSynth.PieceGet(),
        Sfx.Door => door != null ? door : ToneSynth.Door(),
        Sfx.Blip => blip != null ? blip : ToneSynth.Blip(),
        Sfx.Click => click != null ? click : ToneSynth.Click(),
        Sfx.Stamp => stamp != null ? stamp : ToneSynth.Stamp(),
        Sfx.Wrong => wrong != null ? wrong : ToneSynth.Clunk(),
        Sfx.MidnightBell => midnightBell != null ? midnightBell : ToneSynth.MidnightBell(),
        _ => null,
    };
}
