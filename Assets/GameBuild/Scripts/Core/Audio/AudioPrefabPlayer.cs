using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Use GameAudio with FMODSoundEvent assets instead.")]
public static class AudioPrefabPlayer
{
    private sealed class CoroutineHost : MonoBehaviour
    {
    }

    private static readonly Dictionary<AudioSource, GameObject> spawnedRoots = new Dictionary<AudioSource, GameObject>();
    private static CoroutineHost host;

    public static AudioSource Play(GameObject soundPrefab, Vector3 position)
    {
        return Play(soundPrefab, position, null, false, 1f, 1f);
    }

    public static AudioSource Play(GameObject soundPrefab, Vector3 position, Transform parent, bool attachToParent, float volumeMultiplier = 1f, float pitchMultiplier = 1f, float startTimeSeconds = 0f)
    {
        if (soundPrefab == null)
        {
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(soundPrefab, position, Quaternion.identity);
        if (instance == null)
        {
            return null;
        }

        if (parent != null && attachToParent)
        {
            instance.transform.SetParent(parent);
        }

        AudioSource primarySource = instance.GetComponent<AudioSource>();
        if (primarySource == null)
        {
            primarySource = instance.GetComponentInChildren<AudioSource>(true);
        }

        if (primarySource == null)
        {
            UnityEngine.Object.Destroy(instance);
            return null;
        }

        AudioSource[] sources = instance.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            source.volume = Mathf.Clamp01(source.volume * Mathf.Max(0f, volumeMultiplier));
            source.pitch = Mathf.Max(0.01f, source.pitch * Mathf.Max(0.01f, pitchMultiplier));
            if (source.clip != null && source.clip.loadState == AudioDataLoadState.Unloaded)
            {
                source.clip.LoadAudioData();
            }

            if (source.clip != null && startTimeSeconds > 0f)
            {
                source.time = Mathf.Min(startTimeSeconds, Mathf.Max(0f, source.clip.length - 0.01f));
            }

            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        spawnedRoots[primarySource] = instance;

        if (!primarySource.loop)
        {
            EnsureHost().StartCoroutine(DestroyWhenFinished(primarySource, instance));
        }

        return primarySource;
    }

    public static void Stop(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        spawnedRoots.TryGetValue(source, out GameObject root);
        spawnedRoots.Remove(source);
        source.Stop();
        UnityEngine.Object.Destroy(root != null ? root : source.gameObject);
    }

    private static CoroutineHost EnsureHost()
    {
        if (host != null)
        {
            return host;
        }

        GameObject hostObject = new GameObject(nameof(AudioPrefabPlayer));
        UnityEngine.Object.DontDestroyOnLoad(hostObject);
        host = hostObject.AddComponent<CoroutineHost>();
        return host;
    }

    private static IEnumerator DestroyWhenFinished(AudioSource source, GameObject root)
    {
        while (source != null && source.isPlaying)
        {
            yield return null;
        }

        if (source != null)
        {
            spawnedRoots.Remove(source);
        }

        if (root != null)
        {
            UnityEngine.Object.Destroy(root);
        }
    }
}
