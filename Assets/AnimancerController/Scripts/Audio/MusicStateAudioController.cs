using System.Collections;
using DZ_3C.AI.Core;
using UnityEngine;

public class MusicStateAudioController : MonoBehaviour
{
    public enum MusicState
    {
        None,
        Exploration,
        Combat
    }

    [SerializeField] private GameObject explorationMusic;
    [SerializeField] private GameObject combatMusic;
    [SerializeField, Min(0f)] private float defaultFadeDuration = 1f;
    [SerializeField] private bool playExplorationOnStart;
    [SerializeField] private bool restartSameState;
    [SerializeField] private bool autoFollowCombatState = true;
    [SerializeField, Min(0f)] private float combatMusicHoldSeconds = 2f;
    [SerializeField, Min(0.1f)] private float combatScanIntervalSeconds = 0.5f;

    private MusicState currentState = MusicState.None;
    private AudioSource currentMusicSource;
    private Coroutine musicRoutine;
    private AIBlackboard[] cachedBlackboards;
    private float nextCombatScanTime;
    private float combatMusicUntil;

    public MusicState CurrentState => currentState;

    private void Start()
    {
        if (playExplorationOnStart || autoFollowCombatState)
        {
            PlayExploration();
        }
    }

    private void Update()
    {
        if (autoFollowCombatState)
        {
            TickAutoCombatMusic();
        }
    }

    public void PlayExploration() => SetMusicState(MusicState.Exploration);
    public void PlayCombat() => SetMusicState(MusicState.Combat);

    public void SetMusicState(MusicState state)
    {
        if (state == MusicState.None)
        {
            StopMusic();
            return;
        }

        if (currentState == state && !restartSameState)
        {
            return;
        }

        GameObject music = GetMusicForState(state);
        if (music == null)
        {
            return;
        }

        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
        }

        musicRoutine = StartCoroutine(PlayMusicRoutine(music, defaultFadeDuration));
        currentState = state;
    }

    public void StopMusic() => StopMusic(defaultFadeDuration);

    public void StopMusic(float fadeDuration)
    {
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
        }

        musicRoutine = StartCoroutine(StopMusicRoutine(Mathf.Max(0f, fadeDuration)));
        currentState = MusicState.None;
    }

    private void OnDestroy()
    {
        AudioPrefabPlayer.Stop(currentMusicSource);
        currentMusicSource = null;
    }

    private GameObject GetMusicForState(MusicState state)
    {
        switch (state)
        {
            case MusicState.Exploration:
                return explorationMusic;
            case MusicState.Combat:
                return combatMusic;
            default:
                return null;
        }
    }

    private void TickAutoCombatMusic()
    {
        if (Time.time >= nextCombatScanTime)
        {
            nextCombatScanTime = Time.time + combatScanIntervalSeconds;
            if (AnyPlayerIsHateTarget())
            {
                combatMusicUntil = Time.time + combatMusicHoldSeconds;
            }
        }

        if (Time.time < combatMusicUntil)
        {
            PlayCombat();
        }
        else
        {
            PlayExploration();
        }
    }

    private bool AnyPlayerIsHateTarget()
    {
        if (cachedBlackboards == null || cachedBlackboards.Length == 0)
        {
            cachedBlackboards = FindObjectsOfType<AIBlackboard>();
        }

        bool sawLiveBlackboard = false;
        for (int i = 0; i < cachedBlackboards.Length; i++)
        {
            AIBlackboard blackboard = cachedBlackboards[i];
            if (blackboard == null)
            {
                continue;
            }

            sawLiveBlackboard = true;
            AITargetable target = blackboard.HateTarget;
            if (target != null && target.IsPlayer)
            {
                return true;
            }
        }

        if (!sawLiveBlackboard)
        {
            cachedBlackboards = FindObjectsOfType<AIBlackboard>();
        }

        return false;
    }

    private IEnumerator PlayMusicRoutine(GameObject music, float fadeDuration)
    {
        float duration = Mathf.Max(0f, fadeDuration);
        if (currentMusicSource != null)
        {
            if (duration > 0f)
            {
                yield return FadeVolume(currentMusicSource, 0f, duration);
            }

            AudioPrefabPlayer.Stop(currentMusicSource);
            currentMusicSource = null;
        }

        currentMusicSource = AudioPrefabPlayer.Play(music, transform.position, transform, true);
        if (currentMusicSource == null)
        {
            musicRoutine = null;
            yield break;
        }

        float targetVolume = currentMusicSource.volume;
        if (duration > 0f)
        {
            currentMusicSource.volume = 0f;
            yield return FadeVolume(currentMusicSource, targetVolume, duration);
        }

        musicRoutine = null;
    }

    private IEnumerator StopMusicRoutine(float fadeDuration)
    {
        AudioSource source = currentMusicSource;
        if (source != null)
        {
            if (fadeDuration > 0f)
            {
                yield return FadeVolume(source, 0f, fadeDuration);
            }

            AudioPrefabPlayer.Stop(source);
        }

        if (currentMusicSource == source)
        {
            currentMusicSource = null;
        }

        musicRoutine = null;
    }

    private static IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;
        while (source != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (source != null)
        {
            source.volume = targetVolume;
        }
    }
}
