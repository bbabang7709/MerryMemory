using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 대사 속 표기를 실제 글자로 바꿉니다. (GDD 6장, 로드맵 2-3)
///
///   {이름}         → 주인공 이름
///   {이/가} {은/는} {을/를} {과/와} {아/야} {으로/로} ...
///                  → 바로 앞 글자의 받침을 보고 둘 중 하나를 고름
///   {이}           → 호칭용. 받침 있으면 "이", 없으면 생략 ("지민이는" / "하나는")
///
/// 예) Format("{이름}{이/가} 왔어요.", "지민") → "지민이 왔어요."
///     Format("{이름}{이/가} 왔어요.", "하나") → "하나가 왔어요."
///
/// 한글이 아닌 글자 뒤 (6단계):
///   숫자 → 읽는 소리로: 0(영) 3(삼) 6(육)은 받침 있음, 1(일) 7(칠) 8(팔)은 ㄹ 받침, 나머지는 없음  ("7이", "1로", "2를")
///   영문 → 끝 글자로 어림: l은 ㄹ 받침(Paul로), m·n·ng는 받침 있음(Tom이, Kang이), 나머지는 없음(Alex가)
///   그 밖의 기호 등은 받침 없음
/// 게임 코드와 따로 컴파일되는 작은 어셈블리라서 EditMode 테스트에서 바로 검사할 수 있습니다.
/// </summary>
public static class TextFormatter
{
    public const string NameToken = "{이름}";

    // {이름}, {이}, {앞/뒤} 형태의 토큰
    private static readonly Regex Token = new(@"\{(이름|이|[^{}/]+/[^{}]+)\}", RegexOptions.Compiled);

    // 받침 있을 때 쓰는 꼴 → 받침 없을 때 쓰는 꼴. 대사에 {가/이}처럼 순서를 바꿔 써도 알아봅니다.
    private static readonly Dictionary<string, string> ConsonantToVowelForm = new()
    {
        { "이", "가" }, { "은", "는" }, { "을", "를" }, { "과", "와" }, { "아", "야" },
        { "으로", "로" }, { "이랑", "랑" }, { "이나", "나" }, { "이라", "라" },
        { "이야", "야" }, { "이에요", "예요" }, { "이여", "여" },
    };

    /// <summary>{이름}을 name으로 바꾸고 조사 표기를 앞 글자에 맞춰 고릅니다.</summary>
    public static string Format(string text, string name)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        int last = 0;
        foreach (Match m in Token.Matches(text))
        {
            sb.Append(text, last, m.Index - last);
            sb.Append(Resolve(m.Groups[1].Value, sb, name ?? ""));
            last = m.Index + m.Length;
        }
        sb.Append(text, last, text.Length - last);
        return sb.ToString();
    }

    /// <summary>단어 뒤에 조사를 붙입니다. 예) AttachParticle("지민", "으로/로") → "지민으로"</summary>
    public static string AttachParticle(string word, string pair) => Format(word + "{" + pair + "}", "");

    private static string Resolve(string token, StringBuilder before, string name)
    {
        if (token == "이름") return name;

        var final = FinalOf(before);
        if (token == "이") return final != Final.None ? "이" : "";

        int slash = token.IndexOf('/');
        string a = token.Substring(0, slash);
        string b = token.Substring(slash + 1);

        // 어느 쪽이 '받침 있을 때' 꼴인지 정함 (모르는 짝이면 앞쪽을 받침 있을 때로 봄)
        string withConsonant = a, withoutConsonant = b;
        if (!ConsonantToVowelForm.ContainsKey(a) && ConsonantToVowelForm.ContainsKey(b))
        {
            withConsonant = b;
            withoutConsonant = a;
        }

        bool consonant = final != Final.None;
        // (으)로는 ㄹ 받침 뒤에서도 "로": 철로, 별로, 1로, Paul로
        if (withConsonant == "으로" && final == Final.Rieul) consonant = false;

        return consonant ? withConsonant : withoutConsonant;
    }

    private enum Final { None, Consonant, Rieul }

    /// <summary>지금까지 만든 글(before)의 마지막 글자 소리가 받침으로 끝나는지 (한글·숫자·영문)</summary>
    private static Final FinalOf(StringBuilder before)
    {
        if (before.Length == 0) return Final.None;
        char c = before[before.Length - 1];

        if (c >= '가' && c <= '힣')
            return HasRieulFinal(c) ? Final.Rieul : HasFinalConsonant(c) ? Final.Consonant : Final.None;

        switch (c)
        {
            case '0': case '3': case '6': return Final.Consonant; // 영, 삼, 육
            case '1': case '7': case '8': return Final.Rieul;     // 일, 칠, 팔
        }
        if (c >= '0' && c <= '9') return Final.None;            // 이, 사, 오, 구

        char l = char.ToLowerInvariant(c);
        if (l == 'l') return Final.Rieul;                       // Paul → 폴
        if (l == 'm' || l == 'n') return Final.Consonant;       // Tom → 톰, Ian → 이안
        if (l == 'g' && before.Length >= 2 && char.ToLowerInvariant(before[before.Length - 2]) == 'n')
            return Final.Consonant;                             // Kang → 강
        return Final.None;
    }

    /// <summary>
    /// 한글 음절의 받침 여부. 한글 음절은 '가'부터 순서대로 있고,
    /// 28개마다 받침 없는 글자가 옵니다. 한글이 아니면 false.
    /// </summary>
    public static bool HasFinalConsonant(char c)
    {
        if (c < '가' || c > '힣') return false;
        return (c - '가') % 28 != 0;
    }

    /// <summary>받침이 ㄹ인지. (받침 번호 8 = ㄹ)</summary>
    public static bool HasRieulFinal(char c)
    {
        if (c < '가' || c > '힣') return false;
        return (c - '가') % 28 == 8;
    }
}
