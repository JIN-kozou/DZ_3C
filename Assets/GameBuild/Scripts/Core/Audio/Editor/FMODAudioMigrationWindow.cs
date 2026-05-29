#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class FMODAudioMigrationWindow : EditorWindow
{
    private const string EventsRoot = "Assets/GameBuild/Resources/Config/Audio/Events";
    private const string PrefabScanRoot = "Assets/GameBuild/Audio";
    private const string CsvExportPath = "Assets/GameBuild/Resources/Config/Audio/fmod_event_map.csv";

    [MenuItem("GameBuild/Audio/FMOD Migration Window")]
    public static void Open()
    {
        GetWindow<FMODAudioMigrationWindow>("FMOD Migration");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("FMOD Sound Event Migration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans GameBuild audio prefabs, creates FMODSoundEvent ScriptableObjects with suggested event paths, " +
            "and exports a CSV for FMOD Studio authoring.",
            MessageType.Info);

        if (GUILayout.Button("Generate FMODSoundEvent Assets + CSV"))
        {
            GenerateFromPrefabs();
        }

        if (GUILayout.Button("Create / Update FMODDefaultEvents Asset"))
        {
            CreateDefaultEventsAsset();
        }

        if (GUILayout.Button("Create GameAudioSettings Asset"))
        {
            CreateSettingsAsset();
        }
    }

    private static void GenerateFromPrefabs()
    {
        Directory.CreateDirectory(EventsRoot);

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabScanRoot });
        List<string> csvLines = new List<string> { "asset_path,suggested_event_path,so_asset_path" };

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            AudioSource source = prefab.GetComponent<AudioSource>() ?? prefab.GetComponentInChildren<AudioSource>(true);
            if (source == null || source.clip == null)
            {
                continue;
            }

            string suggestedPath = SuggestEventPath(prefabPath);
            string soFileName = Path.GetFileNameWithoutExtension(prefabPath).Replace(" ", "_");
            string soPath = $"{EventsRoot}/{soFileName}.asset";

            FMODSoundEvent soundEvent = AssetDatabase.LoadAssetAtPath<FMODSoundEvent>(soPath);
            if (soundEvent == null)
            {
                soundEvent = CreateInstance<FMODSoundEvent>();
                AssetDatabase.CreateAsset(soundEvent, soPath);
            }

            bool isLoop = source.loop;
            soundEvent.SetEventPath(suggestedPath, is3D: !IsMusicOrUi(suggestedPath), loop: isLoop);
            EditorUtility.SetDirty(soundEvent);

            csvLines.Add($"{prefabPath},{suggestedPath},{soPath}");
        }

        File.WriteAllText(CsvExportPath, string.Join("\n", csvLines), Encoding.UTF8);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FMOD Migration] Generated {prefabGuids.Length} prefab scans. CSV: {CsvExportPath}");
    }

    private static void CreateDefaultEventsAsset()
    {
        const string path = "Assets/GameBuild/Resources/Config/Audio/FMODDefaultEvents.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        FMODDefaultEventsSO defaults = AssetDatabase.LoadAssetAtPath<FMODDefaultEventsSO>(path);
        if (defaults == null)
        {
            defaults = CreateInstance<FMODDefaultEventsSO>();
            AssetDatabase.CreateAsset(defaults, path);
        }

        defaults.gunShoot = LoadEvent("Gun_Shoot");
        defaults.gunHit = LoadEvent("Gun_Hit");
        defaults.gunBulletFly = LoadEvent("Gun_BulletFly");
        defaults.enemyLaser = LoadEvent("Enemy_Laser");
        defaults.enemyHitReaction = LoadEvent("Enemy_HitReaction");
        defaults.enemySpawn = LoadEvent("Enemy_Spawn");
        defaults.enemyTargetHowl = LoadEvent("Enemy_TargetHowl");
        defaults.enemyPatrolSonar = LoadEvent("Enemy_PatrolSonar");
        defaults.enemyPatrolBreathing = LoadEvent("Enemy_PatrolBreathing");
        defaults.reversePlace = LoadEvent("Reverse_Place");
        defaults.reverseStartup = LoadEvent("Reverse_Startup");
        defaults.reverseRecall = LoadEvent("Reverse_Recall");
        defaults.envVentGas = LoadEvent("Env_VentGas");
        defaults.envSpark = LoadEvent("Env_Spark");
        defaults.envGateGas = LoadEvent("Env_GateGas");
        defaults.envMachineryLoop = LoadEvent("Env_MachineryLoop");
        defaults.projectileSpawn = LoadEvent("Projectile_Spawn");
        defaults.projectileFlight = LoadEvent("Projectile_Flight");
        defaults.projectileHit = LoadEvent("Projectile_Hit");
        defaults.projectileDespawn = LoadEvent("Projectile_Despawn");

        EditorUtility.SetDirty(defaults);
        AssetDatabase.SaveAssets();
        Debug.Log("[FMOD Migration] Updated FMODDefaultEvents.asset");
    }

    private static void CreateSettingsAsset()
    {
        const string path = "Assets/GameBuild/Resources/Config/Audio/GameAudioSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<GameAudioSettingsSO>(path) != null)
        {
            Debug.Log("[FMOD Migration] GameAudioSettings already exists.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        GameAudioSettingsSO settings = CreateInstance<GameAudioSettingsSO>();
        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        Debug.Log("[FMOD Migration] Created GameAudioSettings.asset");
    }

    private static FMODSoundEvent LoadEvent(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<FMODSoundEvent>($"{EventsRoot}/{fileName}.asset");
    }

    private static string SuggestEventPath(string prefabPath)
    {
        string normalized = prefabPath.Replace("\\", "/");
        string file = Path.GetFileNameWithoutExtension(normalized).Replace(" ", "_");

        if (normalized.Contains("/Music/") || file.Contains("mainthyme") || file.Contains("battle"))
        {
            return file.Contains("battle") ? "event:/Music/Combat" : "event:/Music/Exploration";
        }

        if (normalized.Contains("/Enemy/"))
        {
            return $"event:/Enemy/{file}";
        }

        if (normalized.Contains("/Character/Footsteps"))
        {
            return "event:/Player/Footstep";
        }

        if (normalized.Contains("/Character/"))
        {
            return $"event:/Player/{file}";
        }

        if (normalized.Contains("/Timeline"))
        {
            return $"event:/Timeline/{file}";
        }

        return $"event:/SFX/{file}";
    }

    private static bool IsMusicOrUi(string eventPath)
    {
        return eventPath.StartsWith("event:/Music/") || eventPath.StartsWith("event:/UI/");
    }
}
#endif
