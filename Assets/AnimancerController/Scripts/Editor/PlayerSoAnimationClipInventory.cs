using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 从 <see cref="PlayerSO"/> 序列化引用闭包收集所有 <see cref="AnimationClip"/>（含 FBX 内嵌片段），用于资源梳理。
/// </summary>
public sealed class PlayerSoAnimationClipInventoryWindow : EditorWindow
{
    private const string DefaultResourcesLoadPath = "Config/Player/Player SO";
    private const string DefaultAssetDatabasePath =
        "Assets/AnimancerController/Resources/Config/Player/Player SO.asset";

    [SerializeField] private PlayerSO playerSo;
    private Vector2 scroll;
    private string lastLogSummary = string.Empty;
    private List<ClipRow> lastRows = new List<ClipRow>();

    [MenuItem("Tools/Player/List AnimationClips from PlayerSO")]
    private static void OpenAndList()
    {
        var win = GetWindow<PlayerSoAnimationClipInventoryWindow>();
        win.titleContent = new GUIContent("Player SO Clips");
        win.minSize = new Vector2(420, 280);
        if (win.playerSo == null)
        {
            win.playerSo = ResolveDefaultPlayerSo();
        }

        win.RunInventory(logToConsole: true);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUI.BeginChangeCheck();
        playerSo = (PlayerSO)EditorGUILayout.ObjectField("Player SO", playerSo, typeof(PlayerSO), false);
        if (EditorGUI.EndChangeCheck() && playerSo != null)
        {
            lastRows.Clear();
            lastLogSummary = string.Empty;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Resolve default SO", GUILayout.Width(140)))
            {
                playerSo = ResolveDefaultPlayerSo();
            }

            if (GUILayout.Button("Refresh list", GUILayout.Width(100)))
            {
                RunInventory(logToConsole: true);
            }
        }

        EditorGUILayout.HelpBox(
            "Collects AnimationClips from the serialized dependency closure of Player SO (ClipTransition, TransitionAsset mixers, and other referenced assets).",
            MessageType.Info);

        if (!string.IsNullOrEmpty(lastLogSummary))
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(lastLogSummary, GUILayout.Height(36));
        }

        EditorGUILayout.LabelField($"Clips ({lastRows.Count})", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var row in lastRows)
        {
            EditorGUILayout.LabelField(row.ClipName, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  " + row.MainAssetPath, EditorStyles.miniBoldLabel);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Export CSV…"))
            {
                ExportCsvDialog();
            }
        }
    }

    private static PlayerSO ResolveDefaultPlayerSo()
    {
        var fromResources = Resources.Load<PlayerSO>(DefaultResourcesLoadPath);
        if (fromResources != null)
        {
            return fromResources;
        }

        return AssetDatabase.LoadAssetAtPath<PlayerSO>(DefaultAssetDatabasePath);
    }

    private void RunInventory(bool logToConsole)
    {
        if (playerSo == null)
        {
            playerSo = ResolveDefaultPlayerSo();
        }

        if (playerSo == null)
        {
            const string msg =
                "Player SO not assigned and default not found. Assign Player SO or ensure it exists at Resources path \"" +
                DefaultResourcesLoadPath + "\" or \"" + DefaultAssetDatabasePath + "\".";
            if (logToConsole)
            {
                Debug.LogError(msg);
            }
            else
            {
                EditorUtility.DisplayDialog("Player SO Clips", msg, "OK");
            }

            lastRows = new List<ClipRow>();
            lastLogSummary = msg;
            return;
        }

        lastRows = CollectClipRows(playerSo);
        lastLogSummary = $"{playerSo.name}: {lastRows.Count} unique AnimationClip(s)";

        if (!logToConsole)
        {
            return;
        }

        var sb = new StringBuilder(1024);
        sb.AppendLine(lastLogSummary);
        sb.AppendLine("MainAssetPath\tClipName\tLength\tLegacy");
        foreach (var row in lastRows)
        {
            sb.Append(row.MainAssetPath).Append('\t')
                .Append(row.ClipName).Append('\t')
                .Append(row.LengthSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append('\t')
                .Append(row.Legacy ? "1" : "0")
                .AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    private void ExportCsvDialog()
    {
        if (playerSo == null)
        {
            EditorUtility.DisplayDialog("Export CSV", "Assign Player SO first.", "OK");
            return;
        }

        if (lastRows.Count == 0)
        {
            RunInventory(logToConsole: false);
        }

        var suggested = SanitizeFileName(playerSo.name) + "_AnimationClips.csv";
        var path = EditorUtility.SaveFilePanel("Export AnimationClip list", "", suggested, "csv");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            WriteCsv(path, lastRows);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"Exported {lastRows.Count} clip(s) to {path}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Export CSV", "Failed: " + e.Message, "OK");
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrEmpty(name) ? "PlayerSO" : name;
    }

    private static void WriteCsv(string path, IReadOnlyList<ClipRow> rows)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("MainAssetPath,ClipName,LengthSeconds,Legacy,HumanoidRig");
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                CsvEscape(row.MainAssetPath),
                CsvEscape(row.ClipName),
                row.LengthSeconds.ToString(CultureInfo.InvariantCulture),
                row.Legacy ? "true" : "false",
                row.HasHumanMotion ? "true" : "false"));
        }
    }

    private static string CsvEscape(string s)
    {
        if (s.IndexOfAny(new[] { '"', ',', '\r', '\n' }) < 0)
        {
            return s;
        }

        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    internal static List<ClipRow> CollectClipRows(PlayerSO root)
    {
        var deps = EditorUtility.CollectDependencies(new UnityEngine.Object[] { root });
        var clips = new List<AnimationClip>();
        foreach (var o in deps)
        {
            if (o is AnimationClip clip && clip != null)
            {
                clips.Add(clip);
            }
        }

        var distinct = clips
            .GroupBy(c => c.GetInstanceID())
            .Select(g => g.First())
            .OrderBy(c => AssetDatabase.GetAssetPath(c), StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<ClipRow>(distinct.Count);
        foreach (var clip in distinct)
        {
            rows.Add(ClipRow.FromClip(clip));
        }

        return rows;
    }

    internal readonly struct ClipRow
    {
        public readonly string MainAssetPath;
        public readonly string ClipName;
        public readonly float LengthSeconds;
        public readonly bool Legacy;
        public readonly bool HasHumanMotion;

        private ClipRow(string mainAssetPath, string clipName, float lengthSeconds, bool legacy, bool hasHumanMotion)
        {
            MainAssetPath = mainAssetPath;
            ClipName = clipName;
            LengthSeconds = lengthSeconds;
            Legacy = legacy;
            HasHumanMotion = hasHumanMotion;
        }

        public static ClipRow FromClip(AnimationClip clip)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            // Avoid AnimationClip.humanMotion (API differs by Unity version / may not be a struct with muscles).
            var humanRig = IsClipOnHumanoidModelImporter(path);

            return new ClipRow(
                path ?? string.Empty,
                clip.name ?? string.Empty,
                clip.length,
                clip.legacy,
                humanRig);
        }

        /// <summary>
        /// True when the clip lives on a model whose <see cref="ModelImporter"/> rig is Humanoid (typical for FBX sub-clips).
        /// </summary>
        private static bool IsClipOnHumanoidModelImporter(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            return importer != null && importer.animationType == ModelImporterAnimationType.Human;
        }
    }
}
