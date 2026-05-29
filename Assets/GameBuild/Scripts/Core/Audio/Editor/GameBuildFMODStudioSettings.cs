#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class GameBuildFMODStudioSettings
{
    private const string StudioProjectRelativePath = "FMOD/DZ_3C.fspro";
    private const string BankBuildRelativePath = "Assets/StreamingAssets/FMOD/Desktop";

    static GameBuildFMODStudioSettings()
    {
        EditorApplication.delayCall += TryApplyStudioProjectPath;
    }

    private static void TryApplyStudioProjectPath()
    {
        System.Type settingsType = System.Type.GetType("FMODUnity.Settings, FMODUnityEditor");
        if (settingsType == null)
        {
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string fsproPath = Path.Combine(projectRoot, StudioProjectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fsproPath))
        {
            return;
        }

        ScriptableObject settings = GetSettingsInstance(settingsType);
        if (settings == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty sourceProjectPath = serialized.FindProperty("SourceProjectPath");
        SerializedProperty sourceBankPath = serialized.FindProperty("SourceBankPath");

        if (sourceProjectPath != null)
        {
            sourceProjectPath.stringValue = MakeRelativeToAssets(fsproPath);
        }

        if (sourceBankPath != null)
        {
            sourceBankPath.stringValue = BankBuildRelativePath;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ScriptableObject GetSettingsInstance(System.Type settingsType)
    {
        var instanceProperty = settingsType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return instanceProperty?.GetValue(null) as ScriptableObject;
    }

    private static string MakeRelativeToAssets(string absolutePath)
    {
        string assetsPath = Path.GetFullPath(Application.dataPath);
        if (absolutePath.StartsWith(assetsPath))
        {
            return "Assets" + absolutePath.Substring(assetsPath.Length).Replace('\\', '/');
        }

        return absolutePath.Replace('\\', '/');
    }
}
#endif
