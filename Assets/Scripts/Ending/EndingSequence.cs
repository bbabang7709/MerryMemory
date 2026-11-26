using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엔딩 연출 (로드맵 2-5 최소 버전, GDD 3.4). 코루틴 하나로 순서대로 진행합니다.
///   ① 이동 잠금 → ② 잠시 멈췄다가 화면(아래)을 봄 → ③ 화면이 어두워지며 정면 클로즈업
///   → ④ 엔딩 대사 → ⑤ 화면 가득 "MERRY CHRISTMAS!!" → ⑥ 몇 초 뒤 타이틀로
/// 5단계에서 Timeline으로 다듬을 예정입니다.
///
/// 화면 요소(Dim, Closeup, Merry)는 UICanvas/EndingView 아래에 투명(CanvasGroup alpha 0)으로
/// 숨어 있다가 FadeTo로 서서히 나타납니다.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("주인공")]
    [SerializeField] private PlayerController player;

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

    [Header("대사")]
    [SerializeField] private DialogueData endingDialogue;

    [Header("시간 (초)")]
    [SerializeField, Min(0f)] private float pauseBeforeTurn = 0.6f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.8f;
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
        // ① 이동 잠금
        GameplayLock.Acquire(this);

        // ② 잠시 멈췄다가 화면 쪽(아래)을 봄 (GDD: "잠시 멈췄다가, 천천히 화면 정면을 본다")
        yield return new WaitForSeconds(pauseBeforeTurn);
        player.Face(Vector2.down);

        // ③ 어둡게 + 정면 클로즈업
        closeupImage.sprite = GameSession.Profile.Gender == Gender.Female ? femaleCloseup : maleCloseup;
        StartCoroutine(FadeTo(dim, 1f, fadeDuration));                       // 기다리지 않음 → 아래와 동시에
        yield return StartCoroutine(FadeTo(closeupGroup, 1f, fadeDuration)); // 끝날 때까지 기다림

        // ④ 엔딩 대사: 대화가 끝나면 콜백이 dialogueDone을 켜고, 그때까지 기다립니다.
        //    시작 자체를 못 하면(Runner 없음, 대사 비어 있음, 다른 대화 중) 기다리지 않고 넘어갑니다.
        bool dialogueDone = false;
        var runner = DialogueRunner.Instance;
        bool started = runner != null && runner.StartDialogue(endingDialogue, () => dialogueDone = true);
        if (started)
            yield return new WaitUntil(() => dialogueDone);
        else
            Debug.LogWarning("엔딩 대사를 시작하지 못했습니다. (DialogueRunner 없음, 대사 비어 있음, 또는 다른 대화 중)", this);

        // ⑤ MERRY CHRISTMAS!! : 빨리 나타나고(fadeDuration) → 그대로 보여 주기(merryHold)
        yield return StartCoroutine(FadeTo(merry, 1f, fadeDuration));
        yield return new WaitForSeconds(merryHold);

        // ⑥ 정리하고 타이틀로 (타이틀에서 GameSession.Clear로 이번 플레이 정보가 지워짐)
        //    엔딩 클리어를 기록(새 게임을 해도 남음)하고, 다 깬 판의 진행 세이브는 지움 → [이어하기] 꺼짐 (3-5)
        SaveSystem.MarkEndingCleared();
        SaveSystem.Delete();
        GameplayLock.Release(this);
        SceneLoader.Load(SceneNames.Title);
    }

    /// <summary>CanvasGroup의 투명도를 target까지 duration초 동안 바꿉니다.</summary>
    private IEnumerator FadeTo(CanvasGroup group, float target, float duration)
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
