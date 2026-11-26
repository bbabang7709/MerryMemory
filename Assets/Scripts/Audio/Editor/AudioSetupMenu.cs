using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Setup Audio (5-2)  (여러 번 실행해도 안전)
///  - Assets/Resources/AudioLibrary.asset 만들기 (칸은 비워 둠 → 즉석 소리)
///  - Title / NameBadge / Village 씬에 SceneAudio (배경 음악·바람 크기)
///  - Title / NameBadge 화면 Canvas에 UIClickSounds (버튼 "틱")
///  - Village의 Player에 FootstepPlayer (발소리)
/// 씬을 차례로 열어 고치고 저장한 뒤 Village로 돌아옵니다. 지금 씬에 저장 안 한 변경이 있으면 멈춥니다.
/// </summary>
public static class AudioSetupMenu
{
    private const string LibraryPath = "Assets/Resources/AudioLibrary.asset";

    [MenuItem("Merry Memory/Setup Audio (5-2)")]
    private static void Setup()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorUtility.DisplayDialog("Setup Audio", "지금 씬에 저장하지 않은 변경이 있습니다. 먼저 저장하세요.", "확인");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath) == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AudioLibrary>(), LibraryPath);
            AssetDatabase.SaveAssets();
        }

        var log = new System.Text.StringBuilder("소리(5-2) 설정:\n");
        Scene($"Assets/Scenes/{SceneNames.Title}.unity", Bgm.Title, 0.5f, 0.5f, true, false, log);
        Scene($"Assets/Scenes/{SceneNames.NameBadge}.unity", Bgm.Title, 0.3f, 0.3f, true, false, log);
        Scene($"Assets/Scenes/{SceneNames.Village}.unity", Bgm.Village, 1f, 0.2f, false, true, log);
        Debug.Log(log.ToString());
    }

    private static void Scene(string path, Bgm bgm, float outdoor, float indoor, bool clickSounds, bool footsteps, System.Text.StringBuilder log)
    {
        var scene = EditorSceneManager.OpenScene(path);

        // 주의: 에디터의 GetComponent는 없을 때 "가짜 null"을 돌려줘서 ??가 안 먹으므로 == null로 확인
        var go = GameObject.Find("SceneAudio");
        if (go == null) go = new GameObject("SceneAudio");
        var audio = go.GetComponent<SceneAudio>();
        if (audio == null) audio = go.AddComponent<SceneAudio>();
        var so = new SerializedObject(audio);
        so.FindProperty("bgm").enumValueIndex = (int)bgm;
        so.FindProperty("outdoorAmbience").floatValue = outdoor;
        so.FindProperty("indoorAmbience").floatValue = indoor;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (clickSounds)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.GetComponent<UIClickSounds>() == null) canvas.gameObject.AddComponent<UIClickSounds>();
        }

        if (footsteps)
        {
            var player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null && player.GetComponent<FootstepPlayer>() == null) player.gameObject.AddComponent<FootstepPlayer>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"  {Path.GetFileNameWithoutExtension(path)}: BGM {bgm}, 바람 {outdoor}/{indoor}" +
                       (clickSounds ? ", 버튼 소리" : "") + (footsteps ? ", 발소리" : ""));
    }
}
