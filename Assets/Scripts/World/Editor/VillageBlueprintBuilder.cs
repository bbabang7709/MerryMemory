using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 메뉴 Merry Memory → Build Village Blueprint
/// Docs/MAP.md(및 마을 지도 아티팩트)의 배치를 청사진 스타일 임시 그림으로 씬에 만듭니다.
///  - 바닥·지형은 타일맵(Ground / Decoration / Collision), 그림은 코드로 생성한 청사진 타일
///  - 건물·진행 오브젝트·인물·장식·실내는 이름표가 달린 윤곽 블록 + 충돌
/// 좌표는 MAP.md 그대로(왼쪽 위 원점, y 남쪽)이고, Unity로는 셀 = (x, 47 − y)입니다.
/// 실제 그림이 준비되면 블록을 하나씩 실제 오브젝트로 바꿔 나가면 됩니다.
/// </summary>
public static class VillageBlueprintBuilder
{
    private const int MapH = 48;
    private const int PPU = 32;
    private const string RootName = "Village Blueprint";
    private const string ArtFolder = "Assets/Art/Blueprint";
    private const string TexFolder = ArtFolder + "/Textures";
    private const string TileFolder = ArtFolder + "/Tiles";
    private const string PalettePath = ArtFolder + "/Blueprint Palette.prefab";
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";

    // 그리기 순서: 바닥 < 장식 < 막힌 지형 < 블록 < 문 < 이름표 < 플레이어(0)
    private const int OrderGround = -10, OrderDeco = -9, OrderCollision = -8, OrderBlock = -5, OrderDoor = -4, OrderLabel = -3;

    // ───────────────────────── 색 ─────────────────────────
    private static readonly Color CBuilding = new(0.92f, 0.96f, 1f);
    private static readonly Color CDoor = new(1f, 0.83f, 0.29f);
    private static readonly Color CProgress = new(1f, 0.76f, 0.30f);
    private static readonly Color CPiece = new(1f, 0.45f, 0.45f);
    private static readonly Color CFragment = new(0.35f, 0.9f, 0.82f);
    private static readonly Color CNpc = new(0.49f, 1f, 0.70f);
    private static readonly Color CHint = new(0.79f, 0.63f, 1f);
    private static readonly Color CDecor = new(0.66f, 0.85f, 1f);
    private static readonly Color CEnding = new(1f, 0.56f, 0.69f, 0.45f);
    private static readonly Color CZone = new(0.85f, 0.92f, 1f, 0.30f);

    // MAP.md / 아티팩트 기준 충돌하는 종류
    private static readonly HashSet<string> Collide = new()
    {
        "building", "house", "tower", "gazebo", "pine", "lamp", "bench", "snowman", "sign", "board", "crate", "statue",
        "table", "telescope", "woodpile", "mailbox", "stall", "fountain", "fort", "fence", "bigtree", "ribbontree", "rock",
        "sacks", "bicycle", "gift", "sled", "npc", "hint", "chalk", "counter", "desk", "cubby", "stove", "oven", "fire",
        "shelf", "case", "gears", "horse", "bell",
    };

    private enum Shape { Box, Circle, Tree, None }

    private sealed class Obj
    {
        public string Id, Kind, Name, Note, Mark;
        public int X, Y, W = 1, H = 1;
        public (int x, int y, int w)? Door;
        public Obj(string id, string kind, string name, int x, int y, int w = 1, int h = 1, string note = "")
        { Id = id; Kind = kind; Name = name; X = x; Y = y; W = w; H = h; Note = note; }
    }

    // ───────────────────────── 메뉴 ─────────────────────────

