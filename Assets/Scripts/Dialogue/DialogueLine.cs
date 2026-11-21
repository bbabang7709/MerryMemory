using UnityEngine;

/// <summary>대사 한 줄. 화자가 비어 있으면 이름표 없이 지문처럼 표시됩니다.</summary>
[System.Serializable]
public class DialogueLine
{
    public string speaker;               // 화자 이름 ("우체국장", "{이름}")
    [TextArea(2, 4)] public string text;
}
