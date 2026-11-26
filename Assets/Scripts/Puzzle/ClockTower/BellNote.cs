/// <summary>종탑의 종 4개가 내는 음. (로드맵 4-3, MAP 7.4: 도·레·미·솔, 「징글벨」 기준)</summary>
public enum BellNote { Do, Re, Mi, Sol }

/// <summary>음 이름과 높이(Hz)</summary>
public static class BellNotes
{
    public static string Label(BellNote note) => note switch
    {
        BellNote.Do => "도",
        BellNote.Re => "레",
        BellNote.Mi => "미",
        _ => "솔",
    };

    /// <summary>5옥타브 도(C5)부터. 종소리답게 조금 높게.</summary>
    public static float Frequency(BellNote note) => note switch
    {
        BellNote.Do => 523.25f,
        BellNote.Re => 587.33f,
        BellNote.Mi => 659.25f,
        _ => 783.99f,
    };
}