    [MenuItem("Merry Memory/Build Village Blueprint")]
    private static void Build()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null &&
            !EditorUtility.DisplayDialog("Build Village Blueprint",
                "씬에 청사진 맵이 이미 있습니다. 지우고 다시 만들까요?\n(직접 고친 내용도 사라집니다. Ctrl+Z로 되돌릴 수 있습니다.)",
                "다시 만들기", "취소"))
            return;

        var tiles = CreateTiles();
        var sprites = CreateObjectSprites();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        Undo.SetCurrentGroupName("Build Village Blueprint");
        int group = Undo.GetCurrentGroup();
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        var ctx = new Ctx { Sprites = sprites, Font = font };

        // ── 타일맵 ──
        var grid = Child(root.transform, "Grid").gameObject;
        grid.AddComponent<Grid>();
        ctx.Ground = CreateTilemap(grid.transform, "Ground", OrderGround);
        ctx.Deco = CreateTilemap(grid.transform, "Decoration", OrderDeco);
        ctx.Collision = CreateTilemap(grid.transform, "Collision", OrderCollision);
        SetupCollision(ctx.Collision.gameObject);

        PaintTerrain(ctx, tiles);
        BuildExterior(ctx, root.transform);
        BuildInteriors(ctx, root.transform, tiles);

        ctx.Collision.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        ctx.Collision.GetComponent<CompositeCollider2D>().GenerateGeometry();

        CreatePaletteIfMissing(tiles);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("청사진 마을을 만들었습니다. 씬을 저장하세요 (Ctrl+S).");
    }

    private sealed class Ctx
    {
        public Tilemap Ground, Deco, Collision;
        public Dictionary<Shape, Sprite> Sprites;
        public TMP_FontAsset Font;
    }

    // ───────────────────────── 바깥 지형 (MAP.md 5장) ─────────────────────────

    private static void PaintTerrain(Ctx c, Dictionary<string, Tile> t)
    {
        Fill(c.Ground, t["Snow"], 0, 0, 64, 48);              // 나머지 빈칸은 모두 눈 덮인 땅

        Fill(c.Ground, t["Plateau"], 2, 2, 19, 11);           // 눈 언덕 고지대
        Fill(c.Ground, t["Ice"], 5, 4, 10, 7);                // 얼어붙은 연못 (지나갈 수 있음)
        Fill(c.Ground, t["Stairs"], 15, 13, 3, 1);            // 눈 언덕 계단
        Fill(c.Ground, t["Road"], 15, 14, 3, 9);              // 눈 언덕 오르막길
        Fill(c.Ground, t["Road"], 27, 13, 10, 3);             // 시계탑 앞마당
        Fill(c.Ground, t["Road"], 30, 16, 4, 1);              // 북쪽 길
        Fill(c.Ground, t["Road"], 37, 13, 19, 2);             // 북동 골목
        Fill(c.Ground, t["Road"], 6, 23, 18, 3);              // 공방 거리
        Fill(c.Ground, t["Road"], 40, 23, 19, 3);             // 빵집 거리
        Fill(c.Ground, t["Road"], 19, 26, 2, 12);             // 서쪽 샛길
        Fill(c.Ground, t["Plaza"], 24, 17, 16, 14);           // 광장
        Fill(c.Ground, t["Road"], 30, 31, 4, 15);             // 남쪽 큰길
        Fill(c.Ground, t["Road"], 5, 38, 16, 2);              // 남서 골목
        Fill(c.Ground, t["Road"], 21, 38, 9, 3);              // 우체국 앞마당
        Fill(c.Ground, t["Road"], 34, 38, 17, 2);             // 남동 골목
        Fill(c.Ground, t["Bridge"], 46, 40, 3, 3);            // 나무 다리
        Fill(c.Ground, t["Road"], 44, 43, 9, 3);              // 개울 건너 전망대

        // 경계·충돌 지형
        foreach (var (x, y, w, h) in new[]
                 {
                     (0, 0, 64, 2), (0, 2, 2, 44), (62, 2, 2, 44), (0, 46, 29, 2), (35, 46, 29, 2), // 마을 둘레 숲
                     (2, 40, 4, 6), (6, 42, 15, 4), (57, 2, 5, 7), (53, 43, 9, 3),                // 구석 숲
                 })
            Fill(c.Collision, t["Forest"], x, y, w, h);
        Fill(c.Collision, t["Cliff"], 2, 13, 13, 1);          // 눈 언덕 절벽 (계단 x 15–17만 뚫림)
        Fill(c.Collision, t["Cliff"], 18, 13, 4, 1);
        Fill(c.Collision, t["Cliff"], 21, 2, 1, 11);
        Fill(c.Collision, t["RockWall"], 22, 2, 35, 2);       // 북쪽 바위 절벽
        Fill(c.Collision, t["Stream"], 35, 41, 11, 2);        // 얼어붙은 개울 — 다리(x 46–48)만 비움
        Fill(c.Collision, t["Stream"], 49, 41, 13, 2);
        Fill(c.Collision, t["Drift"], 29, 46, 6, 2);          // 큰 눈 더미

        // 눈 더미 5개 (Decoration, 지나갈 수 있음)
        foreach (var (x, y) in new[] { (22, 16), (40, 16), (18, 30), (44, 30), (37, 44) })
            Fill(c.Deco, t["SnowPile"], x, y, 1, 1);
    }

    // ───────────────────────── 바깥 오브젝트 (MAP.md 4·6장) ─────────────────────────

    private static void BuildExterior(Ctx c, Transform root)
    {
        // 구역 이름
        var zones = Child(root, "Zones");
        foreach (var (name, x, y) in new[]
                 {
                     ("눈 언덕", 6.5f, 11.8f), ("시계탑", 39.5f, 5.3f), ("북동 주택가", 49f, 4.9f), ("공방 거리", 10f, 15.8f),
                     ("광장", 32f, 16.4f), ("빵집 거리", 51f, 15.8f), ("남서 주택가", 11f, 32.2f), ("남쪽 큰길", 26.5f, 42.1f),
                     ("남동 개울가", 52f, 39.8f),
                 })
            Label(c, zones, name, new Vector2(x, MapH - y), new Vector2(8f, 1.5f), 9f, CZone);

        // 건물
        var buildings = Child(root, "Buildings");
        foreach (var o in new[]
                 {
                     new Obj("B1", "building", "우체국", 21, 31, 8, 7, "게임이 시작되는 건물. 튜토리얼을 끝내기 전에는 문이 잠겨 있습니다.") { Door = (24, 37, 2) },
                     new Obj("B2", "building", "장난감 공방", 6, 17, 8, 6, "조각 ② 팝업 트리. 실내에서 장치 재조립 퍼즐.") { Door = (9, 22, 2) },
                     new Obj("B3", "tower", "시계탑", 29, 6, 6, 7, "조각 ③ 멜로디 칩. 1층과 종탑 두 개의 실내. 탑 꼭대기 그림은 위쪽 바위 절벽에 겹쳐 그림.") { Door = (31, 12, 2) },
                     new Obj("B4", "building", "빵집", 50, 17, 8, 6, "조각 ⑤ 봉투. 실내에서 번진 주소 복원 퍼즐(글자 조각 3개 필요).") { Door = (53, 22, 2) },
                     new Obj("H1", "house", "주택", 6, 33, 5, 5, "들어갈 수 없는 집. 문을 조사하면 짧은 대사.") { Door = (8, 37, 1) },
                     new Obj("H2", "house", "주택", 13, 33, 5, 5, "들어갈 수 없는 집.") { Door = (15, 37, 1) },
                     new Obj("H3", "house", "주택", 36, 33, 5, 5, "들어갈 수 없는 집.") { Door = (38, 37, 1) },
                     new Obj("H4", "house", "주택", 43, 33, 5, 5, "들어갈 수 없는 집.") { Door = (45, 37, 1) },
                     new Obj("H5", "house", "주택", 42, 7, 5, 5, "들어갈 수 없는 집.") { Door = (44, 11, 1) },
                     new Obj("H6", "house", "주택", 50, 7, 5, 5, "들어갈 수 없는 집.") { Door = (52, 11, 1) },
                     new Obj("H7", "house", "주택", 41, 17, 5, 5, "들어갈 수 없는 집.") { Door = (43, 21, 1) },
                     new Obj("G1", "gazebo", "전구 정자", 56, 10, 4, 4, "지붕 둘레에 전구를 단 정자. 조명 포인트."),
                 })
            Place(c, buildings, o, 0, 0, CBuilding, Shape.Box, 6f);

        // 진행 오브젝트
        var progress = Child(root, "Progress");
        Place(c, progress, new Obj("P0", "bigtree", "광장 크리스마스트리", 30, 21, 4, 5, "엔딩 트리거. 조각 5개 전에는 \"조각이 아직 모자라.\" 대사."), 0, 0, CProgress, Shape.Tree, 4f);
        Place(c, progress, new Obj("P4", "ribbontree", "리본 나무", 9, 2, 2, 2, "조각 ④ 리본. 얼음 위로 미끄러져 (9, 4)에 닿아야 조사 가능."), 0, 0, CPiece, Shape.Tree, 3f);
        Place(c, progress, new Obj("PZ", "area", "얼어붙은 연못", 5, 4, 10, 7, "얼음 격자. 바위나 가장자리에 닿을 때까지 미끄러짐. 퍼즐 전용 이동 모드."), 0, 0, CProgress, Shape.None, 4f);
        Place(c, progress, new Obj("PS", "start", "시작", 10, 11, 1, 1, "미끄럼 퍼즐 시작 지점. 다시 하기 시 여기로 복귀."), 0, 0, CProgress, Shape.Circle, 2.5f);
        Place(c, progress, new Obj("PR", "resetsign", "다시 하기", 12, 11, 1, 1, "조사하면 퍼즐 초기화(필수)."), 0, 0, CProgress, Shape.Box, 2.5f);
        foreach (var (id, x, y, n) in new[] { ("PK1", 10, 6, "풀이용"), ("PK2", 8, 7, "풀이용"), ("PK3", 12, 5, "방해용"), ("PK4", 6, 9, "방해용"), ("PK5", 13, 8, "방해용") })
            Place(c, progress, new Obj(id, "rock", "바위", x, y, 1, 1, $"얼음 위 바위 ({n}). 예시 배치."), 0, 0, CDecor, Shape.Circle, 2.5f);
        Place(c, progress, new Obj("F1", "frag", "글자 1", 36, 17, 1, 1, "광장 게시판 전단의 귀퉁이."), 0, 0, CFragment, Shape.Box, 2.5f);
        Place(c, progress, new Obj("F2", "frag", "글자 2", 48, 41, 1, 1, "나무 다리 난간에 걸린 종이."), 0, 0, CFragment, Shape.Box, 2.5f);
        Place(c, progress, new Obj("F3", "frag", "글자 3", 35, 44, 1, 1, "입구 아치 옆 눈 속에 떨어진 간판 글자."), 0, 0, CFragment, Shape.Box, 2.5f);
        // 문 이동 지점: 그림은 건물의 문 표시가 대신하고, 여기는 위치만 잡아 둡니다(4단계에서 이동 기능을 붙임).
        foreach (var (id, x, y, n) in new[] { ("W1", 24, 37, "우체국 실내 (튜토리얼 완료 전엔 잠김)"), ("W2", 9, 22, "장난감 공방 실내"), ("W3", 31, 12, "시계탑 1층"), ("W4", 53, 22, "빵집 실내") })
            Place(c, progress, new Obj(id, "warp", "문 → " + n, x, y, 2, 1, "문 이동 지점."), 0, 0, CDoor, Shape.None, 0f);

        // 인물
        var npcs = Child(root, "NPCs");
        Place(c, npcs, new Obj("N1", "npc", "아이", 17, 6, 1, 1, "눈 언덕 아이. 조각 ④의 주인. 리본이 나무에 걸려 울상."), 0, 0, CNpc, Shape.Circle, 2.5f);
        Place(c, npcs, new Obj("N2", "npc", "아이", 18, 8, 1, 1, "눈 언덕 아이. 미끄럼 요령 힌트."), 0, 0, CNpc, Shape.Circle, 2.5f);
        Place(c, npcs, new Obj("N3", "npc", "아이", 16, 9, 1, 1, "눈 언덕 아이. 오래 막히면 힌트."), 0, 0, CNpc, Shape.Circle, 2.5f);
        Place(c, npcs, new Obj("N4", "hint", "콧노래", 36, 23, 1, 1, "(제안) 콧노래 부르는 주민. 시계탑 종 순서 힌트."), 0, 0, CHint, Shape.Circle, 2.5f);

        // 엔딩 자리 (충돌 없음)
        var ending = Child(root, "Ending Marks");
        foreach (var (id, x, y, w, n) in new[]
                 {
                     ("E0", 31, 28, 2, "주인공"), ("E1", 28, 26, 1, "우체국장"), ("E2", 35, 26, 1, "공방 주인"), ("E3", 28, 23, 1, "종지기"),
                     ("E4", 35, 23, 1, "빵집 사장"), ("E5", 29, 28, 1, "아이"), ("E6", 34, 28, 1, "아이"), ("E7", 35, 29, 1, "아이"),
                 })
            Place(c, ending, new Obj(id, "ghost", n, x, y, w, 1, "엔딩에서 트리 앞에 모일 자리."), 0, 0, CEnding, Shape.Box, 2f);

        // 장식
        var props = Child(root, "Props");
        int i = 0;
        foreach (var (x, y) in new[] { (25, 18), (38, 18), (25, 29), (38, 29), (27, 13), (36, 13), (40, 12), (48, 12), (19, 22), (11, 26), (46, 22), (56, 26), (29, 33), (34, 33), (29, 41), (34, 41), (12, 40), (41, 40) })
            Place(c, props, new Obj("DL" + ++i, "lamp", "가로등", x, y, 1, 1, "주황 Point Light 2D 자리."), 0, 0, CDoor, Shape.Circle, 0f);
        i = 0;
        foreach (var (x, y, w, h) in new[] { (27, 18, 2, 1), (26, 22, 1, 2), (37, 22, 1, 2), (28, 15, 2, 1), (12, 27, 2, 1), (13, 11, 1, 1), (46, 44, 2, 1) })
            Place(c, props, new Obj("DB" + ++i, "bench", "벤치", x, y, w, h, "조사 시 혼잣말."), 0, 0, CDecor, Shape.Box, 0f);
        i = 0;
        foreach (var (x, y) in new[] { (20, 11), (3, 11), (8, 28), (9, 28), (10, 28), (49, 12), (18, 40), (50, 29), (50, 35) })
            Place(c, props, new Obj("DS" + ++i, "snowman", "눈사람", x, y, 1, 1, "(8~10, 28)은 눈사람 가족."), 0, 0, CDecor, Shape.Circle, 0f);
        i = 0;
        foreach (var (x, y) in new[] { (3, 3), (16, 3), (3, 7), (23, 6), (39, 6), (26, 8), (37, 8), (26, 11), (37, 11), (3, 16), (3, 23), (3, 27), (3, 30), (6, 30), (13, 30), (41, 30), (59, 27), (60, 30), (22, 43), (26, 44), (52, 36) })
            Place(c, props, new Obj("DP" + ++i, "pine", "소나무", x, y, 1, 1, "숲과 별개로 흩어 둔 나무."), 0, 0, CDecor, Shape.Tree, 0f);
        i = 0;
        foreach (var (x, y, n) in new[] { (35, 15, "시계탑 앞 갈림길"), (18, 21, "눈 언덕 갈림길"), (34, 31, "광장 남쪽 출구") })
            Place(c, props, new Obj("DG" + ++i, "sign", "표지판", x, y, 1, 1, "방향 표지판: " + n), 0, 0, CDecor, Shape.Box, 2f);
        Place(c, props, new Obj("DN1", "board", "게시판", 35, 18, 2, 1, "광장 게시판. 글자 조각 ①이 붙어 있음."), 0, 0, CDecor, Shape.Box, 2.5f);
        i = 0;
        foreach (var (x, y) in new[] { (29, 25), (34, 25), (30, 26), (33, 26) })
            Place(c, props, new Obj("DX" + ++i, "gift", "선물", x, y, 1, 1, "트리 밑 선물 상자."), 0, 0, CPiece, Shape.Box, 0f);
        i = 0;
        foreach (var (x, y, n) in new[] { (25, 27, "코코아 노점"), (36, 27, "오너먼트 노점"), (41, 26, "마켓 노점"), (45, 26, "마켓 노점") })
            Place(c, props, new Obj("DM" + ++i, "stall", n, x, y, 3, 2, "문 닫은 노점."), 0, 0, CDecor, Shape.Box, 3f);
        i = 0;
        foreach (var (x, y, w, h) in new[] { (30, 17, 4, 1), (24, 23, 1, 3), (39, 23, 1, 3), (30, 30, 4, 1) })
            Place(c, props, new Obj("DA" + ++i, "arch", "가랜드 아치", x, y, w, h, "광장 입구. 기둥만 충돌, 가운데는 지나갈 수 있음."), 0, 0, CDecor, Shape.Box, 2f);
        Place(c, props, new Obj("DA5", "gate", "마을 입구 아치", 29, 44, 6, 2, "\"어서 오세요\" 간판. 양쪽 기둥만 충돌."), 0, 0, CDecor, Shape.Box, 3f);
        Place(c, props, new Obj("DC1", "crate", "상자", 4, 20, 1, 1, "공방 부품 상자."), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DC2", "crate", "상자", 4, 21, 1, 1, "공방 부품 상자."), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DT1", "statue", "병정", 14, 21, 1, 1, "장난감 병정 조각상. 공방 문 옆."), 0, 0, CDecor, Shape.Box, 2f);
        Place(c, props, new Obj("DD1", "sled", "썰매", 14, 19, 1, 1, ""), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DD2", "sled", "썰매", 17, 4, 1, 1, ""), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DD3", "sled", "썰매", 20, 7, 1, 1, ""), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DE1", "slope", "썰매 슬로프", 18, 3, 3, 4, "지나갈 수 있음."), 0, 0, CDecor, Shape.Box, 3f);
        Place(c, props, new Obj("DF1", "fort", "눈 요새", 17, 9, 3, 2, ""), 0, 0, CDecor, Shape.Box, 3f);
        Place(c, props, new Obj("DF2", "fence", "언덕 난간", 2, 12, 13, 1, "절벽 가장자리."), 0, 0, CDecor, Shape.Box, 2f);
        Place(c, props, new Obj("DF3", "fence", "언덕 난간", 18, 12, 3, 1, "절벽 가장자리."), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DW1", "woodpile", "장작", 11, 36, 2, 1, ""), 0, 0, CDecor, Shape.Box, 2f);
        Place(c, props, new Obj("DW2", "woodpile", "장작", 48, 9, 1, 1, ""), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DK1", "table", "카페 테이블", 47, 19, 1, 1, "빵집 바깥."), 0, 0, CDecor, Shape.Circle, 0f);
        Place(c, props, new Obj("DK2", "table", "카페 테이블", 47, 21, 1, 1, "빵집 바깥."), 0, 0, CDecor, Shape.Circle, 0f);
        Place(c, props, new Obj("DK3", "chalk", "입간판", 49, 22, 1, 1, "메뉴 입간판."), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DK4", "sacks", "밀가루", 58, 20, 1, 1, "밀가루 포대."), 0, 0, CDecor, Shape.Circle, 0f);
        Place(c, props, new Obj("DK5", "bicycle", "자전거", 58, 22, 1, 1, "빵집 배달 자전거."), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DQ1", "fountain", "얼어붙은 분수", 52, 27, 3, 3, ""), 0, 0, CDecor, Shape.Circle, 3f);
        Place(c, props, new Obj("DO1", "mailbox", "우체통", 28, 38, 1, 1, "빨간 우체통. 타이틀 화면 모티프."), 0, 0, CPiece, Shape.Box, 2f);
        Place(c, props, new Obj("DO2", "bicycle", "우편 자전거", 22, 38, 1, 1, ""), 0, 0, CDecor, Shape.Box, 0f);
        Place(c, props, new Obj("DO3", "sacks", "우편 자루", 21, 38, 1, 1, ""), 0, 0, CDecor, Shape.Circle, 0f);
        Place(c, props, new Obj("DV1", "telescope", "망원경", 50, 44, 1, 1, "조사 시 \"아주 먼 곳까지 보일 것 같아.\""), 0, 0, CDecor, Shape.Box, 2f);
    }

    // ───────────────────────── 실내 (MAP.md 7장) ─────────────────────────

    private static void BuildInteriors(Ctx c, Transform root, Dictionary<string, Tile> t)
    {
        var parent = Child(root, "Interiors");

        Room(c, parent, t, "우체국 실내", 80, 0, 14, 10, new[]
        {
            I("door", "출구", 6, 9, 2, 1, "바깥 우체국 문 (24, 37). 튜토리얼 전엔 우체국장이 막아섬."),
            I("start", "시작 위치", 6, 6, 1, 1, "명찰 작성이 끝나면 여기서 시작."),
            I("npc", "우체국장", 5, 2, 1, 1, "의뢰인. 조각 ① 지급."),
            I("piece", "조각 1", 6, 2, 1, 1, "조각 ① 우표. 편지 6통 분류 후 받음."),
            I("counter", "카운터", 2, 3, 8, 1, ""),
            I("desk", "도장 책상", 10, 2, 2, 1, "명찰 도장 연출에도 사용."),
            I("cubby", "분류함", 1, 4, 1, 4, "위에서부터 시계탑 길 · 공방 길 · 빵집 길 · 눈 언덕."),
            I("letter", "편지", 4, 6, 1, 1, "흩어진 편지. 조사하면 들고 다님."), I("letter", "편지", 7, 5, 1, 1, ""), I("letter", "편지", 9, 7, 1, 1, ""),
            I("letter", "편지", 11, 6, 1, 1, ""), I("letter", "편지", 3, 8, 1, 1, ""), I("letter", "편지", 12, 8, 1, 1, ""),
            I("table", "카드 탁자", 10, 4, 3, 1, "프롤로그에서 카드가 놓였던 탁자."),
            I("window", "창문", 12, 0, 2, 2, "프롤로그 눈바람이 들이친 창문."),
            I("stove", "난로", 12, 7, 1, 1, ""), I("xtree", "작은 트리", 13, 3, 1, 1, ""), I("sacks", "우편 자루", 8, 8, 1, 1, ""),
        });
        Room(c, parent, t, "장난감 공방 실내", 96, 0, 12, 9, new[]
        {
            I("door", "출구", 5, 8, 2, 1, "바깥 공방 문 (9, 22)."),
            I("npc", "공방 주인", 8, 4, 1, 1, ""),
            I("bench", "작업대", 5, 3, 3, 1, "망가진 팝업 장치. 재조립 퍼즐 UI. 부품 3개를 모은 뒤 풀 수 있음."),
            I("piece", "조각 2", 6, 4, 1, 1, "조각 ② 팝업 트리."),
            I("part", "톱니", 1, 3, 1, 1, "부품: 톱니바퀴. 낮은 선반 위."), I("part", "태엽", 10, 3, 1, 1, "부품: 태엽 스프링. 서랍 속."),
            I("part", "트리 판", 9, 6, 1, 1, "부품: 종이 트리 판. 장난감 기차에 실려 있음."),
            I("shelf", "선반", 1, 1, 4, 1, ""), I("shelf", "선반", 8, 1, 4, 1, ""), I("horse", "흔들목마", 2, 6, 1, 1, ""),
            I("train", "기차 선로", 6, 5, 4, 3, "지나갈 수 있음."), I("fire", "벽난로", 11, 5, 1, 2, ""),
        });
        Room(c, parent, t, "시계탑 1층", 80, 14, 8, 8, new[]
        {
            I("door", "출구", 3, 7, 2, 1, "바깥 시계탑 문 (31, 12)."),
            I("stairs", "계단 (종탑)", 6, 1, 1, 2, "종탑으로 이동."),
            I("npc", "종지기", 4, 3, 1, 1, ""),
            I("gears", "톱니 장치", 1, 1, 2, 3, ""),
            I("memo", "악보", 2, 5, 1, 1, "(선택) 첫 두 음만 남은 악보. 추가 힌트."),
        });
        Room(c, parent, t, "시계탑 종탑", 90, 14, 8, 8, new[]
        {
            I("stairs", "계단 (1층)", 1, 6, 1, 1, "1층으로 이동."),
            I("bell", "도", 2, 4, 1, 1, "종: 도"), I("bell", "레", 3, 2, 1, 1, "종: 레"), I("bell", "미", 5, 2, 1, 1, "종: 미"), I("bell", "솔", 6, 4, 1, 1, "종: 솔"),
            I("piece", "조각 3", 4, 5, 1, 1, "조각 ③ 멜로디 칩. 「징글벨」 후렴을 치면 나타남."),
            I("window", "창", 3, 0, 2, 1, "창밖으로 광장 트리."),
        });
        Room(c, parent, t, "빵집 실내", 110, 0, 12, 9, new[]
        {
            I("door", "출구", 5, 8, 2, 1, "바깥 빵집 문 (53, 22)."),
            I("npc", "빵집 사장", 4, 2, 1, 1, ""),
            I("bench", "작업대", 7, 6, 3, 1, "번진 봉투. 주소 복원 퍼즐. 글자 조각 ①②③ 사용."),
            I("piece", "조각 5", 8, 5, 1, 1, "조각 ⑤ 봉투."),
            I("counter", "계산대", 2, 3, 5, 1, ""), I("case", "진열 케이스", 7, 3, 2, 1, ""), I("oven", "오븐", 9, 1, 2, 2, "오븐 효과음 위치."),
            I("shelf", "빵 선반", 1, 1, 3, 1, ""), I("table", "창가 테이블", 1, 6, 2, 1, ""),
            I("sacks", "밀가루", 11, 5, 1, 1, ""), I("sacks", "밀가루", 11, 6, 1, 1, ""),
        });
    }

    private static Obj I(string kind, string name, int x, int y, int w, int h, string note) => new(kind, kind, name, x, y, w, h, note);

    /// <summary>방 하나: 바닥, 맨 위 2줄 벽, 둘레 벽, 안의 오브젝트.</summary>
    private static void Room(Ctx c, Transform parent, Dictionary<string, Tile> t, string name, int ox, int oy, int w, int h, Obj[] items)
    {
        Fill(c.Ground, t["Floor"], ox, oy, w, h);
        Fill(c.Collision, t["Wall"], ox, oy, w, 2);              // 맨 위 2줄은 벽
        Fill(c.Collision, t["Wall"], ox - 1, oy, 1, h + 1);      // 왼쪽
        Fill(c.Collision, t["Wall"], ox + w, oy, 1, h + 1);      // 오른쪽
        Fill(c.Collision, t["Wall"], ox - 1, oy + h, w + 2, 1);  // 아래

        var room = Child(parent, name);
        Label(c, room, name, new Vector2(ox + w / 2f, MapH - oy + 0.6f), new Vector2(w, 1f), 5f, CBuilding);

        int n = 0;
        foreach (var o in items)
        {
            o.Id = $"{name}/{++n:00}";
            Color color = o.Kind switch
            {
                "npc" => CNpc,
                "piece" => CPiece,
                "door" or "stairs" => CDoor,
                "start" or "letter" or "part" or "memo" or "bell" or "cubby" or "bench" => CProgress,
                _ => CDecor,
            };
            Shape shape = o.Kind switch
            {
                "npc" or "start" or "stove" or "sacks" or "bell" => Shape.Circle,
                "xtree" => Shape.Tree,
                _ => Shape.Box,
            };
            Place(c, room, o, ox, oy, color, shape, 2.5f);
        }
    }

    // ───────────────────────── 오브젝트 놓기 ─────────────────────────

    /// <summary>MAP.md 좌표(ox, oy는 방 원점)에 블록 하나를 놓습니다. labelSize 0이면 이름표 없음.</summary>
    private static void Place(Ctx c, Transform parent, Obj o, int ox, int oy, Color color, Shape shape, float labelSize)
    {
        float left = ox + o.X;
        float top = MapH - (oy + o.Y);
        var center = new Vector2(left + o.W / 2f, top - o.H / 2f);
        var size = new Vector2(o.W, o.H);

        var go = Child(parent, $"{o.Id} {o.Name}").gameObject;
        go.transform.position = center;
        var info = go.AddComponent<BlueprintInfo>();
        info.id = o.Id;
        info.note = o.Note;

        if (shape != Shape.None)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = c.Sprites[shape];
            sr.color = color;
            sr.sortingOrder = OrderBlock;
            if (shape == Shape.Box) { sr.drawMode = SpriteDrawMode.Sliced; sr.size = size; }
            else
            {
                float s = o.Kind is "npc" or "hint" or "lamp" ? 0.8f : 1f; // 사람·가로등은 칸보다 조금 작게
                go.transform.localScale = new Vector3(o.W * s, o.H * s, 1f);
            }
        }
        else if (o.Kind == "area")
        {
            // 영역 표시: 채우지 않고 윤곽만
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = c.Sprites[Shape.Box];
            sr.color = new Color(color.r, color.g, color.b, 0.35f);
            sr.sortingOrder = OrderBlock;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
        }

        // 충돌
        if (o.Kind is "arch" or "gate")
        {
            // 기둥만 막힘: 가로 아치는 양 끝 칸, 세로 아치는 위아래 칸
            bool horizontal = o.W > o.H;
            for (int k = 0; k < (horizontal ? o.H : 1); k++)
            {
                if (horizontal)
                {
                    AddBox(go, new Vector2(left + 0.5f, top - k - 0.5f) - center, Vector2.one);
                    AddBox(go, new Vector2(left + o.W - 0.5f, top - k - 0.5f) - center, Vector2.one);
                }
                else
                {
                    AddBox(go, new Vector2(left + 0.5f, top - 0.5f) - center, Vector2.one);
                    AddBox(go, new Vector2(left + 0.5f, top - o.H + 0.5f) - center, Vector2.one);
                }
            }
        }
        else if (Collide.Contains(o.Kind))
        {
            var box = go.AddComponent<BoxCollider2D>();
            if (shape != Shape.Box && shape != Shape.None) box.size = Vector2.one; // 스케일된 오브젝트는 1×1이 곧 w×h
            else box.size = size;
        }

        // 문 표시 (건물 아래쪽 줄)
        if (o.Door.HasValue)
        {
            var (dx, dy, dw) = o.Door.Value;
            var door = Child(go.transform, "Door");
            door.position = new Vector2(dx + dw / 2f, MapH - dy - 0.5f);
            var sr = door.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = c.Sprites[Shape.Box];
            sr.color = CDoor;
            sr.sortingOrder = OrderDoor;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(dw, 1f);
        }

        if (labelSize > 0f)
        {
            // 건물은 지붕 쪽(위)에, 나머지는 가운데에
            var pos = o.Kind is "building" or "house" or "tower" ? new Vector2(center.x, top - Mathf.Min(1.5f, o.H / 2f)) : center;
            Label(c, go.transform, o.Name, pos, new Vector2(Mathf.Max(o.W, 1.6f), Mathf.Max(o.H, 0.8f)), labelSize, Color.white);
        }
    }

    private static void AddBox(GameObject go, Vector2 offset, Vector2 size)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.offset = offset;
        box.size = size;
    }

    private static void Label(Ctx c, Transform parent, string text, Vector2 worldPos, Vector2 size, float maxSize, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one;
        // 부모가 크기 조절된 원(사람 등)이어도 글자 비율이 깨지지 않게
        var ls = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f);

        var tmp = go.AddComponent<TextMeshPro>();
        if (c.Font != null) tmp.font = c.Font;
        tmp.text = text;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = maxSize;
        tmp.rectTransform.sizeDelta = size;
        tmp.sortingOrder = OrderLabel;
    }

    private static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // ───────────────────────── 타일맵 ─────────────────────────

    /// <summary>MAP.md 좌표의 w × h 영역을 칠합니다. 셀 = (x, 47 − y)</summary>
    private static void Fill(Tilemap map, TileBase tile, int x, int y, int w, int h)
    {
        var block = new TileBase[w * h];
        for (int k = 0; k < block.Length; k++) block[k] = tile;
        map.SetTilesBlock(new BoundsInt(x, MapH - y - h, 0, w, h, 1), block);
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    private static void SetupCollision(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>(); // Composite가 Dynamic 바디를 자동으로 붙이기 전에 Static으로
        rb.bodyType = RigidbodyType2D.Static;
        go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
    }

    // ───────────────────────── 청사진 그림 생성 ─────────────────────────

    private static readonly (string name, bool collide)[] TileNames =
    {
        ("Snow", false), ("Plateau", false), ("Road", false), ("Plaza", false), ("Stairs", false), ("Bridge", false), ("Ice", false), ("Floor", false),
        ("SnowPile", false),
        ("Forest", true), ("Cliff", true), ("RockWall", true), ("Stream", true), ("Drift", true), ("Wall", true),
    };

    private static Dictionary<string, Tile> CreateTiles()
    {
        Directory.CreateDirectory(TileFolder);
        var result = new Dictionary<string, Tile>();
        foreach (var (name, collide) in TileNames)
        {
            var sprite = GetOrCreateSprite("Tile_" + name, () => DrawTile(name), 0);
            string path = $"{TileFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, path);
            }
            result[name] = tile;
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    private static Dictionary<Shape, Sprite> CreateObjectSprites() => new()
    {
        [Shape.Box] = GetOrCreateSprite("Block_Box", DrawBox, 1),
        [Shape.Circle] = GetOrCreateSprite("Block_Circle", DrawCircle, 0),
        [Shape.Tree] = GetOrCreateSprite("Block_Tree", DrawTree, 0),
    };

    /// <summary>PNG가 없을 때만 그려서 저장하고, 픽셀 아트 설정으로 임포트합니다.</summary>
    private static Sprite GetOrCreateSprite(string name, System.Func<Px> draw, int border)
    {
        Directory.CreateDirectory(TexFolder);
        string path = $"{TexFolder}/{name}.png";
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, draw().EncodePng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PPU;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // Sliced에 필요
            importer.SetTextureSettings(settings);
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Px DrawTile(string name)
    {
        var p = new Px();
        switch (name)
        {
            case "Snow": p.Fill("#1C4580").Grid("#2A5A99"); break;
            case "Plateau": p.Fill("#21508F").Grid("#3069AB").Dot(8, 8, "#4A84C8").Dot(24, 24, "#4A84C8"); break;
            case "Road": p.Fill("#2B5E9F").Grid("#3C72B4"); break;
            case "Plaza": p.Fill("#27599A").Grid("#3C72B4").Rect(8, 8, 16, 16, "#3A6EAF"); break;
            case "Stairs": p.Fill("#2B5E9F").Grid("#3C72B4").HLine(8, "#A8CBF2").HLine(16, "#A8CBF2").HLine(24, "#A8CBF2"); break;
            case "Bridge": p.Fill("#2F67A6").VLine(8, "#6E9BD0").VLine(16, "#6E9BD0").VLine(24, "#6E9BD0").Grid("#8DB5E3"); break;
            case "Ice": p.Fill("#2C7BC0").Grid("#7FC0EE"); break;
            case "Floor": p.Fill("#1E3F70").Grid("#2C5288"); break;
            case "SnowPile": p.Ellipse(16, 12, 9, 5, "#E6F0FF"); break;
            case "Forest": p.Fill("#143563").TreeIcon("#CFE3FF"); break;
            case "Cliff": p.Fill("#173C6E").Hatch(6, "#8FB5E3"); break;
            case "RockWall": p.Fill("#102C55").Hatch(4, "#7EA3D3"); break;
            case "Stream": p.Fill("#1E66A8").Wave(10, "#A9D6F7").Wave(22, "#A9D6F7"); break;
            case "Drift": p.Fill("#1C4580").Dots(4, "#C9DCF5"); break;
            case "Wall": p.Fill("#102C55").HLine(10, "#6F93C4").HLine(21, "#6F93C4").Grid("#6F93C4"); break;
        }
        return p;
    }

    private static Px DrawBox()
    {
        var p = new Px();
        for (int y = 0; y < Px.S; y++)
        for (int x = 0; x < Px.S; x++)
        {
            bool edge = x == 0 || y == 0 || x == Px.S - 1 || y == Px.S - 1;
            p.Set(x, y, new Color32(255, 255, 255, edge ? (byte)255 : (byte)60));
        }
        return p;
    }

    private static Px DrawCircle()
    {
        var p = new Px();
        for (int y = 0; y < Px.S; y++)
        for (int x = 0; x < Px.S; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
            if (d <= 15.5f) p.Set(x, y, new Color32(255, 255, 255, d > 14.3f ? (byte)255 : (byte)60));
        }
        return p;
    }

    private static Px DrawTree()
    {
        var p = new Px();
        // 몸통
        for (int y = 0; y < 5; y++) for (int x = 14; x < 18; x++) p.Set(x, y, new Color32(255, 255, 255, 255));
        // 삼각형 잎
        for (int y = 5; y < Px.S; y++)
        {
            float half = 15f * (Px.S - 1 - y) / (Px.S - 6);
            for (int x = 0; x < Px.S; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - 16f);
                if (dx > half) continue;
                bool edge = y == 5 || dx > half - 1.3f;
                p.Set(x, y, new Color32(255, 255, 255, edge ? (byte)255 : (byte)60));
            }
        }
        return p;
    }

    /// <summary>32×32 픽셀 그리기 도우미. y는 아래가 0입니다.</summary>
    private sealed class Px
    {
        public const int S = 32;
        private readonly Color32[] pixels = new Color32[S * S];

        private static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < S && y < S) pixels[y * S + x] = c; }
        public Px Fill(string hex) { var c = C(hex); for (int i = 0; i < pixels.Length; i++) pixels[i] = c; return this; }
        // 위·왼쪽 가장자리 1px: 타일을 이으면 모눈이 됩니다.
        public Px Grid(string hex) { var c = C(hex); for (int i = 0; i < S; i++) { Set(i, S - 1, c); Set(0, i, c); } return this; }
        public Px HLine(int y, string hex) { var c = C(hex); for (int x = 0; x < S; x++) Set(x, y, c); return this; }
        public Px VLine(int x, string hex) { var c = C(hex); for (int y = 0; y < S; y++) Set(x, y, c); return this; }
        public Px Dot(int x, int y, string hex) { var c = C(hex); Set(x, y, c); Set(x + 1, y, c); Set(x, y + 1, c); Set(x + 1, y + 1, c); return this; }
        public Px Dots(int step, string hex) { var c = C(hex); for (int y = 2; y < S; y += step) for (int x = 2 + (y / step % 2) * 2; x < S; x += step) Set(x, y, c); return this; }
        public Px Hatch(int step, string hex) { var c = C(hex); for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if ((x + y) % step == 0) Set(x, y, c); return this; }
        public Px Rect(int x0, int y0, int w, int h, string hex)
        {
            var c = C(hex);
            for (int i = 0; i < w; i++) { Set(x0 + i, y0, c); Set(x0 + i, y0 + h - 1, c); }
            for (int i = 0; i < h; i++) { Set(x0, y0 + i, c); Set(x0 + w - 1, y0 + i, c); }
            return this;
        }
        public Px Wave(int y0, string hex)
        {
            var c = C(hex);
            for (int x = 0; x < S; x++) Set(x, y0 + Mathf.RoundToInt(2f * Mathf.Sin(x / (float)S * Mathf.PI * 4f)), c);
            return this;
        }
        public Px Ellipse(int cx, int cy, int rx, int ry, string hex)
        {
            var c = C(hex);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Pow((x - cx) / (float)rx, 2) + Mathf.Pow((y - cy) / (float)ry, 2);
                if (d <= 1f && d > 0.6f) Set(x, y, c);
            }
            return this;
        }
        public Px TreeIcon(string hex)
        {
            var c = C(hex);
            for (int y = 9; y <= 27; y++)
            {
                int half = (27 - y) * 9 / 18;
                Set(16 - half, y, c); Set(16 + half, y, c);
            }
            for (int x = 7; x <= 25; x++) Set(x, 9, c);
            for (int y = 4; y < 9; y++) Set(16, y, c);
            return this;
        }

        public byte[] EncodePng()
        {
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }
    }

    // ───────────────────────── 타일 팔레트 ─────────────────────────

    private static void CreatePaletteIfMissing(Dictionary<string, Tile> tiles)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) != null) return;

        GridPaletteUtility.CreateNewPalette(ArtFolder, Path.GetFileNameWithoutExtension(PalettePath),
            GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, Vector3.one,
            GridLayout.CellSwizzle.XYZ, TransparencySortMode.CustomAxis, Vector3.up);

        // 위 줄: 지나갈 수 있는 바닥·장식 / 아래 줄: 막히는 지형
        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        var tilemap = root.GetComponentInChildren<Tilemap>();
        int walk = 0, block = 0;
        foreach (var (name, collide) in TileNames)
            tilemap.SetTile(collide ? new Vector3Int(block++, -2, 0) : new Vector3Int(walk++, 0, 0), tiles[name]);
        PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
