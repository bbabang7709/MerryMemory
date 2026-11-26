using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 메뉴 Merry Memory → Check Dialogue (5-3)  대사 검사 + 대본 내보내기
///  ① 게임의 모든 글을 모읍니다
///     - 대사 에셋(DialogueData)의 말한 사람·대사
///     - 카드 조각(CardPieceData)의 이름·만든 사람·한마디
///     - 스크립트 안의 한글 문자열 (Say("...")처럼 코드에서 즉석으로 만드는 대사)
///       Editor 폴더·개발자용 메시지(Debug.Log 등)는 빼고, $"..."는 {식} 자리를 채운 "화면에 나올 모양"으로
///  ② 검사 → 콘솔에 문제 목록
///     - 폰트에 없는 글자 (4-5의 "…"처럼 화면에 네모로 나오는 글자)
///     - 모르는 {표기} (예: {이릅} 오타 → TextFormatter가 못 바꾸고 중괄호가 그대로 남음)
///  ③ Docs/DIALOGUE.md 로 대본 전체를 내보냄 → 한눈에 읽으며 말투·오타 다듬기용
/// </summary>
public static class DialogueCheckMenu
{
    private const string FontPath = "Assets/Fonts/Pretendard-Regular SDF.asset";
    private const string ScriptsRoot = "Assets/Scripts";
    private const string OutputPath = "Docs/DIALOGUE.md";
    private const string SampleName = "지민"; // {이름} 자리에 넣어 볼 이름 (받침 있는 이름)

    // 스크립트의 "..." 문자열 중 한글이 들어 있는 것. 1번 그룹 = 앞의 $ (보간 문자열 표시), 2번 그룹 = 내용
    private static readonly Regex KoreanLiteral = new("(\\$?)\"((?:[^\"\\\\]|\\\\.)*[가-힣](?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);

    // $"..." 안의 {식} 자리
    private static readonly Regex Hole = new(@"\{[^{}]*\}", RegexOptions.Compiled);

    // TextFormatter가 못 바꾼 표기: 안에 공백이 없는 {…} ("{ } 기호는…" 같은 안내 문구는 표기가 아님)
    private static readonly Regex LeftoverToken = new(@"\{[^{}\s]+\}", RegexOptions.Compiled);

    // 개발자에게만 보이는 메시지 줄 (플레이어 화면에 안 나옴)
    private static readonly string[] DeveloperOnly = { "Debug.Log", "LogWarning", "LogError", "DisplayDialog", "throw " };

    /// <summary>검사할 글 한 줄: 어디서 왔는지 + 내용</summary>
    private struct Entry
    {
        public string group;   // 대본에서 묶을 제목 (폴더·파일)
        public string where;   // 콘솔에 보일 위치
        public string text;
    }

    [MenuItem("Merry Memory/Check Dialogue (5-3)")]
    private static void Check()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError($"폰트를 찾을 수 없습니다: {FontPath}");
            return;
        }

        var entries = Collect();
        var problems = new List<string>();

        foreach (var e in entries)
        {
            // {이름}과 조사를 실제로 바꿔 본 글 = 화면에 나올 글
            string shown = TextFormatter.Format(e.text, SampleName);

            var missing = new SortedSet<char>();
            CollectMissingGlyphs(shown, font, missing);
            if (missing.Count > 0)
                problems.Add($"[폰트에 없는 글자 {string.Join(" ", missing)}] {e.where}: {e.text}");

            if (HasUnknownToken(shown))
                problems.Add($"[모르는 표기] {e.where}: {e.text}");
        }

        WriteScript(entries);

