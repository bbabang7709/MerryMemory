using TMPro;
using UnityEngine;

/// <summary>
/// 화면 왼쪽 위 "들고 있는 것: 편지 (시계탑 길)" 표시. 빈손이면 숨깁니다. (로드맵 4-1)
/// CarriedItem.Changed를 듣고, 시작할 때 한 번 직접 물어봅니다. (3-2b 현황 UI와 같은 방식)
/// </summary>
public class CarriedItemView : MonoBehaviour
{
    [SerializeField] private GameObject box;
    [SerializeField] private TMP_Text label;

    private void OnEnable() => CarriedItem.Changed += Show;
    private void OnDisable() => CarriedItem.Changed -= Show;
    private void Start() => Show(CarriedItem.Current);

    private void Show(CarryItem item)
    {
        box.SetActive(item != null);
        if (item != null) label.text = $"들고 있는 것: {item.label}";
    }
}
