using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 엔딩 연출 (로드맵 2-5 → 5-4, GDD 3.4). 코루틴으로 장면을 순서대로 진행합니다.
///   ① 모임   : 화면이 어두워졌다 밝아지면 트리 앞에 마을 사람들이 모여 있음 → 우체국장 한마디
///   ② 완성   : [F]를 누를 때마다 카드에 조각이 하나씩 붙음 (쉬운 마무리 퍼즐, 의식에 가까움)
///   ③ 주소   : "To. 화면 너머의 너에게"가 한 글자씩
///   ④ 깨달음 : 잠시 멈췄다가 화면 정면을 봄 → 클로즈업 + "날 여기까지 데려와 준 거, 너였구나."
///   ⑤ 자정   : 종이 세 번 울리고 카드가 펼쳐짐 → 캐럴(엔딩 편곡) + 마을 사람들의 한마디가 하나씩
///   ⑥ 배달   : "배달 왔습니다! …그럼, 다 같이…" → 화면 가득 MERRY CHRISTMAS!!
///   ⑦ 크레딧 : 카드의 "From." 칸처럼 보낸 사람들 (EndingCredits, 5-5)
///   ⑧ 마무리 : 엔딩 클리어 기록, 진행 세이브 삭제 → 타이틀 ("배달 완료" 도장)
/// 대사를 기다리고 키 입력을 기다리는 구간이 많아서 Timeline 대신 코루틴으로 둡니다.
/// 화면 요소(Dim, Card, Closeup, Merry)는 UICanvas/EndingView 아래에 투명하게 숨어 있습니다.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("주인공")]
    [SerializeField] private PlayerController player;
    [Tooltip("모임 장면에서 주인공이 설 자리 (트리 앞)")]
    [SerializeField] private Transform gatherPoint;

    [Header("마을 사람들")]
    [Tooltip("트리 앞에 모인 마을 사람들 (평소엔 꺼 둠)")]
    [SerializeField] private GameObject crowd;
    [Tooltip("엔딩 동안 숨길 것 (조각 현황 UI 등)")]
    [SerializeField] private GameObject[] hideDuringEnding;

    [Header("카드")]
    [SerializeField] private EndingCardView card;
    [Tooltip("조각 1~5 (번호 순서). 카드에 붙는 순서이자 한마디가 떠오르는 순서")]
    [SerializeField] private CardPieceData[] pieces;
    [SerializeField] private string address = "To. 화면 너머의 너에게";

    [Header("화면")]
    [Tooltip("화면을 어둡게 덮는 검은 판")]
    [SerializeField] private CanvasGroup dim;
    [Tooltip("주인공 정면 클로즈업 (임시: 큰 스프라이트)")]
    [SerializeField] private CanvasGroup closeupGroup;
    [SerializeField] private Image closeupImage;
    [SerializeField] private Sprite femaleCloseup;
    [SerializeField] private Sprite maleCloseup;
    [Tooltip("화면 가득 MERRY CHRISTMAS!!")]
    [SerializeField] private CanvasGroup merry;
    [Tooltip("From. 칸 형식의 크레딧 (5-5)")]
    [SerializeField] private EndingCredits credits;

    [Header("대사")]
    [SerializeField] private DialogueData gatherDialogue;
    [FormerlySerializedAs("endingDialogue")]
    [SerializeField] private DialogueData realizeDialogue;
    [SerializeField] private DialogueData deliverDialogue;

    [Header("시간 (초)")]
    [SerializeField, Min(0f)] private float pauseBeforeTurn = 0.8f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.8f;
    [SerializeField, Min(0f)] private float messageInterval = 2.4f;
    [SerializeField, Min(0f)] private float merryHold = 4f;

    private bool playing;

    /// <summary>엔딩을 시작합니다. 이미 진행 중이면 무시합니다.</summary>
    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        GameplayLock.Acquire(this);
        AudioManager.PlayBgm(Bgm.None, 1.5f); // 모이는 동안은 바람 소리만

        yield return Gather();
        yield return Assemble();
        yield return Realize();
        yield return Midnight();
        yield return Deliver();
        if (credits != null) yield return credits.Play(pieces);

        // ⑧ 엔딩 클리어를 기록(새 게임을 해도 남음)하고, 다 깬 판의 진행 세이브는 지움 → [이어하기] 꺼짐 (3-5)
        SaveSystem.MarkEndingCleared();
        SaveSystem.Delete();
        GameplayLock.Release(this);
        SceneLoader.Load(SceneNames.Title);
    }

    // ① 트리 앞에 모임
    private IEnumerator Gather()
    {
        yield return SceneLoader.FadeScreen(1f);

        foreach (var go in hideDuringEnding) if (go != null) go.SetActive(false);
        if (crowd != null) crowd.SetActive(true);

        if (gatherPoint != null)
        {
            Vector3 from = player.transform.position;
            player.transform.position = gatherPoint.position;
            if (player.TryGetComponent(out Rigidbody2D rb)) rb.position = gatherPoint.position;
            CameraAreaSwitcher.Instance?.SetArea(CameraAreaSwitcher.Instance.DefaultArea);
            CinemachineCore.OnTargetObjectWarped(player.transform, gatherPoint.position - from);
        }
        player.Face(Vector2.up);

        yield return SceneLoader.FadeScreen(0f);
        yield return PlayDialogue(gatherDialogue);
    }

    // ② [F]를 누를 때마다 조각 하나씩
    private IEnumerator Assemble()
    {
        StartCoroutine(FadeTo(dim, 0.55f, fadeDuration));
        yield return card.Show(fadeDuration);

        for (int i = 0; i < pieces.Length; i++)
        {
            card.SetHint($"[F] 조각 맞추기  ({i}/{pieces.Length})");
            yield return WaitForConfirm();
            card.Place(i, pieces[i]);
            AudioManager.PlaySfx(Sfx.Stamp);
        }
        card.SetHint("");
        AudioManager.PlaySfx(Sfx.PieceGet);
        yield return new WaitForSeconds(0.8f);
    }

    // ③ 주소 → ④ 화면 정면
    private IEnumerator Realize()
    {
        yield return card.RevealAddress(address, 8f);
        yield return new WaitForSeconds(1.6f);

        yield return card.Hide(fadeDuration);
        yield return new WaitForSeconds(pauseBeforeTurn); // "잠시 멈췄다가"
        player.Face(Vector2.down);                        // "천천히 화면 정면을 본다"

        closeupImage.sprite = GameSession.Profile.Gender == Gender.Female ? femaleCloseup : maleCloseup;
        StartCoroutine(FadeTo(dim, 1f, fadeDuration));
        yield return FadeTo(closeupGroup, 1f, fadeDuration);
        yield return PlayDialogue(realizeDialogue);
        yield return FadeTo(closeupGroup, 0f, fadeDuration);
    }

    // ⑤ 자정의 종 → 카드가 펼쳐지고 한마디가 하나씩
    private IEnumerator Midnight()
    {
        for (int i = 0; i < 3; i++)
        {
            AudioManager.PlaySfx(Sfx.MidnightBell);
            yield return new WaitForSeconds(1.3f);
        }

        AudioManager.PlayBgm(Bgm.Ending, 1f); // 3번 조각에서 완성한 캐럴
        StartCoroutine(FadeTo(dim, 0.85f, fadeDuration));
        yield return card.Show(fadeDuration);
        yield return card.Open(1.2f);

        for (int i = 0; i < pieces.Length; i++)
        {
            yield return card.ShowMessage(i, pieces[i], 0.8f);
            yield return new WaitForSeconds(messageInterval);
        }
        yield return new WaitForSeconds(1f);
    }

    // ⑥ 카드를 화면 쪽으로 내밀며 → MERRY CHRISTMAS!!
    private IEnumerator Deliver()
    {
        yield return card.Hide(fadeDuration);
        yield return FadeTo(closeupGroup, 1f, fadeDuration);
        yield return PlayDialogue(deliverDialogue);

        AudioManager.PlaySfx(Sfx.PieceGet);
        yield return FadeTo(merry, 1f, fadeDuration * 0.5f); // 빨리 나타나고
        yield return new WaitForSeconds(merryHold);          // 그대로 보여 주기
    }

    // ───────────────────────── 도우미 ─────────────────────────

    /// <summary>F / Space / Enter / 마우스 클릭 중 하나를 새로 누를 때까지 기다립니다.</summary>
    private static IEnumerator WaitForConfirm()
    {
        yield return null; // 앞 대사를 닫은 키가 같은 프레임에 또 잡히지 않게 한 프레임 쉼
        while (true)
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            if ((k != null && (k.fKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame))
                yield break;
            yield return null;
        }
    }

    /// <summary>대사를 재생하고 끝날 때까지 기다립니다. 시작을 못 하면(대사 없음 등) 바로 넘어갑니다.</summary>
    private IEnumerator PlayDialogue(DialogueData data)
    {
        var runner = DialogueRunner.Instance;
        bool done = false;
        if (runner == null || data == null || !runner.StartDialogue(data, () => done = true))
        {
            Debug.LogWarning("엔딩 대사를 시작하지 못했습니다. (DialogueRunner 없음, 대사 비어 있음, 또는 다른 대화 중)", this);
            yield break;
        }
        yield return new WaitUntil(() => done);
    }

    /// <summary>CanvasGroup의 투명도를 target까지 duration초 동안 바꿉니다.</summary>
    private static IEnumerator FadeTo(CanvasGroup group, float target, float duration)
    {
        if (group == null) yield break;
        float start = group.alpha;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
    }

    // 엔딩 도중 오브젝트가 꺼져도 잠금이 남지 않게
    private void OnDisable() => GameplayLock.Release(this);
}
