using UnityEngine;

/// <summary>
/// 카드 조각 하나의 정보. (로드맵 3-1, GDD 5.1)
/// Project 창 우클릭 → Create → Merry Memory → Card Piece 로 만들고,
/// Assets/ScriptableObjects/CardPieces/ 에 5개(우표, 팝업 트리, 멜로디 칩, 리본, 봉투)를 둡니다.
///
/// 조각 = 마을 사람의 한마디(message) + 받는 사람 단서(clueDialogue). 이 구조는 바꾸지 않습니다.
/// </summary>
[CreateAssetMenu(menuName = "Merry Memory/Card Piece", fileName = "Piece_")]
public class CardPieceData : ScriptableObject
{
    [Tooltip("저장할 때 쓰는 고유 키. 한 번 정하면 바꾸지 마세요. (예: stamp)")]
    public string id;

    [Tooltip("카드 위 순서 (1~5). 현황 UI와 엔딩에서 이 순서로 놓입니다.")]
    [Min(1)] public int number = 1;

    [Tooltip("화면에 보이는 이름 (예: 우표)")]
    public string displayName;

    public Sprite icon;

    [Tooltip("이 조각을 만든 사람 (예: 우체국장)")]
    public string makerName;

    [Tooltip("만든 사람이 카드에 적은 한마디. 엔딩에서 카드가 펼쳐질 때 떠오릅니다.")]
    [TextArea(2, 4)] public string message;

    [Tooltip("조각을 받은 뒤 듣는 '받는 사람' 단서 대사")]
    public DialogueData clueDialogue;
}
