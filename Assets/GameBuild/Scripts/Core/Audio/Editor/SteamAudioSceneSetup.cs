#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class SteamAudioSceneSetup
{
    [MenuItem("GameBuild/Audio/Tag Static Geometry for Steam Audio")]
    public static void TagStaticGeometry()
    {
        Type geometryType = SteamAudioTypeResolver.Geometry;
        if (geometryType == null)
        {
            EditorUtility.DisplayDialog(
                "Steam Audio",
                "Steam Audio Unity package is not imported. Import steamaudio_unity first (see Assets/Plugins/SteamAudio/INSTALL.md).",
                "OK");
            return;
        }

        MeshRenderer[] renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
        int tagged = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.isStatic)
            {
                continue;
            }

            if (renderer.GetComponent(geometryType) != null)
            {
                continue;
            }

            if (renderer.GetComponentInParent<Player>() != null)
            {
                continue;
            }

            Undo.AddComponent(renderer.gameObject, geometryType);
            tagged++;
        }

        Debug.Log($"[Steam Audio] Tagged {tagged} static mesh objects with SteamAudioGeometry.");
    }

    [MenuItem("GameBuild/Audio/Add Steam Audio Manager To Scene")]
    public static void AddManagerToScene()
    {
        Type managerType = SteamAudioTypeResolver.Manager;
        if (managerType == null)
        {
            EditorUtility.DisplayDialog("Steam Audio", "Steam Audio package not found.", "OK");
            return;
        }

        if (UnityEngine.Object.FindObjectOfType(managerType) != null)
        {
            Debug.Log("[Steam Audio] Manager already exists in scene.");
            return;
        }

        GameObject managerObject = new GameObject("SteamAudioManager");
        Undo.RegisterCreatedObjectUndo(managerObject, "Add Steam Audio Manager");
        managerObject.AddComponent(managerType);
        Selection.activeGameObject = managerObject;
    }
}
#endif
