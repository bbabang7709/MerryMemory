using System.Collections;
using UnityEngine;

/// <summary>
/// 광장 벤치에서 콧노래를 부르는 주민 (로드맵 4-3, MAP N4). 시계탑 종 순서 힌트.
///   말을 걸면 대사를 보여 주면서, 동시에 정답 멜로디를 콧노래(ToneSynth.Hum)로 흥얼거립니다.
///   멜로디는 BellSequencePuzzle.Melody를 그대로 읽어서 → 정답을 바꾸면 콧노래도 같이 바뀜.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class HummingNpc : MonoBehaviour, IInteractable
{
    [SerializeField] private BellSequencePuzzle puzzle;
    [SerializeField] private DialogueData hintDialogue;

    [Tooltip("음마다 몇 박인지 (멜로디와 같은 길이). 징글벨: 1 1 2 / 1 1 2 / 1 1 1.5 0.5 4")]
    [SerializeField] private float[] beats = { 1, 1, 2, 1, 1, 2, 1, 1, 1.5f, 0.5f, 4 };

    [Tooltip("1박의 길이(초)")]
    [SerializeField, Min(0.05f)] private float beatSeconds = 0.32f;

    [SerializeField] private Vector2 promptOffset = new(0f, 1.6f);

    private AudioSource source;
    private bool humming;

    public string Prompt => "말 걸기";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake() => source = GetComponent<AudioSource>();

    public void Interact(PlayerInteractor interactor)
    {
        DialogueRunner.Instance?.StartDialogue(hintDialogue);

        // 이미 흥얼거리는 중이면 새로 시작하지 않음 (연달아 말을 걸어도 콧노래가 겹치지 않게)
        if (humming) return;

        StartCoroutine(Hum());
    }

    /// <summary>멜로디를 한 음씩 흥얼거립니다. 음 길이 = 박 수 × beatSeconds, 소리는 그 90%만 내서 음 사이를 살짝 띄움.</summary>
    private IEnumerator Hum()
    {
        humming = true;
        var melody = puzzle.Melody;

        for (int i = 0; i < melody.Count; i++)
        {
            // 멜로디만 늘리고 beats를 깜빡해도 범위를 벗어나지 않게: 모자라면 1박
            float length = (i < beats.Length ? beats[i] : 1f) * beatSeconds;
            source.PlayOneShot(ToneSynth.Hum(melody[i], length * 0.9f));
            yield return new WaitForSeconds(length);
        }

        humming = false;
    }

    // 흥얼거리는 도중 오브젝트가 꺼지면 코루틴도 멈추므로 humming을 되돌려 둠
    private void OnDisable() => humming = false;
}
