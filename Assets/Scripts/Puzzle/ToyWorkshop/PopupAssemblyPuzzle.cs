using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장난감 공방: 망가진 팝업 트리 장치 재조립 퍼즐 (로드맵 4-2). 처음 만드는 UI 퍼즐입니다.
///   ① 부품 3개를 작업대에 올려놓는다 (Workbench가 AddPart를 부름)
///   ② 다 모이면 작업대에서 OpenPuzzle() → 잠금 + 조립 화면(PopupAssemblyView) 열기
///   ③ 화면에서 부품을 고르면 TryInsert(부품) → 맞는 순서면 끼워지고, 틀리면 처음부터
///   ④ 다 끼우면 화면이 성공 메시지를 잠깐 보여 준 뒤 Finish() → 닫고 MarkSolved (보상은 CardPieceReward)
/// 퍼즐은 "규칙"만 압니다. 버튼·글자는 화면(View)이 그리고, 결과(InsertResult)만 받아 갑니다.
/// </summary>
public class PopupAssemblyPuzzle : PuzzleBase
{
    /// <summary>TryInsert 결과. 화면이 이걸 보고 메시지를 고릅니다.</summary>
    public enum InsertResult { Correct, Wrong, Completed }

    [Tooltip("맞는 조립 순서 (부품 tag). 톱니바퀴 → 태엽 스프링 → 종이 트리 판")]
    [SerializeField] private string[] correctOrder = { "popup_gear", "popup_spring", "popup_board" };

    [Tooltip("공방 바닥의 부품 3개 (복원 때 숨김)")]
    [SerializeField] private CarryPickup[] partPickups;

    [SerializeField] private PopupAssemblyView view;

    // 작업대에 올려 둔 부품 tag (HashSet이라 같은 부품을 두 번 올려도 한 번만 셈)
    private readonly HashSet<string> partsOnBench = new();
    // 조립 화면에서 지금까지 맞게 끼운 부품 tag (순서대로)
    private readonly List<string> inserted = new();

    /// <summary>작업대에 올려 둔 부품 수 / 필요한 부품 수</summary>
    public int PartsOnBench => partsOnBench.Count;
    public int PartsNeeded => correctOrder.Length;
    public bool HasAllParts => PartsOnBench >= PartsNeeded;

    /// <summary>지금까지 맞게 끼운 부품 (화면 칸 그리기용, 읽기만)</summary>
    public IReadOnlyList<string> Inserted => inserted;

    /// <summary>이 tag가 이 장치의 부품인지 (Workbench가 씀)</summary>
    public bool IsPart(string tag) => System.Array.IndexOf(correctOrder, tag) >= 0;

    /// <summary>부품을 작업대에 올려놓습니다. (Workbench가 손에서 꺼내서 부름) 부품이 아니면 무시.</summary>
    public void AddPart(CarryItem item)
    {
        if (item != null && IsPart(item.tag))
        {
            partsOnBench.Add(item.tag);
        }
    }

    /// <summary>조립 화면을 엽니다. 이미 풀었거나 부품이 모자라면 열지 않습니다.</summary>
    public override void OpenPuzzle()
    {
        if (IsSolved || !HasAllParts) return;

        GameplayLock.Acquire(this); // 화면이 열린 동안 캐릭터가 움직이지 않게
        inserted.Clear();           // 열 때마다 처음부터
        view.Open(this);
    }

    /// <summary>
    /// 부품 하나를 끼워 봅니다. 화면의 부품 버튼이 부릅니다.
    /// 다음 차례(correctOrder[inserted.Count])와 같으면 끼우고 Correct/Completed, 다르면 처음부터 Wrong.
    /// </summary>
    public InsertResult TryInsert(string partTag)
    {
        if (inserted.Count >= correctOrder.Length) return InsertResult.Completed;

        var nextTag = correctOrder[inserted.Count];
        if (partTag == nextTag)
        {
            inserted.Add(partTag);

            if (inserted.Count == correctOrder.Length) return InsertResult.Completed;
            
            return InsertResult.Correct;
        }

        inserted.Clear();
        return InsertResult.Wrong;
    }

    /// <summary>화면을 닫고 잠금을 풉니다. (Esc, 또는 Finish에서)</summary>
    public void ClosePuzzle()
    {
        view.Close();
        GameplayLock.Release(this);
    }

    /// <summary>다 끼운 뒤 화면이 성공 메시지를 보여 주고 부릅니다: 닫고 → 풀림</summary>
    public void Finish()
    {
        ClosePuzzle();
        MarkSolved(); // → CardPieceReward가 팝업 트리 조각 + 공방 주인 단서
    }

    /// <summary>예전에 푼 퍼즐로 복원될 때 (PuzzleBase.Start가 부름): 바닥의 부품을 숨김</summary>
    protected override void OnRestoredSolved()
    {
        foreach (var part in partPickups)
        {
            if (part == null) continue;
            part.gameObject.SetActive(false);
        }
    }

    // 화면이 열린 채로 오브젝트가 꺼지면 잠금이 남지 않게
    private void OnDisable() => GameplayLock.Release(this);
}
