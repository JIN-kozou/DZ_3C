#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将实际参与 Player 打包的内容整理到 Assets/GameBuild/ 下，按类型分子目录。
/// 菜单：Tools / GameBuild / Organize Build Content
/// </summary>
public static class GameBuildOrganizer
{
    private const string Root = "Assets/GameBuild";

    private static readonly string[] BuildScenes =
    {
        "Assets/GameBuild/Scenes/MainMenu.unity",
        "Assets/GameBuild/Scenes/Timeline.unity",
        "Assets/GameBuild/Scenes/Level test.unity",
        "Assets/GameBuild/Scenes/TimelineEND.unity",
    };

    private static readonly (string From, string To)[] FolderMoves =
    {
        ("Assets/AnimancerController/Scripts", $"{Root}/Scripts/Core"),
        ("Assets/Scripts", $"{Root}/Scripts/Gameplay"),
        ("Assets/Prefab", $"{Root}/Prefabs"),
        ("Assets/Timeline", $"{Root}/Timeline"),
        ("Assets/Game", $"{Root}/HUD"),
        ("Assets/AnimancerController/Resources", $"{Root}/Resources"),
        ("Assets/AnimancerController/Arts", $"{Root}/Art"),
        ("Assets/AnimancerController/AudioPrefabs", $"{Root}/Audio"),
        ("Assets/AnimancerController/Shader", $"{Root}/Shaders"),
        ("Assets/Settings", $"{Root}/Settings"),
        ("Assets/AnimancerController/Model", $"{Root}/Art/Model"),
        ("Assets/AnimancerController/Config", $"{Root}/Config"),
    };

    private static readonly (string From, string To)[] FileMoves =
    {
        ("Assets/GameBuild/Prefabs/UI/WorldInteractionPrompt.prefab", $"{Root}/Resources/WorldInteraction/WorldInteractionPrompt.prefab"),
        ("Assets/GameBuild/Prefabs/UI/WorldInteractionMarker.prefab", $"{Root}/Resources/WorldInteraction/WorldInteractionMarker.prefab"),
    };

    [MenuItem("Tools/GameBuild/Organize Build Content")]
    public static void Organize()
    {
        if (!EditorUtility.DisplayDialog(
                "整理 GameBuild 目录",
                "将把打包用场景、脚本、Resources、Prefab 等移动到 Assets/GameBuild/。\n" +
                "移动通过 AssetDatabase 完成，GUID 引用会保留。\n\n是否继续？",
                "继续",
                "取消"))
            return;

        RunOrganize(silent: false);
    }

    /// <summary>供 Unity -executeMethod 批处理调用。</summary>
    public static void OrganizeFromBatch()
    {
        RunOrganize(silent: true);
        EditorApplication.Exit(0);
    }

    private static void RunOrganize(bool silent)
    {
        EnsureFolders();
        var moved = new List<string>();
        var failed = new List<string>();

        foreach (var (from, to) in FileMoves)
            MoveAssetSafe(from, to, moved, failed);

        foreach (var (from, to) in FolderMoves)
            MoveAssetSafe(from, to, moved, failed);

        foreach (var scene in BuildScenes)
            MoveAssetSafe(scene, $"{Root}/Scenes/{Path.GetFileName(scene)}", moved, failed);

        var scenesDir = "Assets/GameBuild/Scenes";
        if (AssetDatabase.IsValidFolder(scenesDir))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { scenesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (BuildScenes.Contains(path))
                    continue;
                MoveAssetSafe(path, $"{Root}/Scenes/_Dev/{Path.GetFileName(path)}", moved, failed);
            }
        }

        MergeSecondaryResources(moved, failed);

        UpdateBuildSettings();
        UpdateHardcodedScriptPaths();
        WriteReadme();
        AssetDatabase.Refresh();

        var msg = $"完成。成功 {moved.Count} 项";
        if (failed.Count > 0)
            msg += $"，失败 {failed.Count} 项：\n" + string.Join("\n", failed.Take(8));

