using DZ_3C.AI.Core;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AIBlackboard blackboard;

    [Header("One-Shot SFX")]
    [SerializeField] private GameObject laserAttack;
    [SerializeField] private GameObject hitReaction;
    [SerializeField] private GameObject targetAcquisitionHowl;
    [SerializeField] private GameObject movement;
    [SerializeField] private GameObject spawn;
    [SerializeField] private GameObject patrolSonar;
    [SerializeField, Min(0f)] private float laserAttackStartTimeSeconds = 1.5f;

    [Header("Looping SFX")]
    [SerializeField] private GameObject patrolBreathingLoop;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float laserAttackNoiseLoudness = 3f;
    [SerializeField, Min(0f)] private float laserAttackNoiseDuration = 0.4f;
    [SerializeField, Min(0f)] private float hitReactionNoiseLoudness = 1f;
    [SerializeField, Min(0f)] private float hitReactionNoiseDuration = 0.25f;
    [SerializeField, Min(0f)] private float targetAcquisitionNoiseLoudness = 3f;
    [SerializeField, Min(0f)] private float targetAcquisitionNoiseDuration = 0.8f;
    [SerializeField, Min(0f)] private float patrolSonarNoiseLoudness = 1f;
    [SerializeField, Min(0f)] private float patrolSonarNoiseDuration = 0.25f;
    [SerializeField, Min(0f)] private float movementNoiseLoudness = 0.4f;
    [SerializeField, Min(0f)] private float movementNoiseDuration = 0.15f;
    [SerializeField, Min(0f)] private float spawnNoiseLoudness = 2f;
    [SerializeField, Min(0f)] private float spawnNoiseDuration = 0.6f;
    [SerializeField, Min(0f)] private float patrolBreathingNoiseLoudness = 0.3f;
    [SerializeField] private bool patrolBreathingEmitsContinuousNoise;

    private AudioSource patrolBreathingSource;
    private bool patrolBreathingNoiseActive;
    private bool blackboardSubscribed;

    private void Awake()
    {
        ResolveReferences();
        LoadDefaultPrefabsIfNeeded();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeBlackboard();

        if (blackboard == null || blackboard.HateTarget == null)
        {
            StartPatrolBreathing();
        }
    }

    private void OnDisable()
    {
        UnsubscribeBlackboard();
        StopAllLoops();
    }

    private void OnDestroy()
    {
        UnsubscribeBlackboard();
        StopAllLoops();
    }

    public void PlayLaserAttack() { PlayOneShot(laserAttack, laserAttackStartTimeSeconds); EmitAINoise(laserAttackNoiseLoudness, laserAttackNoiseDuration); }
    public void PlayHitReaction() { PlayOneShot(hitReaction); EmitAINoise(hitReactionNoiseLoudness, hitReactionNoiseDuration); }
    public void PlayTargetAcquisitionHowl() { PlayOneShot(targetAcquisitionHowl); EmitAINoise(targetAcquisitionNoiseLoudness, targetAcquisitionNoiseDuration); }
    public void PlayPatrolSonar() { PlayOneShot(patrolSonar); EmitAINoise(patrolSonarNoiseLoudness, patrolSonarNoiseDuration); }
    public void PlayMovement() { PlayOneShot(movement); EmitAINoise(movementNoiseLoudness, movementNoiseDuration); }
    public void PlaySpawn() { PlayOneShot(spawn); EmitAINoise(spawnNoiseLoudness, spawnNoiseDuration); }

    public void StartPatrolBreathing()
    {
        patrolBreathingSource = StartLoop(patrolBreathingLoop, patrolBreathingSource);
        if (patrolBreathingEmitsContinuousNoise)
        {
            patrolBreathingNoiseActive = true;
            StartAINoise(patrolBreathingNoiseLoudness);
        }
    }

    public void StopPatrolBreathing()
    {
        StopLoop(ref patrolBreathingSource);
        if (patrolBreathingNoiseActive)
        {
            patrolBreathingNoiseActive = false;
            StopAINoise();
        }
    }

    public void StopAllLoops() => StopPatrolBreathing();

    private void ResolveReferences()
    {
        if (blackboard == null)
        {
            blackboard = GetComponent<AIBlackboard>();
        }

        if (blackboard == null)
        {
            blackboard = GetComponentInParent<AIBlackboard>();
        }

        if (aiNoiseBridge == null)
        {
            aiNoiseBridge = GetComponent<AINoiseAudioBridge>();
        }

        if (aiNoiseBridge == null)
        {
            aiNoiseBridge = GetComponentInParent<AINoiseAudioBridge>();
        }
    }

    private void SubscribeBlackboard()
    {
        if (blackboard == null || blackboardSubscribed)
        {
            return;
        }

        blackboard.OnHateTargetChanged += HandleHateTargetChanged;
        blackboardSubscribed = true;
    }

    private void UnsubscribeBlackboard()
    {
        if (blackboard == null || !blackboardSubscribed)
        {
            return;
        }

        blackboard.OnHateTargetChanged -= HandleHateTargetChanged;
        blackboardSubscribed = false;
    }

    private void HandleHateTargetChanged(AITargetable target)
    {
        if (target != null)
        {
            StopPatrolBreathing();
            PlayTargetAcquisitionHowl();
            return;
        }

        StartPatrolBreathing();
    }

    private void PlayOneShot(GameObject soundPrefab, float startTimeSeconds = 0f)
    {
        AudioPrefabPlayer.Play(soundPrefab, transform.position, null, false, 1f, 1f, startTimeSeconds);
    }

    private AudioSource StartLoop(GameObject soundPrefab, AudioSource currentSource)
    {
        if (currentSource != null && currentSource.isPlaying)
        {
            return currentSource;
        }

        return AudioPrefabPlayer.Play(soundPrefab, transform.position, transform, true);
    }

    private void StopLoop(ref AudioSource source)
    {
        AudioPrefabPlayer.Stop(source);
        source = null;
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }

    private void StartAINoise(float loudness)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.StartContinuousNoise(loudness);
        }
    }

    private void StopAINoise()
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.StopContinuousNoise();
        }
    }

    private void LoadDefaultPrefabsIfNeeded()
    {
        if (laserAttack == null)
        {
            laserAttack = AudioDefaultPrefabs.Load("Assets/AnimancerController/AudioPrefabs/Enemy/lazer.prefab");
        }

        if (hitReaction == null)
        {
            hitReaction = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolyBulletExplosionSND.prefab");
        }

        if (spawn == null)
        {
            spawn = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolyShadowExplosionSND.prefab");
        }

        if (targetAcquisitionHowl == null)
        {
            targetAcquisitionHowl = AudioDefaultPrefabs.Load("Assets/AnimancerController/AudioPrefabs/Enemy/targetAcquisitionHowl.prefab");
        }

        if (patrolSonar == null)
        {
            patrolSonar = AudioDefaultPrefabs.Load("Assets/AnimancerController/AudioPrefabs/Enemy/sonar.prefab");
        }

        if (patrolBreathingLoop == null)
        {
            patrolBreathingLoop = AudioDefaultPrefabs.Load("Assets/AnimancerController/AudioPrefabs/Enemy/enemypatrol.prefab");
        }
    }
}
