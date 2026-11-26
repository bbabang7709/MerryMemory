using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 얼음 연못의 "퍼즐 전용 이동 모드" (로드맵 4-4). 연못 아래 시작 칸에 놓이는 [F] 대상입니다.
///   - [F] 얼음에 올라서기 → 일반 이동을 잠그고(GameplayLock) 물리를 끈 채, 방향키 한 번 = 한 번 미끄러짐
///   - 어디서 멈출지는 IceSlidePuzzle.Slide가 계산, 여기서는 그 칸까지 부드럽게 옮기기만 함
///   - R: 처음 자리로 (페이드 → 시작 칸 → 모드 끝). 구석에 갇혀도 언제든 빠져나올 수 있음
///   - 미끄러져 시작 칸으로 돌아오거나, 시작 칸에서 ↓를 누르면 모드 끝 (얼음에서 내려가기)
///   - 올라선 직후에는 방향키를 한 번 뗄 때까지 입력을 무시 (걸어오며 누르던 키로 바로 미끄러지지 않게)
///   - 리본 나무 밑에 멈추면: "손이 닿았다" 대사 → 시작 칸으로 → puzzle.Complete() (보상 대사는 땅 위에서)
///     (조각을 받는 순간 자동 저장되므로, 얼음 위가 아닌 땅 위 위치가 저장되게 하려는 순서)
/// </summary>
public class IceSlideMover : MonoBehaviour, IInteractable
{
    [SerializeField] private IceSlidePuzzle puzzle;
    [Tooltip("얼음 위에 있는 동안만 보이는 조작 안내 (UICanvas/IceSlideHint)")]
    [SerializeField] private GameObject hint;
    [Tooltip("리본 나무 밑에 닿았을 때 대사")]
    [SerializeField] private DialogueData reachedDialogue;
    [SerializeField, Min(1f)] private float slideSpeed = 8f;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.8f);

    private PlayerController player;
    private PlayerInputReader input;
    private Rigidbody2D rb;

    private Vector2Int cell;
    private bool onIce;
    private bool busy;
    private bool waitRelease;

    public string Prompt => "얼음에 올라서기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Start()
    {
        if (hint != null) hint.SetActive(false);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (onIce) return;
        player = interactor.GetComponent<PlayerController>();
        input = interactor.GetComponent<PlayerInputReader>();
        rb = interactor.GetComponent<Rigidbody2D>();
        Enter();
    }

    private void Enter()
    {
        onIce = true;
        GameplayLock.Acquire(this);   // 일반 이동·상호작용 멈춤
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;         // 물리 끄기: 바위·연못 경계 충돌에 밀려나지 않게 위치를 직접 정함
        cell = puzzle.StartCell;
        player.transform.position = IceSlidePuzzle.CellToWorld(cell);
        player.Face(Vector2.up);
        waitRelease = true;
        if (hint != null) hint.SetActive(true);
    }

    private void Exit()
    {
        onIce = false;
        rb.simulated = true;
        rb.position = player.transform.position;
        GameplayLock.Release(this);
        if (hint != null) hint.SetActive(false);
    }

    private void Update()
    {
        if (!onIce || busy) return;
        if (DialogueRunner.Instance != null && DialogueRunner.Instance.IsRunning) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(ReturnToStart());
            return;
        }

        var dir = ToDirection(input.MoveInput);
        if (waitRelease)
        {
            if (dir == Vector2Int.zero) waitRelease = false;
            return;
        }
        if (dir != Vector2Int.zero) StartCoroutine(SlideRoutine(dir));
    }

    private IEnumerator SlideRoutine(Vector2Int dir)
    {
        busy = true;
        player.Face(new Vector2(dir.x, dir.y));

        var to = puzzle.Slide(cell, dir);
        if (to == cell)
        {
            // 한 칸도 못 감. 시작 칸에서 ↓면 얼음에서 내려가기, 그 밖에는 아무 일도 없음
            if (cell == puzzle.StartCell && dir == Vector2Int.down) Exit();
            busy = false;
            yield break;
        }

        Vector3 target = IceSlidePuzzle.CellToWorld(to);
        while (player.transform.position != target)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, target, slideSpeed * Time.deltaTime);
            yield return null;
        }
        cell = to;

        if (cell == puzzle.StartCell)
        {
            Exit(); // 미끄러져 시작 칸(땅)으로 돌아옴
        }
        else if (!puzzle.IsSolved && puzzle.IsGoal(cell))
        {
            yield return PlayDialogue(reachedDialogue);
            yield return ReturnToStart();
            puzzle.Complete();
        }
        busy = false;
    }

    /// <summary>페이드 → 시작 칸 → 모드 끝 → 페이드 아웃</summary>
    private IEnumerator ReturnToStart()
    {
        busy = true;
        yield return SceneLoader.FadeScreen(1f);

        Vector3 from = player.transform.position;
        cell = puzzle.StartCell;
        player.transform.position = IceSlidePuzzle.CellToWorld(cell);
        player.Face(Vector2.down);
        CinemachineCore.OnTargetObjectWarped(player.transform, player.transform.position - from);
        Exit();

        yield return SceneLoader.FadeScreen(0f);
        busy = false;
    }

    private static IEnumerator PlayDialogue(DialogueData data)
    {
        var runner = DialogueRunner.Instance;
        if (runner == null || data == null) yield break;
        bool done = false;
        if (!runner.StartDialogue(data, () => done = true)) yield break;
        while (!done) yield return null;
    }

    /// <summary>입력을 상하좌우 한 칸 방향으로 (가장 크게 누른 축 하나만)</summary>
    private static Vector2Int ToDirection(Vector2 v)
    {
        if (v.sqrMagnitude < 0.25f) return Vector2Int.zero;
        return Mathf.Abs(v.x) > Mathf.Abs(v.y)
            ? new Vector2Int(v.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, v.y > 0 ? 1 : -1);
    }

    // 얼음 위에 있는 채로 꺼지면 잠금·물리가 남지 않게
    private void OnDisable()
    {
        if (!onIce) return;
        onIce = false;
        busy = false;
        if (rb != null) rb.simulated = true;
        GameplayLock.Release(this);
        if (hint != null) hint.SetActive(false);
    }
}
