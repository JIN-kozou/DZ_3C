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

    [SerializeField] private FMODSoundEvent explorationMusic;
    [SerializeField] private FMODSoundEvent combatMusic;
    [SerializeField, Min(0f)] private float defaultFadeDuration = 1f;
    [SerializeField] private bool playExplorationOnStart;
    [SerializeField] private bool restartSameState;
    [SerializeField] private bool autoFollowCombatState = true;
    [SerializeField, Min(0f)] private float combatMusicHoldSeconds = 2f;
    [SerializeField, Min(0.1f)] private float combatScanIntervalSeconds = 0.5f;

    private MusicState currentState = MusicState.None;
    private FMODLoopHandle currentMusicHandle;
    private float currentMusicTargetVolume = 1f;
    private Coroutine musicRoutine;
    private AIBlackboard[] cachedBlackboards;
    private float nextCombatScanTime;
    private float combatMusicUntil;

    public MusicState CurrentState => currentState;

    private void Awake()
    {
        if (explorationMusic == null)
        {
            explorationMusic = Resources.Load<FMODSoundEvent>("Config/Audio/Events/Music_Exploration");
        }

        if (combatMusic == null)
        {
            combatMusic = Resources.Load<FMODSoundEvent>("Config/Audio/Events/Music_Combat");
        }
    }

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

        FMODSoundEvent music = GetMusicForState(state);
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
        if (currentMusicHandle != null)
        {
            GameAudio.Stop(currentMusicHandle);
            currentMusicHandle = null;
        }
    }

    private FMODSoundEvent GetMusicForState(MusicState state)
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

    private IEnumerator PlayMusicRoutine(FMODSoundEvent music, float fadeDuration)
    {
        float duration = Mathf.Max(0f, fadeDuration);
        if (currentMusicHandle != null && currentMusicHandle.IsValid)
        {
            if (duration > 0f)
            {
                yield return GameAudio.FadeLoopVolume(currentMusicHandle, 0f, duration);
            }

            GameAudio.Stop(currentMusicHandle);
            currentMusicHandle = null;
        }

        currentMusicHandle = GameAudio.StartLoop2D(music);
        if (currentMusicHandle == null || !currentMusicHandle.IsValid)
        {
            musicRoutine = null;
            yield break;
        }

        currentMusicTargetVolume = 1f;
        if (duration > 0f)
        {
            currentMusicHandle.SetVolume(0f);
            yield return GameAudio.FadeLoopVolume(currentMusicHandle, currentMusicTargetVolume, duration);
        }
        else
        {
            currentMusicHandle.SetVolume(currentMusicTargetVolume);
        }

        musicRoutine = null;
    }

    private IEnumerator StopMusicRoutine(float fadeDuration)
    {
        FMODLoopHandle handle = currentMusicHandle;
        if (handle != null && handle.IsValid)
        {
            if (fadeDuration > 0f)
            {
                yield return GameAudio.FadeLoopVolume(handle, 0f, fadeDuration);
            }

            GameAudio.Stop(handle);
        }

        if (currentMusicHandle == handle)
        {
            currentMusicHandle = null;
        }

        musicRoutine = null;
    }
}
