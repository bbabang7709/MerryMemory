using System.Collections;
using UnityEngine;

/// <summary>
/// 종탑의 종 하나 (로드맵 4-3). [F]로 치면 자기 음을 울리고 BellSequencePuzzle.Ring(음)에 알립니다.
///   - Correct / Completed: 종소리 + 흔들림
///   - Wrong: 종소리 + 잠깐 뒤 "덜컥" + 붉게 깜빡 (처음부터 다시라는 신호)
///   - 퍼즐이 풀린 뒤에는 소리만 (자유 연주)
/// 소리는 ToneSynth가 즉석에서 만듭니다. (진짜 SFX는 5단계)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Bell : MonoBehaviour, IInteractable
{
    [SerializeField] private BellSequencePuzzle puzzle;
    [SerializeField] private BellNote note;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.8f);
    [SerializeField, Min(0f)] private float clunkDelay = 0.25f;

    private AudioSource source;
    private Color baseColor;
    private Coroutine swing;

    public BellNote Note => note;
    public string Prompt => $"치기 ({BellNotes.Label(note)})";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (body != null) baseColor = body.color;
    }

    public void Interact(PlayerInteractor interactor)
    {
        source.PlayOneShot(ToneSynth.Bell(note));

        var result = puzzle.IsSolved ? BellSequencePuzzle.RingResult.Completed : puzzle.Ring(note);

        if (swing != null) StopCoroutine(swing);
        swing = StartCoroutine(Swing(result == BellSequencePuzzle.RingResult.Wrong));
    }

    /// <summary>좌우로 흔들림. 틀렸으면 "덜컥" 소리와 붉은 깜빡임을 더함</summary>
    private IEnumerator Swing(bool wrong)
    {
        const float duration = 0.6f;
        bool clunked = false;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = 1f - t / duration;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 28f) * 14f * k);

            if (wrong && !clunked && t >= clunkDelay)
            {
                clunked = true;
                source.PlayOneShot(ToneSynth.Clunk());
            }
            if (body != null)
                body.color = wrong && t >= clunkDelay ? Color.Lerp(baseColor, new Color(1f, 0.35f, 0.3f), k) : baseColor;

            yield return null;
        }
        transform.localRotation = Quaternion.identity;
        if (body != null) body.color = baseColor;
        swing = null;
    }
}
