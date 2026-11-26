using UnityEditor;
using UnityEngine;

/// <summary>
/// 저장 테스트용 메뉴 (로드맵 3-5)
///   Merry Memory → Open Save Folder   : save.json이 있는 폴더 열기
///   Merry Memory → Delete Save Data   : 진행 세이브 삭제
///   Merry Memory → Reset Ending Cleared : "엔딩 클리어" 기록 지우기
/// </summary>
public static class SaveDebugMenu
{
    [MenuItem("Merry Memory/Open Save Folder")]
    private static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath + "/");

    [MenuItem("Merry Memory/Delete Save Data")]
    private static void DeleteSave()
    {
        SaveSystem.Delete();
        Debug.Log($"세이브를 지웠습니다: {SaveSystem.FilePath}");
    }

    [MenuItem("Merry Memory/Reset Ending Cleared")]
    private static void ResetEnding()
    {
        PlayerPrefs.DeleteKey("EndingCleared");
        PlayerPrefs.Save();
        Debug.Log("엔딩 클리어 기록을 지웠습니다.");
    }
}