        Debug.Log(msg);
        if (!silent)
            EditorUtility.DisplayDialog("GameBuild 整理", msg, "确定");
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            Root,
            $"{Root}/Scenes",
            $"{Root}/Scenes/_Dev",
            $"{Root}/Scripts",
            $"{Root}/Scripts/Core",
            $"{Root}/Scripts/Gameplay",
            $"{Root}/Prefabs",
            $"{Root}/Resources",
            $"{Root}/Resources/WorldInteraction",
            $"{Root}/Art",
            $"{Root}/Audio",
            $"{Root}/Shaders",
            $"{Root}/Timeline",
            $"{Root}/Settings",
            $"{Root}/HUD",
            $"{Root}/Config",
        };

        foreach (var f in folders)
        {
            if (!AssetDatabase.IsValidFolder(f))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(f)?.Replace('\\', '/') ?? "Assets", Path.GetFileName(f));
        }
    }

    private static void MergeSecondaryResources(List<string> moved, List<string> failed)
    {
        const string secondary = "Assets/GameBuild/Resources";
        if (!AssetDatabase.IsValidFolder(secondary))
            return;

        foreach (var guid in AssetDatabase.FindAssets("", new[] { secondary }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path))
                continue;
            var relative = path.Substring(secondary.Length + 1);
            MoveAssetSafe(path, $"{Root}/Resources/{relative}", moved, failed);
        }

        if (AssetDatabase.IsValidFolder(secondary))
            AssetDatabase.DeleteAsset(secondary);
    }

    private static void MoveAssetSafe(string from, string to, List<string> moved, List<string> failed)
    {
        if (string.IsNullOrEmpty(from))
            return;

        var exists = AssetDatabase.IsValidFolder(from) || AssetDatabase.LoadMainAssetAtPath(from) != null;
        if (!exists)
            return;

        if (AssetDatabase.IsValidFolder(to) || AssetDatabase.LoadMainAssetAtPath(to) != null)
        {
            moved.Add($"{from} (already at destination)");
            return;
        }

        var toDir = Path.GetDirectoryName(to)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(toDir) && !AssetDatabase.IsValidFolder(toDir))
        {
            var parts = toDir.Split('/');
            var built = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = built + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(built, parts[i]);
                built = next;
            }
        }

        var err = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(err))
            moved.Add($"{from} -> {to}");
        else
            failed.Add($"{from}: {err}");
    }

    private static void UpdateBuildSettings()
    {
        var scenes = BuildScenes
            .Select(s => $"{Root}/Scenes/{Path.GetFileName(s)}")
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
        EditorBuildSettings.scenes = scenes;
    }

    private static void UpdateHardcodedScriptPaths()
    {
        var replacements = new (string Old, string New)[]
        {
            ("Assets/GameBuild/Scripts/Core/", "Assets/GameBuild/Scripts/Core/"),
            ("Assets/GameBuild/Scenes/", "Assets/GameBuild/Scenes/"),
            ("Assets/GameBuild/Resources/", "Assets/GameBuild/Resources/"),
            ("Assets/GameBuild/Audio/", "Assets/GameBuild/Audio/"),
            ("Assets/GameBuild/Prefabs/", "Assets/GameBuild/Prefabs/"),
            ("Assets/GameBuild/Scripts/Gameplay/", "Assets/GameBuild/Scripts/Gameplay/"),
            ("Assets/GameBuild/Resources/", "Assets/GameBuild/Resources/"),
            ("Assets/GameBuild/Timeline/", "Assets/GameBuild/Timeline/"),
            ("Assets/GameBuild/HUD/", "Assets/GameBuild/HUD/"),
        };

        var scripts = AssetDatabase.FindAssets("t:Script", new[] { Root, "Assets/Editor" });
        foreach (var guid in scripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs"))
                continue;

            var text = File.ReadAllText(path);
            var updated = text;
            foreach (var (old, newPath) in replacements)
                updated = updated.Replace(old, newPath);

            if (updated != text)
                File.WriteAllText(path, updated);
        }
    }

    private static void WriteReadme()
    {
        var readme = $@"# GameBuild — 打包用资源目录

由 **Tools → GameBuild → Organize Build Content** 自动生成/更新。

## 构建场景（Build Settings）
- `Scenes/MainMenu.unity`
- `Scenes/Timeline.unity`
- `Scenes/Level test.unity`
- `Scenes/TimelineEND.unity`

## 目录说明

| 目录 | 内容 |
|------|------|
| `Scenes/` | 发布场景；`_Dev/` 为测试场景 |
| `Scripts/Core/` | 玩家、相机、音频、UI、Timeline 等核心逻辑 |
| `Scripts/Gameplay/` | 反转重力、机器修复、AI、世界交互 |
| `Resources/` | `Resources.Load` 与打包进 resources.assets 的资源 |
| `Prefabs/` | 场景引用的 Prefab（关卡、UI、机器修复等） |
| `Art/` | 动画、字体、模型贴图 |
| `Audio/` | 音频 Prefab |
| `Shaders/` | 项目自定义 Shader |
| `Timeline/` | Timeline 资源 |
| `Settings/` | URP / Animancer 等项目设置 |
| `HUD/` | 准星、命中标记等 HUD Prefab |
| `Config/` | 非 Resources 的配置资产 |

## Resources.Load 路径（运行时）

- `Config/UI/WorldInteractionConfig`
- `Config/Reverse/ReverseConfig`
- `Config/Weapon/DefaultGun`
- `Config/MachineRepair/*`
- `WorldInteraction/WorldInteractionPrompt`
- `WorldInteraction/WorldInteractionMarker`

## 仍在 Assets/ 根目录的第三方依赖（随场景引用打包）

- `Packages/com.kybernetik.animancer` — Animancer
- `Assets/GameSoftCraft/S.P.A.C.E/` — 星空背景
- `Assets/Blackhole/` — 黑洞天空
- `Assets/Snapshot Shaders Pro/` — 后处理（若场景使用）
- `Assets/Cuboom/CB Sci-Fi Pack/` — Timeline 场景道具
- `Assets/TextMesh Pro/` — UI 文字
- `Assets/Plugins/` — AllIn1、Excelsior 等

整理后请在 Unity 中 **Reimport** 并试打包到空目录。
";
        File.WriteAllText(Path.Combine(Application.dataPath, "GameBuild/README.md"), readme);
        AssetDatabase.ImportAsset($"{Root}/README.md");
    }
}
#endif
