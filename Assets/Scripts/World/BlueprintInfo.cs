using UnityEngine;

/// <summary>
/// 청사진 맵 오브젝트의 설계 메모. Docs/MAP.md의 ID와 설명을 Inspector에서 바로 볼 수 있게 붙여 둡니다.
/// 실제 그림·기능으로 바꿀 때 지워도 됩니다.
/// </summary>
public class BlueprintInfo : MonoBehaviour
{
    public string id;
    [TextArea(2, 6)] public string note;
}
