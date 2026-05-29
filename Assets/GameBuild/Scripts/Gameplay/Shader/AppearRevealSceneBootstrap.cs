using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures every Appear ghost <see cref="Renderer"/> gets <see cref="AppearRevealDualRenderer"/> at runtime
/// (scenes rarely have the component pre-placed).
/// </summary>
public static class AppearRevealSceneBootstrap
{
    static readonly string[] DefaultExcludes = { "Glass", "glass" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        if (!Application.isPlaying)
            return;

        for (int s = 0; s < SceneManager.sceneCount; s++)
            EnsureDualRenderersInScene(SceneManager.GetSceneAt(s));
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying)
            return;

        EnsureDualRenderersInScene(scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void EnsureDualRenderersInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            EnsureDualRenderersUnder(roots[i]);
    }

    public static void EnsureDualRenderersUnder(GameObject root)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            AppearRevealDualRenderer.EnsureOnRenderer(renderers[i], DefaultExcludes);
    }
}
