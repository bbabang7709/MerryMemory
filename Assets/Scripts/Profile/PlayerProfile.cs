using System;

/// <summary>
/// 명찰에 적는 주인공 정보: 이름 + 성별.
/// 이름 규칙(최대 길이, 빈 이름 금지)도 여기서 한곳에 정해 둡니다.
/// </summary>
[Serializable]
public class PlayerProfile
{
    /// <summary>이름 최대 글자 수. 대화창 줄 넘침 방지 (GDD 6장: 한글 6자 정도)</summary>
    public const int MaxNameLength = 6;

    /// <summary>명찰을 거치지 않고 Village를 바로 실행했을 때 쓰는 기본값.</summary>
    public const string DefaultName = "집배원";

    public string Name;
    public Gender Gender;

    public PlayerProfile(string name, Gender gender)
    {
        Name = name;
        Gender = gender;
    }

    public static PlayerProfile CreateDefault() => new(DefaultName, Gender.Female);

    /// <summary>
    /// 이름으로 쓸 수 있는지 검사합니다. 쓸 수 없으면 이유를 돌려줍니다.
    /// 앞뒤 공백은 검사 전에 잘라서 봅니다.
    /// </summary>
    public static bool IsValidName(string name, out string reason)
    {
        string trimmed = name?.Trim() ?? "";

        if (trimmed.Length == 0)
        {
            reason = "이름을 적어 주세요.";
            return false;
        }
        if (trimmed.Length > MaxNameLength)
        {
            reason = $"이름은 {MaxNameLength}글자까지 쓸 수 있어요.";
            return false;
        }
        // {이름}{이/가} 같은 대사 치환 표기와 섞이지 않도록 중괄호는 막습니다.
        if (trimmed.IndexOf('{') >= 0 || trimmed.IndexOf('}') >= 0)
        {
            reason = "{ } 기호는 쓸 수 없어요.";
            return false;
        }

        reason = null;
        return true;
    }

    public override string ToString() => $"{Name} ({Gender})";
}
