using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Finish Stage 4 (4-6)  (Village 씬에서 실행, 여러 번 실행해도 안전)
///  - 광장 트리(EndingTrigger): 필요한 조각을 5개 전부로
///  - 퍼즐 주인 4명(공방 주인·종지기·리본 아이·빵집 사장): 퍼즐을 푼 뒤 대사(solvedFlag / solvedDialogue) 연결
///  - 우체국장: NpcTalk → GuideNpcTalk 로 바꾸고(기존 값 유지) 조각별 힌트 연결
///    → 2~5번을 어떤 순서로 풀어도 대사가 어색하지 않고, 막히면 우체국장이 힌트를 줌
/// </summary>
public static class Stage4FinishMenu
{
    private const string PieceFolder = "Assets/ScriptableObjects/CardPieces";

    [MenuItem("Merry Memory/Finish Stage 4 (4-6)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        if (gameplay == null)
        {
            EditorUtility.DisplayDialog("Finish Stage 4", "Village 씬에서 실행하세요.", "확인");
            return;
        }

        var pieces = AssetDatabase.FindAssets("t:CardPieceData", new[] { PieceFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<CardPieceData>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(p => p.number).ToArray();
        CardPieceData Piece(int n) => pieces.First(p => p.number == n);

        Undo.SetCurrentGroupName("Finish Stage 4");
        int group = Undo.GetCurrentGroup();
        var log = new System.Text.StringBuilder("4-6 정리:\n");

        // ── 광장 트리: 조각 5개 전부 ──
        var ending = Object.FindFirstObjectByType<EndingTrigger>(FindObjectsInactive.Include);
        if (ending != null)
        {
            var so = new SerializedObject(ending);
            var arr = so.FindProperty("requiredPieces");
            arr.arraySize = pieces.Length;
            for (int i = 0; i < pieces.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            so.ApplyModifiedProperties();
            log.AppendLine($"  EndingTrigger 필요 조각 {pieces.Length}개");
        }

        // ── 퍼즐을 푼 뒤 대사 ──
        Solved(gameplay, "ToyWorkshop/WorkshopOwner", FlagKeys.PopupAssembly,
            "Assets/ScriptableObjects/Dialogues/ToyWorkshop", "Owner_Solved",
            ("공방 주인", "팝업 트리, 멋지게 솟아오르지? 덕분에 카드가 한층 화려해졌어."),
            ("공방 주인", "다른 조각도 찾고 있다며? 힘내라고!"), log);
        Solved(gameplay, "ClockTower/BellRinger", FlagKeys.BellSequence,
            "Assets/ScriptableObjects/Dialogues/ClockTower", "BellRinger_Solved",
            ("종지기", "자네 덕에 올해도 종소리를 멜로디 칩에 담았네."),
            ("종지기", "마을 사람들도 다 들었을 걸세. 고맙네."), log);
        Solved(gameplay, "SnowHill/Kid_RibbonOwner", FlagKeys.IceSlide,
            "Assets/ScriptableObjects/Dialogues/SnowHill", "Kid_Owner_Solved",
            ("눈 언덕 아이", "리본 찾아 줘서 고마워! 카드에 예쁘게 달아 줘!"),
            ("눈 언덕 아이", "얼음 미끄럼, 나보다 잘 타던데?"), log);
        Solved(gameplay, "Bakery/Baker", FlagKeys.AddressRestore,
            "Assets/ScriptableObjects/Dialogues/Bakery", "Baker_Solved",
            ("빵집 사장", "주소가 조금은 읽히지? '너머의' 누구라니, 나도 궁금하네."),
            ("빵집 사장", "카드가 다 모이면 받는 사람도 알게 될 거야."), log);

        // ── 우체국장: GuideNpcTalk로 ──
        var master = gameplay.transform.Find("PostMaster");
        var talk = master != null ? master.GetComponent<NpcTalk>() : null;
        if (talk != null)
        {
            var so = new SerializedObject(talk);
            if (!(talk is GuideNpcTalk))
            {
                // 스크립트만 바꿔 끼움: GuideNpcTalk는 NpcTalk의 자식이라 기존 값(대사·플래그·안내 위치)이 그대로 남음
                var guideScript = AssetDatabase.FindAssets("GuideNpcTalk t:MonoScript")
                    .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
                    .First(s => s.GetClass() == typeof(GuideNpcTalk));
                so.FindProperty("m_Script").objectReferenceValue = guideScript;
                so.ApplyModifiedProperties();
                talk = master.GetComponent<NpcTalk>();
                so = new SerializedObject(talk);
            }
            so.FindProperty("guideAfterFlag").stringValue = FlagKeys.PuzzleSolved(FlagKeys.MailSort);
            var hints = so.FindProperty("hints");
            var lines = new (int n, string line)[]
            {
                (2, "공방 거리 장난감 공방 주인이 망가진 장치 때문에 울상이던데."),
                (3, "시계탑 종소리가 올해는 아직 안 들리네. 종지기를 찾아가 보렴."),
                (4, "눈 언덕 아이들이 얼음 연못 앞에서 발을 동동 구르더구나."),
                (5, "빵집 사장이 카드 봉투 때문에 곤란해하던데."),
            };
            hints.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var h = hints.GetArrayElementAtIndex(i);
                h.FindPropertyRelative("piece").objectReferenceValue = Piece(lines[i].n);
                h.FindPropertyRelative("line").stringValue = lines[i].line;
            }
            so.ApplyModifiedProperties();
            log.AppendLine("  PostMaster → GuideNpcTalk (힌트 4줄)");
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(gameplay.scene);
        Debug.Log(log + "씬을 저장하세요 (Ctrl+S).");
    }

    private static void Solved(GameObject gameplay, string path, string puzzleId, string folder, string file,
        (string, string) a, (string, string) b, System.Text.StringBuilder log)
    {
        var t = gameplay.transform.Find(path);
        var npc = t != null ? t.GetComponent<NpcTalk>() : null;
        if (npc == null)
        {
            log.AppendLine($"  (없음) {path}");
            return;
        }
        var so = new SerializedObject(npc);
        so.FindProperty("solvedFlag").stringValue = FlagKeys.PuzzleSolved(puzzleId);
        so.FindProperty("solvedDialogue").objectReferenceValue = Dialogue(folder, file, a, b);
        so.ApplyModifiedProperties();
        log.AppendLine($"  {path}: 푼 뒤 대사 {file}");
    }

    private static DialogueData Dialogue(string folder, string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(folder);
        string path = $"{folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }
}