        if (problems.Count == 0)
            Debug.Log($"대사 검사 끝: {entries.Count}줄, 문제 없음. 대본 → {OutputPath}");
        else
            Debug.LogWarning($"대사 검사 끝: {entries.Count}줄 중 문제 {problems.Count}개. 대본 → {OutputPath}\n" + string.Join("\n", problems));
    }

    /// <summary>
    /// text의 글자 중 font에 없는 글자를 missing에 모읍니다.
    /// 공백·줄바꿈은 폰트에 없어도 화면에 문제없으므로 건너뜁니다.
    /// HasCharacter(글자, true, false): 예비 폰트(fallback)까지 찾아보되, 없는 글자를 새로 만들지는 않음
    /// </summary>
    private static void CollectMissingGlyphs(string text, TMP_FontAsset font, SortedSet<char> missing)
    {
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (!font.HasCharacter(c, true, false)) missing.Add(c);
        }
    }

    /// <summary>
    /// 바꾼 뒤에도 중괄호가 남아 있으면 TextFormatter가 모르는 표기입니다. (예: {이릅})
    /// </summary>
    private static bool HasUnknownToken(string formatted) => LeftoverToken.IsMatch(formatted);

    // ───────────────────────── 모으기 (완성) ─────────────────────────

    private static List<Entry> Collect()
    {
        var list = new List<Entry>();

        // 대사 에셋
        foreach (var guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null) continue;
            string group = Path.GetDirectoryName(path).Replace('\\', '/').Replace("Assets/ScriptableObjects/Dialogues", "대사").TrimEnd('/');
            for (int i = 0; i < data.lines.Count; i++)
            {
                var line = data.lines[i];
                string speaker = string.IsNullOrEmpty(line.speaker) ? "(지문)" : line.speaker;
                list.Add(new Entry { group = $"{group} / {data.name}", where = $"{path} #{i + 1}", text = $"{speaker}: {line.text}" });
            }
        }

        // 카드 조각
        foreach (var guid in AssetDatabase.FindAssets("t:CardPieceData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var piece = AssetDatabase.LoadAssetAtPath<CardPieceData>(path);
            if (piece == null) continue;
            string group = $"카드 조각 / {piece.number}. {piece.displayName}";
            list.Add(new Entry { group = group, where = path, text = $"{piece.makerName}의 한마디: {piece.message}" });
        }

        // 스크립트 속 한글 문자열 (게임 중에 즉석으로 만드는 대사·화면 글)
        //   Editor 폴더(셋업 메뉴)는 뺌 → 그 메뉴들이 만든 대사 에셋을 위에서 이미 검사함
        //   Formatting(TextFormatter)은 정규식·예시뿐이라 뺌
        foreach (var file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories).OrderBy(f => f))
        {
            string rel = file.Replace('\\', '/');
            if (rel.Contains("/Editor/") || rel.Contains("/Formatting/")) continue;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("[Tooltip") || trimmed.StartsWith("[Header")) continue;
                if (DeveloperOnly.Any(lines[i].Contains)) continue;
                foreach (Match m in KoreanLiteral.Matches(lines[i]))
                {
                    string text = Regex.Unescape(m.Groups[2].Value);
                    if (m.Groups[1].Value == "$") text = AsPrinted(text);
                    list.Add(new Entry { group = $"코드 / {Path.GetFileNameWithoutExtension(file)}", where = $"{rel}:{i + 1}", text = text });
                }
            }
        }
        return list;
    }

    /// <summary>$"..."를 화면에 나올 모양으로: {식} 자리 → "가"(글자 하나), {{ }} → { }</summary>
    private static string AsPrinted(string interpolated)
    {
        const char open = '\u0001', close = '\u0002'; // {{ }}를 잠깐 숨겨 둘 표시
        string s = interpolated.Replace("{{", open.ToString()).Replace("}}", close.ToString());
        s = Hole.Replace(s, "가");
        return s.Replace(open, '{').Replace(close, '}');
    }

    private static void WriteScript(List<Entry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MERRY MEMORY 대본");
        sb.AppendLine();
        sb.AppendLine("> Merry Memory → Check Dialogue (5-3) 메뉴가 만든 파일입니다. 직접 고치지 말고, 원본(에셋·스크립트)을 고친 뒤 다시 만드세요.");
        sb.AppendLine($"> {{이름}}은 그대로 두었습니다. 줄 수: {entries.Count}");
        foreach (var g in entries.GroupBy(e => e.group))
        {
            sb.AppendLine();
            sb.AppendLine($"## {g.Key}");
            foreach (var e in g) sb.AppendLine($"- {e.text}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, sb.ToString(), new UTF8Encoding(false));
    }
}
