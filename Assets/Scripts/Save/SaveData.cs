using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장 파일(save.json)에 들어가는 내용. (로드맵 3-5)
/// JsonUtility가 읽고 쓸 수 있도록 [Serializable] + public 필드로만 씁니다.
/// (Dictionary, HashSet, 속성(property)은 JsonUtility가 저장하지 못함 → List로)
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>저장 형식 버전. 나중에 칸이 바뀌면 올려서 옛 파일을 구분합니다.</summary>
    public int version = 1;

    /// <summary>사람이 파일을 열어 볼 때 참고용 저장 시각</summary>
    public string savedAt;

    public string playerName;
    public Gender gender;

    /// <summary>모은 조각 id (CardProgress)</summary>
    public List<string> collectedPieces = new();

    /// <summary>켜진 진행 플래그 키 (GameFlags)</summary>
    public List<string> flags = new();

    /// <summary>위치가 저장됐는지. 명찰 직후 저장처럼 플레이어가 없을 때는 false</summary>
    public bool hasPlayerPosition;
    public Vector2 playerPosition;

    /// <summary>이어하기로 들어갈 씬 이름</summary>
    public string sceneName;
}
