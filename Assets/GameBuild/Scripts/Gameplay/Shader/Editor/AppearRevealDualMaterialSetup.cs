#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Batch-adds <see cref="AppearRevealDualRenderer"/> to renderers using AppearShader ghost materials.
/// </summary>
public static class AppearRevealDualMaterialSetup
{
    static readonly string[] DefaultExcludes = { "Glass", "glass" };

    [MenuItem("GameBuild/Appear/Setup Dual Reveal Materials")]
    public static void SetupDualRevealMaterials()
    {
        int addedComponents = 0;
        int dualizedRenderers = 0;
        int skipped = 0;
        var processed = new HashSet<int>();

        ProcessGameObjects(AssetDatabase.FindAssets("t:Prefab"), true, processed, ref addedComponents, ref dualizedRenderers, ref skipped);

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
                ProcessGameObject(root, false, processed, ref addedComponents, ref dualizedRenderers, ref skipped);
        }

        if (Application.isPlaying)
        {
            for (int s = 0; s < SceneManager.sceneCount; s++)
                AppearRevealSceneBootstrap.EnsureDualRenderersInScene(SceneManager.GetSceneAt(s));
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[Appear Dual Reveal] Components added: {addedComponents}, renderers updated: {dualizedRenderers}, skipped: {skipped}.");
    }

    static void ProcessGameObjects(string[] guids, bool isPrefab, HashSet<int> processed, ref int added, ref int dualized, ref int skipped)
    {
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject go = isPrefab
                ? AssetDatabase.LoadAssetAtPath<GameObject>(path)
                : null;
            if (go != null)
                ProcessGameObject(go, isPrefab, processed, ref added, ref dualized, ref skipped);
        }
    }

    static void ProcessGameObject(GameObject root, bool isPrefab, HashSet<int> processed, ref int added, ref int dualized, ref int skipped)
    {
        if (root == null)
            return;

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null)
                continue;

            int id = r.GetInstanceID();
            if (processed.Contains(id))
                continue;

            if (!RendererUsesAppearGhost(r))
                continue;

            if (r is not MeshRenderer and not SkinnedMeshRenderer)
            {
                skipped++;
                processed.Add(id);
                continue;
            }

            if (r.GetComponent<AppearRevealDualRenderer>() == null)
            {
                Undo.AddComponent<AppearRevealDualRenderer>(r.gameObject);
                added++;
            }

            if (isPrefab)
                EditorUtility.SetDirty(r.gameObject);

            dualized++;
            processed.Add(id);
        }
    }

    static bool RendererUsesAppearGhost(Renderer r)
    {
        Material[] mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (AppearRevealDualRenderer.IsAppearGhostMaterial(mats[i], DefaultExcludes))
                return true;
        }

        return false;
    }
}
#endif
