using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Central FMOD playback API for gameplay and timeline audio.
/// </summary>
public static class GameAudio
{
    private const string SettingsResourcePath = "Config/Audio/GameAudioSettings";
    private static GameAudioSettingsSO settings;
    private static readonly Dictionary<FMODLoopHandle, Coroutine> fadeRoutines = new Dictionary<FMODLoopHandle, Coroutine>();
    private static CoroutineHost coroutineHost;

    public static GameAudioSettingsSO Settings => settings != null ? settings : (settings = Resources.Load<GameAudioSettingsSO>(SettingsResourcePath));

    public static void EnsureBanksLoaded()
    {
        if (Settings == null || !Settings.WaitForBanksOnBoot)
        {
            return;
        }

        RuntimeManager.WaitForAllSampleLoading();
    }

    public static bool Play3D(
        FMODSoundEvent sound,
        Vector3 position,
        Transform attach = null,
        float volumeMultiplier = 1f,
        float pitchMultiplier = 1f,
        float startTimeSeconds = 0f)
    {
        if (!TryCreateInstance(sound, out EventInstance instance))
        {
            return false;
        }

        Apply3D(instance, position, attach);
        ApplyPlaybackModifiers(instance, volumeMultiplier, pitchMultiplier, startTimeSeconds, Settings != null ? Settings.SfxVolume : 1f);
        instance.start();
        instance.release();
        return true;
    }

    public static bool Play2D(
        FMODSoundEvent sound,
        float volumeMultiplier = 1f,
        float pitchMultiplier = 1f,
        float startTimeSeconds = 0f)
    {
        if (!TryCreateInstance(sound, out EventInstance instance))
        {
            return false;
        }

        ApplyPlaybackModifiers(instance, volumeMultiplier, pitchMultiplier, startTimeSeconds, Settings != null ? Settings.MusicVolume : 1f);
        instance.start();
        instance.release();
        return true;
    }

    public static FMODLoopHandle StartLoop3D(
        FMODSoundEvent sound,
        Vector3 position,
        Transform attach = null,
        float volumeMultiplier = 1f,
        float pitchMultiplier = 1f)
    {
        FMODLoopHandle handle = new FMODLoopHandle();
        if (!TryCreateInstance(sound, out EventInstance instance))
        {
            return handle;
        }

        Apply3D(instance, position, attach);
        ApplyPlaybackModifiers(instance, volumeMultiplier, pitchMultiplier, 0f, Settings != null ? Settings.SfxVolume : 1f);
        instance.start();

        handle.Instance = instance;
        handle.IsValid = true;
        return handle;
    }

    public static FMODLoopHandle StartLoop2D(FMODSoundEvent sound, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        FMODLoopHandle handle = new FMODLoopHandle();
        if (!TryCreateInstance(sound, out EventInstance instance))
        {
            return handle;
        }

        ApplyPlaybackModifiers(instance, volumeMultiplier, pitchMultiplier, 0f, Settings != null ? Settings.MusicVolume : 1f);
        instance.start();

        handle.Instance = instance;
        handle.IsValid = true;
        return handle;
    }

    public static void Stop(FMODLoopHandle handle, bool allowFadeout = true)
    {
        if (handle == null || !handle.IsValid)
        {
            return;
        }

        if (fadeRoutines.TryGetValue(handle, out Coroutine routine) && routine != null && coroutineHost != null)
        {
            coroutineHost.StopCoroutine(routine);
            fadeRoutines.Remove(handle);
        }

        if (handle.Instance.isValid())
        {
            handle.Instance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            handle.Instance.release();
        }

        handle.IsValid = false;
    }

    public static void SetLoopVolume(FMODLoopHandle handle, float volume01)
    {
        handle?.SetVolume(volume01);
    }

    public static Coroutine FadeLoopVolume(FMODLoopHandle handle, float targetVolume, float duration, System.Action onComplete = null)
    {
        if (handle == null || !handle.IsValid)
        {
            onComplete?.Invoke();
            return null;
        }

        if (fadeRoutines.TryGetValue(handle, out Coroutine existing) && existing != null)
        {
            EnsureHost().StopCoroutine(existing);
        }

        Coroutine routine = EnsureHost().StartCoroutine(FadeLoopVolumeRoutine(handle, targetVolume, duration, onComplete));
        fadeRoutines[handle] = routine;
        return routine;
    }

    public static void SetMasterVolume(float volume01)
    {
        float v = Mathf.Clamp01(volume01);
        if (Settings != null)
        {
            // Settings asset is read-only at runtime; drive FMOD bus if present.
        }

        Bus masterBus;
        if (RuntimeManager.StudioSystem.getBus("bus:/", out masterBus) == FMOD.RESULT.OK)
        {
            masterBus.setVolume(v);
        }
    }

    private static bool TryCreateInstance(FMODSoundEvent sound, out EventInstance instance)
    {
        instance = default;
        if (sound == null || !sound.HasEvent)
        {
            return false;
        }

        try
        {
            instance = RuntimeManager.CreateInstance(sound.EventReference);
            return instance.isValid();
        }
        catch (EventNotFoundException ex)
        {
            Debug.LogWarning($"[GameAudio] FMOD event not found: {sound.EventPath}. Build banks in FMOD Studio. {ex.Message}");
            return false;
        }
    }

    private static void Apply3D(EventInstance instance, Vector3 position, Transform attach)
    {
        if (attach != null)
        {
            RuntimeManager.AttachInstanceToGameObject(instance, attach, attach.GetComponent<Rigidbody>());
            return;
        }

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
    }

    private static void ApplyPlaybackModifiers(
        EventInstance instance,
        float volumeMultiplier,
        float pitchMultiplier,
        float startTimeSeconds,
        float categoryVolume)
    {
        float master = Settings != null ? Settings.MasterVolume : 1f;
        instance.setVolume(Mathf.Clamp01(Mathf.Max(0f, volumeMultiplier) * categoryVolume * master));
        instance.setPitch(Mathf.Clamp(Mathf.Max(0.01f, pitchMultiplier), 0.01f, 100f));

        if (startTimeSeconds > 0f)
        {
            instance.setTimelinePosition(Mathf.RoundToInt(startTimeSeconds * 1000f));
        }
    }

    private static IEnumerator FadeLoopVolumeRoutine(FMODLoopHandle handle, float targetVolume, float duration, System.Action onComplete)
    {
        if (!handle.IsValid || !handle.Instance.isValid())
        {
            onComplete?.Invoke();
            yield break;
        }

        handle.Instance.getVolume(out float startVolume);
        targetVolume = Mathf.Clamp01(targetVolume);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            handle.SetVolume(targetVolume);
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && handle.IsValid && handle.Instance.isValid())
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            handle.SetVolume(Mathf.Lerp(startVolume, targetVolume, t));
            yield return null;
        }

        if (handle.IsValid && handle.Instance.isValid())
        {
            handle.SetVolume(targetVolume);
        }

        fadeRoutines.Remove(handle);
        onComplete?.Invoke();
    }

    private static CoroutineHost EnsureHost()
    {
        if (coroutineHost != null)
        {
            return coroutineHost;
        }

        GameObject hostObject = new GameObject(nameof(GameAudio));
        Object.DontDestroyOnLoad(hostObject);
        coroutineHost = hostObject.AddComponent<CoroutineHost>();
        return coroutineHost;
    }

    private sealed class CoroutineHost : MonoBehaviour
    {
    }
}
