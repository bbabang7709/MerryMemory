using NUnit.Framework;

/// <summary>
/// TextFormatter 조사 처리 테스트.
/// Window → General → Test Runner → EditMode 탭 → Run All 로 실행합니다.
/// </summary>
public class TextFormatterTests
{
    // ── 받침 판별 ──

    [TestCase('가', false)]
    [TestCase('각', true)]
    [TestCase('민', true)]
    [TestCase('나', false)]
    [TestCase('힣', true)]
    [TestCase('a', false)]
    [TestCase('7', false)]
    public void HasFinalConsonant(char c, bool expected)
    {
        Assert.AreEqual(expected, TextFormatter.HasFinalConsonant(c));
    }

    [TestCase('철', true)]
    [TestCase('별', true)]
    [TestCase('민', false)]
    [TestCase('나', false)]
    public void HasRieulFinal(char c, bool expected)
    {
        Assert.AreEqual(expected, TextFormatter.HasRieulFinal(c));
    }

    // ── 이름 + 조사 (받침 있음 / 없음) ──

    [TestCase("지민", "{이름}{이/가}", "지민이")]
    [TestCase("하나", "{이름}{이/가}", "하나가")]
    [TestCase("지민", "{이름}{은/는}", "지민은")]
    [TestCase("하나", "{이름}{은/는}", "하나는")]
    [TestCase("지민", "{이름}{을/를}", "지민을")]
    [TestCase("하나", "{이름}{을/를}", "하나를")]
    [TestCase("지민", "{이름}{과/와}", "지민과")]
    [TestCase("하나", "{이름}{과/와}", "하나와")]
    [TestCase("지민", "{이름}{아/야}", "지민아")]
    [TestCase("하나", "{이름}{아/야}", "하나야")]
    public void ParticleFollowsFinalConsonant(string name, string text, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format(text, name));
    }

    // ── (으)로: ㄹ 받침이면 "로" ──

    [TestCase("지민", "지민으로")]
    [TestCase("하나", "하나로")]
    [TestCase("철", "철로")]
    [TestCase("별", "별로")]
    public void Euro(string name, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format("{이름}{으로/로}", name));
    }

    // ── 호칭용 {이} ──

    [TestCase("지민", "지민이는 어디 갔어?")]
    [TestCase("하나", "하나는 어디 갔어?")]
    public void CallingSuffix(string name, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format("{이름}{이}는 어디 갔어?", name));
    }

    // ── 한글이 아닌 이름: 받침이 없는 소리로 끝나는 경우 ──

    [TestCase("Alex", "{이름}{이/가}", "Alex가")]
    [TestCase("R2", "{이름}{을/를}", "R2를")]
    [TestCase("Kate", "{이름}{은/는}", "Kate는")]
    [TestCase("Lily", "{이름}{아/야}", "Lily야")]
    [TestCase("No.9", "{이름}{이/가}", "No.9가")]
    public void NonHangulTreatedAsNoFinalConsonant(string name, string text, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format(text, name));
    }

    // ── 숫자는 읽는 소리로 (6단계) ──

    [TestCase("7", "{이름}{이/가}", "7이")]     // 칠
    [TestCase("3", "{이름}{을/를}", "3을")]     // 삼
    [TestCase("0", "{이름}{은/는}", "0은")]     // 영
    [TestCase("1", "{이름}{으로/로}", "1로")]   // 일: ㄹ 받침 뒤 "로"
    [TestCase("8", "{이름}{으로/로}", "8로")]   // 팔
    [TestCase("6", "{이름}{으로/로}", "6으로")] // 육
    [TestCase("5", "{이름}{이/가}", "5가")]     // 오
    public void DigitsUseTheirKoreanReading(string name, string text, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format(text, name));
    }

    // ── 영문은 끝 글자로 어림 (6단계) ──

    [TestCase("Tom", "{이름}{이/가}", "Tom이")]
    [TestCase("Ian", "{이름}{아/야}", "Ian아")]
    [TestCase("Kang", "{이름}{은/는}", "Kang은")]
    [TestCase("Paul", "{이름}{으로/로}", "Paul로")]
    [TestCase("PAUL", "{이름}{이/가}", "PAUL이")]
    [TestCase("Tom", "{이름}{이}는", "Tom이는")]
    public void LatinNamesUseLastLetterSound(string name, string text, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format(text, name));
    }

    // ── 받침 ㄹ 이름 (로드맵 6단계 이름 예외) ──

    [TestCase("별", "{이름}{으로/로}", "별로")]
    [TestCase("별", "{이름}{이/가}", "별이")]
    [TestCase("별", "{이름}{아/야}", "별아")]
    public void RieulFinalName(string name, string text, string expected)
    {
        Assert.AreEqual(expected, TextFormatter.Format(text, name));
    }

    // ── 문장, 여러 토큰, 특이한 경우 ──

    [Test]
    public void FullSentence()
    {
        Assert.AreEqual(
            "지민이 카드를 들고 하나와 함께 광장으로 갔다.",
            TextFormatter.Format("{이름}{이/가} 카드{을/를} 들고 하나{과/와} 함께 광장{으로/로} 갔다.", "지민"));
    }

    [Test]
    public void ReversedPairOrderStillWorks()
    {
        Assert.AreEqual("지민이", TextFormatter.Format("{이름}{가/이}", "지민"));
        Assert.AreEqual("하나가", TextFormatter.Format("{이름}{가/이}", "하나"));
    }

    [Test]
    public void ParticleAfterPlainWordWithoutNameToken()
    {
        Assert.AreEqual("카드를", TextFormatter.Format("카드{을/를}", "지민"));
        Assert.AreEqual("편지를", TextFormatter.Format("편지{을/를}", "지민"));
    }

    [Test]
    public void ParticleAtStartUsesNoFinalConsonantForm()
    {
        Assert.AreEqual("가", TextFormatter.Format("{이/가}", "지민"));
    }

    [Test]
    public void UnknownBracesAreLeftAlone()
    {
        Assert.AreEqual("{foo} 지민", TextFormatter.Format("{foo} {이름}", "지민"));
    }

    [Test]
    public void NullAndEmptyTextReturnAsIs()
    {
        Assert.IsNull(TextFormatter.Format(null, "지민"));
        Assert.AreEqual("", TextFormatter.Format("", "지민"));
        Assert.AreEqual("안녕", TextFormatter.Format("안녕", "지민"));
    }

    [Test]
    public void AttachParticleHelper()
    {
        Assert.AreEqual("지민으로", TextFormatter.AttachParticle("지민", "으로/로"));
        Assert.AreEqual("철로", TextFormatter.AttachParticle("철", "으로/로"));
        Assert.AreEqual("하나로", TextFormatter.AttachParticle("하나", "으로/로"));
    }
}
