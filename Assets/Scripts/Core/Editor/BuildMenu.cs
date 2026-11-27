using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Apply Build Settings (6) / Build Windows (6)  (로드맵 6단계 빌드)
///  Apply Build Settings
///   - 회사·제품 이름 (저장 폴더 이름이 됨: %USERPROFILE%/AppData/LocalLow/{회사}/{제품})
///   - 기본 1920×1080 전체 화면(창 모드에서는 크기 조절 가능), 버전
///   - 아이콘: 32×32 픽셀 그림(밤하늘 + 봉투 + 빨간 봉인)을 코드로 그려 256×256으로 확대 저장
///   - 빌드 씬 순서: Title → NameBadge → Village
///  Build Windows
///   - Builds/Windows/MerryMemory.exe 로 빌드 (Builds/ 는 .gitignore에 들어 있음)
/// </summary>
public static class BuildMenu
{
    private const string Company = "bbabang7709";
    private const string Product = "Merry Memory";
    private const string Version = "1.0";
    private const string IconPath = "Assets/Art/UI/Icon.png";
    private const string OutputPath = "Builds/Windows/MerryMemory.exe";

    private static readonly string[] Scenes =
    {
        $"Assets/Scenes/{SceneNames.Title}.unity",
        $"Assets/Scenes/{SceneNames.NameBadge}.unity",
        $"Assets/Scenes/{SceneNames.Village}.unity",
    };

    [MenuItem("Merry Memory/Apply Build Settings (6)")]
    private static void Apply()
    {
        PlayerSettings.companyName = Company;
        PlayerSettings.productName = Product;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;

        var icon = MakeIcon();
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        EditorBuildSettings.scenes = Scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"빌드 설정(6): {Company} / {Product} {Version}, 1920×1080 전체 화면, 아이콘, 씬 {Scenes.Length}개");
    }

    [MenuItem("Merry Memory/Build Windows (6)")]
    private static void Build()
    {
        Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
            Debug.Log($"빌드 성공: {OutputPath} ({s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0}초)");
        else
            Debug.LogError($"빌드 실패: {s.result}, 에러 {s.totalErrors}개");
    }

    /// <summary>32×32 픽셀 아이콘을 그려서 8배(256×256)로 저장하고 불러옵니다.</summary>
    private static Texture2D MakeIcon()
    {
        const int n = 32, scale = 8;
        var px = new Color32[n * n];
        var sky = new Color32(20, 33, 61, 255);
        var paper = new Color32(246, 240, 220, 255);
        var line = new Color32(190, 176, 150, 255);
        var seal = new Color32(184, 51, 41, 255);
        var star = new Color32(255, 214, 115, 255);
        void Set(int x, int y, Color32 c) { if (x >= 0 && x < n && y >= 0 && y < n) px[y * n + x] = c; }

        for (int i = 0; i < px.Length; i++) px[i] = sky;
        // 눈송이·별
        foreach (var (x, y) in new[] { (4, 27), (9, 29), (26, 28), (29, 24), (3, 20), (28, 8), (6, 5) }) Set(x, y, paper);
        Set(24, 26, star); Set(23, 26, star); Set(25, 26, star); Set(24, 25, star); Set(24, 27, star);
        // 봉투 (x 5~26, y 8~22)
        for (int y = 8; y <= 22; y++)
            for (int x = 5; x <= 26; x++)
                Set(x, y, (x == 5 || x == 26 || y == 8 || y == 22) ? line : paper);
        // 덮개 V자
        for (int i = 0; i <= 10; i++) { Set(5 + i, 22 - i, line); Set(26 - i, 22 - i, line); }
        // 빨간 봉인
        for (int y = 9; y <= 14; y++)
            for (int x = 13; x <= 18; x++)
                if (!((x == 13 || x == 18) && (y == 9 || y == 14))) Set(x, y, seal);

        var big = new Texture2D(n * scale, n * scale, TextureFormat.RGBA32, false);
        for (int y = 0; y < n * scale; y++)
            for (int x = 0; x < n * scale; x++)
                big.SetPixel(x, y, px[(y / scale) * n + x / scale]);
        Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
        File.WriteAllBytes(IconPath, big.EncodeToPNG());
        Object.DestroyImmediate(big);

        AssetDatabase.ImportAsset(IconPath);
        var imp = (TextureImporter)AssetImporter.GetAtPath(IconPath);
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }
}
