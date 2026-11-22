using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image에 스프라이트 몇 장을 차례로 보여 주는 간단한 애니메이션.
/// 캐릭터 애니메이션 클립은 SpriteRenderer용이라 UI Image에는 바로 못 쓰기 때문에,
/// 명찰 사진 칸처럼 UI에서 캐릭터를 움직일 때 씁니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.1f)] private float framesPerSecond = 3f;

    private Image image;
    private float timer;
    private int index;

    private void Awake()
    {
        image = GetComponent<Image>();
        Show(0);
    }

    /// <summary>재생할 그림들을 바꾸고 첫 장부터 다시 보여 줍니다.</summary>
    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        timer = 0f;
        Show(0);
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2) return;

        timer += Time.unscaledDeltaTime;
        float frameTime = 1f / framesPerSecond;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            Show((index + 1) % frames.Length);
        }
    }

    private void Show(int i)
    {
        if (image == null) image = GetComponent<Image>();
        if (frames == null || frames.Length == 0) return;
        index = i;
        image.sprite = frames[i];
    }
}
