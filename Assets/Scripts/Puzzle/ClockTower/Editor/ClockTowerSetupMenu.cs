using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Setup Clock Tower (4-3)  (Village 씬에서 실행)
///  - 시계탑 종탑(1-7 청사진 (90,14) 8×8): 종 4개(도·레·미·솔), 종 순서 퍼즐 + 멜로디 칩 보상
///  - 시계탑 1층(80,14): 종지기, 찢어진 악보(추가 힌트)
///  - 광장 벤치 (36,23): 콧노래 부르는 주민 (MAP N4)
///  - 청사진의 같은 자리 표시는 진짜 오브젝트와 겹치므로 끔
/// 실내 좌표 (lx, ly) → 월드 칸 (방 x + lx, 47 − (14 + ly))
/// </summary>
public static class ClockTowerSetupMenu
{
    private const string DialogueFolder = "Assets/ScriptableObjects/Dialogues/ClockTower";
    private const string CircleSprite = "Assets/Art/Blueprint/Textures/Block_Circle.png";
    private const string BoxSprite = "Assets/Art/Blueprint/Textures/Block_Box.png";
    private const string MaleIdle = "Assets/Art/Characters/Player/Player_Male_Idle.png";
    private const string MelodyPiece = "Assets/ScriptableObjects/CardPieces/Piece_3_MelodyChip.asset";

    private static Vector3 Belfry(int lx, int ly) => new(90 + lx + 0.5f, 33 - ly + 0.5f, 0f);
    private static Vector3 FloorCell(int lx, int ly) => new(80 + lx + 0.5f, 33 - ly + 0.5f, 0f);
    private static Vector3 FloorFeet(int lx, int ly) => new(80 + lx + 0.5f, 33 - ly + 0.1f, 0f);

    // 종: 음, 종탑 안 위치 (MAP 7.4)
    private static readonly (BellNote note, int lx, int ly)[] Bells =
    {
        (BellNote.Do, 2, 4), (BellNote.Re, 3, 2), (BellNote.Mi, 5, 2), (BellNote.Sol, 6, 4),
    };

    private static readonly Color BellGold = new(0.95f, 0.8f, 0.35f, 1f);

    [MenuItem("Merry Memory/Setup Clock Tower (4-3)")]
    private static void Setup()
    {
        var gameplay = GameObject.Find("Gameplay");
        if (gameplay == null)
        {
            EditorUtility.DisplayDialog("Setup Clock Tower", "Village 씬에서 실행하세요.", "확인");
            return;
        }
        if (gameplay.transform.Find("ClockTower") != null)
        {
            EditorUtility.DisplayDialog("Setup Clock Tower", "이미 Gameplay/ClockTower가 있습니다.", "확인");
            return;
        }

        // ── 대사 ──
        var ringerFirst = Dialogue("BellRinger_First",
            ("종지기", "어서 오게. 종탑까지 찾아온 손님은 오랜만이군."),
            ("종지기", "해마다 크리스마스이브엔 위층 종으로 캐럴을 쳐서, 그 소리를 멜로디 칩에 담아 두지."),
            ("종지기", "그런데 올해는 손이 굳었는지 순서가 영 떠오르질 않아."),
            ("종지기", "광장 벤치에 늘 콧노래를 흥얼거리는 친구가 있는데, 그 친구라면 기억할지도 모르겠구먼."),
            ("종지기", "종은 도, 레, 미, 솔 네 개야. 틀려도 괜찮으니 마음껏 쳐 보게."));
        var ringerRepeat = Dialogue("BellRinger_Repeat",
            ("종지기", "종은 위층에 있다네. 캐럴 순서는 광장의 콧노래 친구가 잘 알지."));
        var memo = Dialogue("SheetMemo",
            ("", "찢어진 악보다. 첫머리만 남아 있다."),
            ("", "\"미, 미, 미...\""));
        var hum = Dialogue("Hummer_Hint",
            ("콧노래 주민", "흠~ 흠흠~ 미 미 미~ 미 미 미~ 미 솔 도 레 미~"),
            ("콧노래 주민", "아, 들었어? 시계탑 종지기가 해마다 치는 캐럴이야."),
            ("콧노래 주민", "어릴 때부터 들어서 이젠 자면서도 흥얼거린다니까."));

        Undo.SetCurrentGroupName("Setup Clock Tower");
        int group = Undo.GetCurrentGroup();

        var root = new GameObject("ClockTower");
        Undo.RegisterCreatedObjectUndo(root, "Create ClockTower");
        root.transform.SetParent(gameplay.transform, false);
        root.transform.position = Belfry(4, 4);

        // ── 퍼즐 + 보상 ──
        var puzzle = root.AddComponent<BellSequencePuzzle>();
        var pso = new SerializedObject(puzzle);
        pso.FindProperty("puzzleId").stringValue = FlagKeys.BellSequence;
        pso.ApplyModifiedPropertiesWithoutUndo();

        var reward = root.AddComponent<CardPieceReward>();
        var rso = new SerializedObject(reward);
        rso.FindProperty("puzzle").objectReferenceValue = puzzle;
        rso.FindProperty("piece").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardPieceData>(MelodyPiece);
        rso.ApplyModifiedPropertiesWithoutUndo();

        // ── 종 4개 ──
        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSprite);
        foreach (var (note, lx, ly) in Bells)
        {
            var go = new GameObject($"Bell_{note}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = Belfry(lx, ly);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.8f, 0.8f);
            sr.color = BellGold;
            sr.sortingOrder = -4;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);
            Audio2D(go);

            var bell = go.AddComponent<Bell>();
            var so = new SerializedObject(bell);
            so.FindProperty("puzzle").objectReferenceValue = puzzle;
            so.FindProperty("note").enumValueIndex = (int)note;
            so.FindProperty("body").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 1층: 종지기 (4,3) ──
        var ringer = Person(root.transform, "BellRinger", FloorFeet(4, 3), new Color(0.75f, 0.8f, 1f));
        var npc = ringer.AddComponent<NpcTalk>();
        var nso = new SerializedObject(npc);
        nso.FindProperty("firstDialogue").objectReferenceValue = ringerFirst;
        nso.FindProperty("repeatDialogue").objectReferenceValue = ringerRepeat;
        nso.FindProperty("talkedFlag").stringValue = FlagKeys.BellRingerTalked;
        nso.ApplyModifiedPropertiesWithoutUndo();

        // ── 1층: 찢어진 악보 (2,5) ──
        var sheet = new GameObject("SheetMemo");
        sheet.transform.SetParent(root.transform, false);
        sheet.transform.position = FloorCell(2, 5);
        var msr = sheet.AddComponent<SpriteRenderer>();
        msr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);
        msr.drawMode = SpriteDrawMode.Sliced;
        msr.size = new Vector2(0.45f, 0.55f);
        msr.color = new Color(0.97f, 0.95f, 0.85f);
        msr.sortingOrder = -4;
        var mcol = sheet.AddComponent<BoxCollider2D>();
        mcol.isTrigger = true;
        mcol.size = new Vector2(0.8f, 0.8f);
        var insp = sheet.AddComponent<Inspectable>();
        var iso = new SerializedObject(insp);
        iso.FindProperty("dialogue").objectReferenceValue = memo;
        iso.FindProperty("promptOffset").vector2Value = new Vector2(0f, 0.7f);
        iso.ApplyModifiedPropertiesWithoutUndo();

