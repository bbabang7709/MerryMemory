using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 눈 언덕: 얼어붙은 연못 미끄럼 퍼즐 (로드맵 4-4). 퍼즐은 "격자 규칙"만 압니다.
///   - 얼음 위에서는 한 방향으로 밀면, 바위나 연못 가장자리에 막힐 때까지 칸 단위로 미끄러짐
///   - 리본 나무 밑(goalCell)에 멈추면 성공 → Complete() → 리본을 거두고 MarkSolved (보상은 CardPieceReward)
/// 실제로 캐릭터를 움직이고 키를 읽는 쪽은 IceSlideMover입니다. (퍼즐 전용 이동 모드)
///
/// 칸 좌표 = 월드 칸 (x, y). 위쪽이 +y 입니다. (MAP 좌표 (x, y) → 월드 칸 (x, 47 − y))
///   연못 MAP (5,4) 10×7 → 월드 x 5~14, y 37~43 / 시작 MAP (10,11) → (10,36) / 리본 나무 밑 MAP (9,4) → (9,43)
/// </summary>
public class IceSlidePuzzle : PuzzleBase
{
    [Tooltip("얼음 칸 범위 (월드 칸). RectInt(x, y, 너비, 높이) → x ~ x+너비-1, y ~ y+높이-1")]
    [SerializeField] private RectInt pond = new(5, 37, 10, 7);

    [Tooltip("얼음에 올라서는 칸 (연못 바로 아래 땅). 처음 자리 / 다시 하기 때 돌아오는 곳")]
    [SerializeField] private Vector2Int startCell = new(10, 36);

    [Tooltip("리본 나무 바로 밑 칸. 여기서 멈추면 성공")]
    [SerializeField] private Vector2Int goalCell = new(9, 43);

    [Tooltip("얼음 위 바위 (월드 칸)")]
    [SerializeField] private Vector2Int[] rocks;

    [Tooltip("나무에 걸린 리본 그림 (풀면 숨김)")]
    [SerializeField] private GameObject ribbon;

    // 바위 칸 빨리 찾기용 (배열을 매번 훑지 않게)
    private HashSet<Vector2Int> rockSet;

    public Vector2Int StartCell => startCell;
    public RectInt Pond => pond;

    private void Awake() => rockSet = new HashSet<Vector2Int>(rocks ?? new Vector2Int[0]);

    /// <summary>칸 → 캐릭터를 세울 월드 위치 (발이 칸 아래쪽에 오게)</summary>
    public static Vector3 CellToWorld(Vector2Int cell) => new(cell.x + 0.5f, cell.y + 0.2f, 0f);

    /// <summary>
    /// 미끄러지는 중에 이 칸에 들어갈 수 있는지.
    /// 들어갈 수 있는 칸 = (연못 안이거나 시작 칸) 이면서 바위가 아닌 칸
    /// </summary>
    public bool IsWalkable(Vector2Int cell)
    {
        // RectInt.Contains는 오른쪽·위쪽 끝을 포함하지 않음 → x 5~14, y 37~43
        return (pond.Contains(cell) || cell == startCell) && !rockSet.Contains(cell);
    }

    /// <summary>
    /// from 칸에서 dir 방향(상하좌우 한 칸, 예: Vector2Int.up)으로 밀었을 때 멈추는 칸.
    /// 한 칸도 못 가면 from을 그대로 돌려줍니다.
    /// </summary>
    public Vector2Int Slide(Vector2Int from, Vector2Int dir)
    {
        // 다음 칸에 들어갈 수 있는 동안 한 칸씩 전진. 연못에는 끝이 있으므로 반드시 멈춤
        var cell = from;
        while (IsWalkable(cell + dir))
        {
            cell += dir;
        }

        return cell;
    }

    /// <summary>이 칸이 리본 나무 밑(성공 칸)인지</summary>
    public bool IsGoal(Vector2Int cell)
    {
        return cell == goalCell;
    }

    /// <summary>성공: 리본을 거두고 풀림 처리. (IceSlideMover가 캐릭터를 시작 칸으로 돌려놓은 뒤 부름)</summary>
    public void Complete()
    {
        if (IsSolved) return;

        if (ribbon) ribbon.SetActive(false);
        MarkSolved(); // → CardPieceReward가 리본 조각 + 눈 언덕 아이들 단서
    }

    /// <summary>예전에 푼 퍼즐로 복원될 때 (PuzzleBase.Start가 부름): 나무의 리본을 숨김</summary>
    protected override void OnRestoredSolved()
    {
        if (ribbon) ribbon.SetActive(false);
    }
}
