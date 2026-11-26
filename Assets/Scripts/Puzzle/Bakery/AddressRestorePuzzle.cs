using UnityEngine;

/// <summary>
/// 빵집: 번진 봉투 주소 복원 퍼즐 (로드맵 4-5). 4-2처럼 화면(View)이 따로 있는 UI 퍼즐입니다.
///   봉투 주소  To. ?? [칸0][칸1]의 ?에[칸2]
///   ① 마을에서 글자 스티커 3장(LetterFragmentPickup)을 줍는다 (플래그로 기억)
///   ② 작업대(EnvelopeDesk)에서 OpenPuzzle → 칸을 누를 때마다 글자가 바뀜 (CycleSlot)
///   ③ [확인] → IsCorrect면 화면이 성공 메시지 후 Finish → MarkSolved (보상은 CardPieceReward: 봉투 조각)
/// 정답은 "너 / 머 / 게" → To. ?? 너머의 ?에게  (엔딩 주소가 미리 다 드러나지 않게 일부만 복원)
/// </summary>
public class AddressRestorePuzzle : PuzzleBase
{
    [Tooltip("글자 스티커 3장 (번호 = 이 배열의 순서)")]
    [SerializeField] private LetterFragmentPickup[] fragments;

    [Tooltip("칸마다 들어가야 할 글자 (왼쪽 칸부터)")]
    [SerializeField] private string[] answer = { "너", "머", "게" };

    [Tooltip("작업대 위 번진 봉투 그림 (풀면 숨김: 봉투는 카드 조각이 됨)")]
    [SerializeField] private GameObject envelope;

    [SerializeField] private AddressRestoreView view;

    // 칸마다 끼워 둔 스티커 번호 (fragments의 인덱스). -1 = 빈칸
    private int[] placed;

    public int FragmentCount => fragments.Length;
    public int SlotCount => answer.Length;
    public bool HasAllFragments => CollectedCount >= FragmentCount;

    private void Awake()
    {
        placed = new int[answer.Length];
        ClearSlots();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < placed.Length; i++) placed[i] = -1;
    }

    /// <summary>지금까지 주운 스티커 수</summary>
    public int CollectedCount
    {
        get
        {
            int count = 0;
            foreach (var fragment in fragments)
            {
                if (fragment != null && fragment.IsCollected) count++;
            }

            return count;
        }
    }

    /// <summary>화면에 보일 "가진 글자" 목록. 예: 머  게  너</summary>
    public string LettersSummary
    {
        get
        {
            var letters = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++) letters[i] = fragments[i] != null ? fragments[i].Letter : "?";
            return string.Join("   ", letters);
        }
    }

    /// <summary>조립 화면을 엽니다. 이미 풀었거나 스티커가 모자라면 열지 않습니다.</summary>
    public override void OpenPuzzle()
    {
        if (IsSolved || !HasAllFragments) return;

        GameplayLock.Acquire(this);
        ClearSlots(); // 열 때마다 빈칸부터
        view.Open(this);
    }

    /// <summary>이 칸에 지금 보이는 글자. 빈칸이면 "?"</summary>
    public string LetterAt(int slot) => placed[slot] < 0 ? "?" : fragments[placed[slot]].Letter;

    /// <summary>빈칸이 하나라도 있는지</summary>
    public bool HasEmptySlot()
    {
        foreach (var p in placed) if (p < 0) return true;
        return false;
    }

    /// <summary>스티커 fragment가 slot이 아닌 다른 칸에 이미 끼워져 있는지</summary>
    private bool IsUsedElsewhere(int fragment, int slot)
    {
        for (int i = 0; i < placed.Length; i++)
            if (i != slot && placed[i] == fragment) return true;
        return false;
    }

    /// <summary>
    /// 칸을 누르면 부릅니다(화면의 칸 버튼). 그 칸의 글자를 "다음 스티커"로 바꿉니다.
    /// 순환: 빈칸 → 스티커 0 → 1 → … → 빈칸. 다른 칸에서 이미 쓰고 있는 스티커는 건너뜁니다.
    /// 빈칸도 순환에 넣어서, 세 칸이 다 찬 뒤에도 한 칸을 비워 글자를 옮길 수 있습니다.
    /// </summary>
    public void CycleSlot(int slot)
    {
        // 상태 번호: 0 = 빈칸, 1..n = 스티커 0..n-1 → 전체 n+1개를 % (n + 1)로 돌림
        int n = fragments.Length;
        int state = placed[slot] + 1;
        for (int step = 1; step <= n + 1; step++)
        {
            int candidate = (state + step) % (n + 1) - 1; // -1이면 빈칸 (언제든 갈 수 있음)
            if (candidate < 0 || !IsUsedElsewhere(candidate, slot))
            {
                placed[slot] = candidate;
                return;
            }
        }
    }

    /// <summary>모든 칸이 정답 글자인지 (화면의 [확인] 버튼이 부름)</summary>
    public bool IsCorrect()
    {
        for (int i = 0; i < placed.Length; i++)
        {
            if (placed[i] < 0 || fragments[placed[i]].Letter != answer[i]) return false;
        }

        return true;
    }

    /// <summary>화면을 닫고 잠금을 풉니다. (Esc, 또는 Finish에서)</summary>
    public void ClosePuzzle()
    {
        view.Close();
        GameplayLock.Release(this);
    }

    /// <summary>맞힌 뒤 화면이 성공 메시지를 보여 주고 부릅니다: 닫고 → 봉투 거두기 → 풀림</summary>
    public void Finish()
    {
        ClosePuzzle();
        // 봉투가 카드 조각이 되므로 작업대에서 거둠
        if (envelope != null)
        {
            envelope.SetActive(false);
        }

        MarkSolved(); // → CardPieceReward가 봉투 조각 + 빵집 사장 단서
    }

    /// <summary>예전에 푼 퍼즐로 복원될 때 (PuzzleBase.Start가 부름): 작업대 봉투를 숨김</summary>
    protected override void OnRestoredSolved()
    {
        if (envelope != null)
        {
            envelope.SetActive(false);
        }
    }

    // 화면이 열린 채로 오브젝트가 꺼지면 잠금이 남지 않게
    private void OnDisable() => GameplayLock.Release(this);
}
