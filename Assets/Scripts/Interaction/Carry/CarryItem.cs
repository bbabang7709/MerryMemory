using System;

/// <summary>
/// 손에 들고 다니는 물건 하나의 정보. (로드맵 4-1)
///   id    : 물건 고유 키 (예: letter_1)
///   label : 화면에 보일 이름 (예: 편지 (시계탑 길))
///   tag   : 퍼즐이 맞는지 판단할 꼬리표 (예: 편지의 받는 곳 "clocktower")
/// 편지(4-1), 공방 부품(4-2), 글자 조각(4-5)에 같이 씁니다.
/// </summary>
[Serializable]
public class CarryItem
{
    public string id;
    public string label;
    public string tag;
}
