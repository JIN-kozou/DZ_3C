#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Validates FMOD + Steam Audio integration and opens setup documentation.
/// </summary>
public static class GameBuildAudioPackageInstaller
{
    private const string InstallDoc = "Assets/Plugins/Audio/INSTALL.md";

    [MenuItem("GameBuild/Audio/Validate FMOD + Steam Audio Setup")]
    public static void ValidateSetup()
    {
        bool hasFmod = System.Type.GetType("FMODUnity.RuntimeManager, FMODUnity") != null;
        bool hasSteam = SteamAudioTypeResolver.IsInstalled;

        string message = "FMOD Unity: " + (hasFmod ? "OK" : "MISSING") + "\n" +
                         "Steam Audio: " + (hasSteam ? "OK" : "MISSING") + "\n\n" +
                         "See " + InstallDoc + " for import steps.";

        EditorUtility.DisplayDialog("Audio Package Validation", message, "OK");
        Debug.Log("[GameBuild Audio] " + message.Replace("\n", " | "));
    }

    [MenuItem("GameBuild/Audio/Open Install Documentation")]
    public static void OpenInstallDoc()
    {
        Object doc = AssetDatabase.LoadAssetAtPath<TextAsset>(InstallDoc);
        if (doc != null)
        {
            AssetDatabase.OpenAsset(doc);
            return;
        }

        Debug.LogWarning($"[GameBuild Audio] Missing {InstallDoc}");
    }
}
#endif
