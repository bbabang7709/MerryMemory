using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위의 카드 조각 현황 (로드맵 3-2, GDD 5.2-4).
/// 칸 5개와 "1 / 5" 글자. CardProgress.PieceCollected를 듣고 조각을 얻으면 칸을 채웁니다.
///
/// CardProgress는 이 UI를 모릅니다. 이 UI가 CardProgress를 "듣는" 쪽입니다.
///   - 시작할 때(Start): CardProgress에 직접 물어서 그림 (이미 모아 둔 조각, 불러온 저장)
///   - 그 뒤(이벤트): 새 조각을 얻을 때마다 다시 그리고, 그 칸을 "톡" 키움
/// 둘 다 같은 Refresh()를 써서 그리는 코드는 한 곳에만 있습니다.
/// </summary>
public class CardProgressView : MonoBehaviour
{
    [Tooltip("조각 5개. 카드 순서(number)대로 (Setup 메뉴가 채워 둠)")]
    [SerializeField] private CardPieceData[] pieces;

    [Tooltip("칸 5개. pieces와 같은 순서")]
    [SerializeField] private Image[] slots;

    [Tooltip("\"1 / 5\" 글자")]
    [SerializeField] private TMP_Text countText;

    [SerializeField] private Color emptyColor = new(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color filledColor = new(1f, 0.84f, 0.45f, 1f);

    private void OnEnable()
    {
        CardProgress.PieceCollected += OnPieceCollected;
    }

    private void OnDisable()
    {
        CardProgress.PieceCollected -= OnPieceCollected;
    }

    // 이벤트는 "앞으로 일어날 일"만 알려 주므로, 씬에 들어올 때 지금 상태를 한 번 직접 그립니다.
    private void Start()
    {
        Refresh();
    }

    private void OnPieceCollected(CardPieceData piece)
    {
        Refresh();
        int index = System.Array.IndexOf(pieces, piece);
        if (index >= 0 && index < slots.Length)   // 칸이 없는 조각이면 (-1) 연출 없이 넘어감
            StartCoroutine(SlotEffect(index, 0.5f));
    }

    /// <summary>모든 칸과 개수 글자를 CardProgress의 현재 상태대로 그립니다.</summary>
    private void Refresh()
    {
        // pieces와 slots 길이가 달라도 짧은 쪽까지만 돌아서 안전
        int length = Mathf.Min(pieces.Length, slots.Length);

        for (int i = 0; i < length; i++)
        {
            SetSlot(i, CardProgress.Has(pieces[i]));
        }

        countText.text = $"{CardProgress.Count} / {CardProgress.TotalPieces}";
    }

    /// <summary>i번 칸을 채우거나 비웁니다.</summary>
    private void SetSlot(int i, bool filled)
    {
        var slot = slots[i];
        var piece = pieces[i];
        // 아이콘 그림이 있으면 그림으로, 없으면 색 칸으로 (지금은 아이콘이 아직 없음)
        slot.sprite = filled && piece != null ? piece.icon : null;
        slot.color = filled ? (slot.sprite != null ? Color.white : filledColor) : emptyColor;
    }

    /// <summary>새로 채워진 칸이 1.4배로 커졌다가 duration초 동안 원래 크기로 돌아옵니다.</summary>
    private IEnumerator SlotEffect(int index, float duration)
    {
        slots[index].transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float next = Mathf.Lerp(1.4f, 1f, elapsed / duration);
            slots[index].transform.localScale = new Vector3(next, next, 1f);

            yield return null;
        }
        slots[index].transform.localScale = Vector3.one; // 마지막 값을 확실히 맞춤
    }
}
