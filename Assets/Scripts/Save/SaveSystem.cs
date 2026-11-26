using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 저장과 불러오기. (로드맵 3-5)
///   - 진행 세이브: SaveData를 JSON으로 Application.persistentDataPath/save.json 에 씀
///   - 엔딩 클리어: 새 게임을 해도 남아야 하므로 따로 PlayerPrefs에 씀
///
/// 저장하는 때: 명찰 작성 완료(새 게임), 조각을 얻을 때마다(자동), 엔딩 후에는 삭제.
/// 장부들(GameSession, CardProgress, GameFlags)은 저장을 모릅니다. SaveSystem이 그들을 읽고 씁니다.
///   저장: 장부 → (사본) → SaveData → JSON → 파일
///   불러오기: 파일 → JSON → SaveData → 장부 (이벤트 없이 Restore)
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const string EndingClearedKey = "EndingCleared";

    /// <summary>저장 파일 전체 경로</summary>
    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>이어할 세이브가 있는지</summary>
    public static bool HasSave => File.Exists(FilePath);

    /// <summary>마지막으로 불러온 세이브의 씬 이름 (이어하기에서 이 씬으로 감)</summary>
    public static string LoadedSceneName { get; private set; }

    // 이어하기로 불러온 플레이어 위치. Village의 Player(RestoreSavedPosition)가 시작할 때 한 번 꺼내 씁니다.
    private static Vector2? pendingPlayerPosition;

    // ───────────────────────── 저장 ─────────────────────────

    /// <summary>
    /// 지금 상태를 파일에 씁니다.
    /// sceneName을 주면 그 씬으로 저장 (명찰 화면에서 "Village로 시작"을 저장할 때), 없으면 지금 씬.
    /// 쓰기에 실패해도(디스크 꽉 참, 권한 등) 경고만 남기고 게임은 계속됩니다.
    /// </summary>
    public static void Save(string sceneName = null)
    {
        SaveData data = Capture(sceneName);
        string json = JsonUtility.ToJson(data, true); // true = 사람이 읽기 좋게 줄바꿈
        try
        {
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Save Failed: {e}");
        }
    }

    /// <summary>지금 상태를 SaveData로 모읍니다. 목록은 장부 그대로가 아니라 사본으로 담습니다.</summary>
    private static SaveData Capture(string sceneName)
    {
        var data = new SaveData
        {
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sceneName = string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName,
        };

        data.playerName = GameSession.Profile.Name;
        data.gender = GameSession.Profile.Gender;
        data.collectedPieces = new List<string>(CardProgress.AllIds);
        data.flags = new List<string>(GameFlags.AllKeys);

        // 플레이어가 씬에 있으면 위치도 (명찰 화면처럼 없으면 위치 없음)
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            data.hasPlayerPosition = true;
            data.playerPosition = player.transform.position;
        }
        return data;
    }

    // ───────────────────────── 불러오기 ─────────────────────────

    /// <summary>
    /// 파일을 읽어 장부들에 되돌립니다. 성공하면 true. (씬 이동은 부른 쪽이 LoadedSceneName으로)
    /// 파일이 없거나 깨졌으면(읽기·해석·적용 중 예외) 경고를 남기고 false.
    /// </summary>
    public static bool Load()
    {
        if (!HasSave) return false;

        try
        {
            string json = File.ReadAllText(FilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return false;
            Apply(data);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Load Failed: {e}");
            return false;
        }
    }

    /// <summary>불러온 SaveData를 장부들에 되돌립니다. (저장의 반대 방향)</summary>
    private static void Apply(SaveData data)
    {
        if (data == null) return;

        GameSession.Begin(data.playerName, data.gender);  // 이름이 이상하면 예외 → Load의 catch가 받음
        CardProgress.RestoreIds(data.collectedPieces);    // 이벤트 없이 되돌리기
        GameFlags.RestoreKeys(data.flags);
        LoadedSceneName = data.sceneName;
        pendingPlayerPosition = data.hasPlayerPosition ? data.playerPosition : null; // Vector2? : null이면 위치 없음
    }

    /// <summary>저장된 위치가 있으면 꺼내고(한 번만) true. RestoreSavedPosition이 씁니다.</summary>
    public static bool TryTakePendingPosition(out Vector2 position)
    {
        position = pendingPlayerPosition ?? Vector2.zero;
        bool has = pendingPlayerPosition.HasValue;
        pendingPlayerPosition = null;
        return has;
    }

    /// <summary>세이브 파일을 지웁니다.</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"세이브를 지우지 못했습니다: {e.Message}");
        }
    }

    // ───────────────────────── 엔딩 클리어 (따로 저장) ─────────────────────────

    /// <summary>엔딩을 본 적이 있는지. 새 게임을 해도 남습니다.</summary>
    public static bool EndingCleared => PlayerPrefs.GetInt(EndingClearedKey, 0) == 1;

    /// <summary>엔딩을 봤다고 기록합니다. Save()까지 불러서 갑자기 꺼져도 남게 합니다.</summary>
    public static void MarkEndingCleared()
    {
        PlayerPrefs.SetInt(EndingClearedKey, 1);
        PlayerPrefs.Save();
    }

    // ───────────────────────── 자동 저장 ─────────────────────────

    // 조각을 얻을 때마다 자동 저장. CardProgress는 저장을 모르고, 여기서 이벤트를 듣습니다.
    // (CardProgress가 SubsystemRegistration에서 이벤트를 비운 "뒤"에 구독해야 하므로 BeforeSceneLoad)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        pendingPlayerPosition = null;
        LoadedSceneName = null;
        CardProgress.PieceCollected -= OnPieceCollected;
        CardProgress.PieceCollected += OnPieceCollected;
    }

    private static void OnPieceCollected(CardPieceData piece) => Save();
}
