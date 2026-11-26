using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 이동 + 검은 화면 페이드. 어디서든 SceneLoader.Load(SceneNames.Village) 한 줄로 씁니다.
///
/// 페이드 화면은 처음 쓸 때 스스로 만들어지고(DontDestroyOnLoad), 씬이 바뀌어도 남아서
/// "어두워짐 → 씬 교체 → 밝아짐"이 끊기지 않습니다. 씬에 따로 놓을 필요가 없습니다.
/// 이동하는 동안은 GameplayLock으로 조작을 막고, 버튼도 눌리지 않습니다.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    private const float DefaultFadeDuration = 0.4f;

    private static SceneLoader instance;

    private CanvasGroup fade;
    private bool loading;

    /// <summary>지금 씬을 옮기는 중인지.</summary>
    public static bool IsLoading => instance != null && instance.loading;

    /// <summary>페이드 아웃 → 씬 로드 → 페이드 인. 이미 이동 중이면 무시합니다.</summary>
    public static void Load(string sceneName, float fadeDuration = DefaultFadeDuration)
    {
        var loader = GetOrCreate();
        if (loader.loading) return;
        loader.StartCoroutine(loader.LoadRoutine(sceneName, fadeDuration));
    }

    /// <summary>
    /// 씬은 그대로 두고 화면만 어둡게(1)/밝게(0) 합니다. 문 이동 같은 같은 씬 안의 순간 이동에 씁니다. (4-0)
    /// 코루틴 안에서 yield return SceneLoader.FadeScreen(1f); 처럼 기다리며 씁니다.
    /// </summary>
    public static IEnumerator FadeScreen(float target, float duration = DefaultFadeDuration)
    {
        var loader = GetOrCreate();
        loader.fade.blocksRaycasts = target > 0f;
        yield return loader.FadeTo(target, duration);
    }

    private IEnumerator LoadRoutine(string sceneName, float duration)
    {
        loading = true;
        GameplayLock.Acquire(this);
        fade.blocksRaycasts = true; // 페이드 중 버튼 클릭 막기

        yield return FadeTo(1f, duration);

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"씬 '{sceneName}'을(를) 불러올 수 없습니다. Build Profiles의 씬 목록에 있는지 확인하세요.");
        }
        else
        {
            while (!op.isDone) yield return null;
        }

        // 새 씬이 첫 프레임을 그릴 때까지 한 프레임 기다린 뒤 밝게
        yield return null;
        GameplayLock.Release(this);
        yield return FadeTo(0f, duration);

        fade.blocksRaycasts = false;
        loading = false;
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = fade.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            fade.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        fade.alpha = target;
    }

    // ── 페이드 화면 만들기 ──

    private static SceneLoader GetOrCreate()
    {
        if (instance != null) return instance;

        var go = new GameObject("[SceneLoader]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneLoader>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 모든 UI보다 위
        go.AddComponent<GraphicRaycaster>();
        instance.fade = go.AddComponent<CanvasGroup>();
        instance.fade.alpha = 0f;
        instance.fade.blocksRaycasts = false;

        var image = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(go.transform, false);
        var rt = (RectTransform)image.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        image.GetComponent<Image>().color = Color.black;

        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        GameplayLock.Release(this);
    }

    // 도메인 리로드를 끈 상태에서 Play를 다시 시작할 때 지난 값을 비웁니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}