        // ── 광장: 콧노래 주민 N4 (36,23) ──
        var hummer = Person(root.transform, "HummingVillager", new Vector3(36.5f, 47 - 23 + 0.1f, 0f), new Color(1f, 0.85f, 0.7f));
        Audio2D(hummer);
        var hn = hummer.AddComponent<HummingNpc>();
        var hso = new SerializedObject(hn);
        hso.FindProperty("puzzle").objectReferenceValue = puzzle;
        hso.FindProperty("hintDialogue").objectReferenceValue = hum;
        hso.ApplyModifiedPropertiesWithoutUndo();

        HideBlueprintMarkers();

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("시계탑(4-3)을 배치했습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    /// <summary>임시 사람: 플레이어 앞모습 스프라이트에 색만 입힘 (2단계에서 진짜 그림으로)</summary>
    private static GameObject Person(Transform parent, string name, Vector3 feet, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath(MaleIdle).OfType<Sprite>()
            .Where(s => s.name.Contains("_Front_")).OrderBy(s => s.name).FirstOrDefault();
        sr.color = tint;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = new Vector2(0.6f, 0.3f);
        col.offset = new Vector2(0f, 0.15f);
        return go;
    }

    private static void Audio2D(GameObject go)
    {
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 0f;
    }

    private static void HideBlueprintMarkers()
    {
        var bp = GameObject.Find("Village Blueprint");
        if (bp == null) return;

        string[] interiorEnds = { " 종지기", " 악보", " 도", " 레", " 미", " 솔", " 조각 3" };
        foreach (var t in bp.GetComponentsInChildren<Transform>(true))
        {
            var room = t.parent != null ? t.parent.name : "";
            bool inTower = room.Contains("시계탑 1층") || room.Contains("시계탑 종탑");
            if ((inTower && interiorEnds.Any(e => t.name.EndsWith(e))) || t.name.Contains("콧노래"))
            {
                Undo.RecordObject(t.gameObject, "Hide Blueprint");
                t.gameObject.SetActive(false);
            }
        }
    }

    private static DialogueData Dialogue(string fileName, params (string speaker, string text)[] lines)
    {
        Directory.CreateDirectory(DialogueFolder);
        string path = $"{DialogueFolder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<DialogueData>();
        foreach (var (speaker, text) in lines) data.lines.Add(new DialogueLine { speaker = speaker, text = text });
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }
}
