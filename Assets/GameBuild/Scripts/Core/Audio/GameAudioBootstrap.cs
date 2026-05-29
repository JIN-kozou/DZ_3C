using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures FMOD banks and Steam Audio manager exist when gameplay scenes load.
/// </summary>
[DefaultExecutionOrder(-500)]
public class GameAudioBootstrap : MonoBehaviour
{
    private static GameAudioBootstrap instance;

    [SerializeField] private GameAudioSettingsSO settings;
    [SerializeField] private bool createSteamAudioManager = true;
    [SerializeField] private bool ensureFmodListenerOnCamera = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null)
        {
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            return;
        }

        if (FindObjectOfType<GameAudioBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject(nameof(GameAudioBootstrap));
        bootstrapObject.AddComponent<GameAudioBootstrap>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (settings == null)
        {
            settings = Resources.Load<GameAudioSettingsSO>("Config/Audio/GameAudioSettings");
        }

        GameAudio.EnsureBanksLoaded();
        EnsureSteamAudioManager();
        EnsureListener();
    }

    private void EnsureSteamAudioManager()
    {
        if (!createSteamAudioManager)
        {
            return;
        }

        Type managerType = SteamAudioTypeResolver.Manager;
        if (managerType == null)
        {
            return;
        }

        if (FindObjectOfType(managerType) != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("SteamAudioManager");
        managerObject.transform.SetParent(transform);
        managerObject.AddComponent(managerType);
    }

    private void EnsureListener()
    {
        if (!ensureFmodListenerOnCamera)
        {
            return;
        }

        if (FindObjectOfType<FMODListenerFollower>() != null)
        {
            return;
        }

        Camera main = Camera.main;
        if (main == null)
        {
            return;
        }

        if (main.GetComponent<FMODListenerFollower>() == null)
        {
            main.gameObject.AddComponent<FMODListenerFollower>();
        }
    }
}
