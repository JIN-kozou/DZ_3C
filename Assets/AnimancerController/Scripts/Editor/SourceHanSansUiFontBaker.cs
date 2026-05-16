#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 UI 所需汉字烘焙进 <see cref="SourceHanSansCN-Normal SDF"/>，避免 TMP 缺字警告与方框占位。
/// </summary>
public static class SourceHanSansUiFontBaker
{
    private const string FontAssetPath =
        "Assets/AnimancerController/Arts/Font/SourceHanSansCN-Normal SDF.asset";

    private const string CharacterListPath =
        "Assets/AnimancerController/Arts/Font/SourceHanSansCN-UICharacters.txt";

    private const string SessionKey = "SourceHanSansUiFontBaker.Done";

    [InitializeOnLoadMethod]
    private static void BakeOnEditorLoadIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (BakeMissingCharacters(logToConsole: false))
            {
                SessionState.SetBool(SessionKey, true);
            }
        };
    }

    [MenuItem("Tools/TextMesh Pro/Bake SourceHanSans UI Characters")]
    private static void BakeFromMenu()
    {
        if (BakeMissingCharacters(logToConsole: true))
        {
            SessionState.SetBool(SessionKey, true);
        }
    }

    private static bool BakeMissingCharacters(bool logToConsole)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            Debug.LogError($"[SourceHanSansUiFontBaker] 找不到字体：{FontAssetPath}");
            return false;
        }

        var characters = LoadRequiredCharacters();
        if (characters.Length == 0)
        {
            return false;
        }

        var missing = new List<char>();
        foreach (var c in characters)
        {
            if (!font.HasCharacter(c, searchFallbacks: true))
            {
                missing.Add(c);
            }
        }

        if (missing.Count == 0)
        {
            if (logToConsole)
            {
                Debug.Log("[SourceHanSansUiFontBaker] 字体已包含全部 UI 字符，无需烘焙。");
            }

            return true;
        }

        var toAdd = new string(missing.ToArray());
        if (!font.TryAddCharacters(toAdd, out var stillMissing))
        {
            Debug.LogError(
                $"[SourceHanSansUiFontBaker] 无法写入图集，仍缺字：{stillMissing}。可增大 Atlas Size 或启用 Multi Atlas。");
            return false;
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (logToConsole)
        {
            Debug.Log($"[SourceHanSansUiFontBaker] 已烘焙 {missing.Count} 个字符：{toAdd}");
        }

        return true;
    }

    private static char[] LoadRequiredCharacters()
    {
        var set = new HashSet<char>();

        if (File.Exists(CharacterListPath))
        {
            var text = File.ReadAllText(CharacterListPath, Encoding.UTF8);
            foreach (var c in text)
            {
                if (!char.IsControl(c) || c == '\n')
                {
                    set.Add(c);
                }
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/AnimancerController/Scenes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sceneText = File.ReadAllText(path, Encoding.UTF8);
            foreach (Match match in Regex.Matches(
                         sceneText, @"m_text:\s*""((?:\\.|[^""\\])*)"""))
            {
                var raw = match.Groups[1].Value;
                var decoded = RegexUnescapeUnityYamlString(raw);
                foreach (var c in decoded)
                {
                    if (!char.IsControl(c))
                    {
                        set.Add(c);
                    }
                }
            }
        }

        set.Remove('\n');
        set.Remove('\r');
        set.Remove('\t');

        var result = new char[set.Count];
        set.CopyTo(result);
        return result;
    }

    private static string RegexUnescapeUnityYamlString(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '\\' || i + 1 >= raw.Length)
            {
                sb.Append(raw[i]);
                continue;
            }

            var next = raw[++i];
            if (next == 'n')
            {
                sb.Append('\n');
            }
            else if (next == 'r')
            {
                sb.Append('\r');
            }
            else if (next == 't')
            {
                sb.Append('\t');
            }
            else if (next == 'u' && i + 4 < raw.Length)
            {
                var hex = raw.Substring(i + 1, 4);
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code))
                {
                    sb.Append((char)code);
                    i += 4;
                }
                else
                {
                    sb.Append('\\').Append(next);
                }
            }
            else
            {
                sb.Append(next);
            }
        }

        return sb.ToString();
    }
}
#endif
