using UnityEngine;

public class ProjectileAudio : MonoBehaviour
{
    [Header("Projectile SFX")]
    [SerializeField] private FMODSoundEvent spawnSound;
    [SerializeField] private FMODSoundEvent flightLoop;
    [SerializeField] private FMODSoundEvent hitSound;
    [SerializeField] private FMODSoundEvent despawnSound;

    [Header("Lifecycle")]
    [SerializeField] private bool playSpawnOnEnable;
    [SerializeField] private bool startFlightLoopOnEnable = true;
    [SerializeField] private bool stopFlightLoopOnDisable = true;
    [SerializeField] private bool stopFlightLoopOnDestroy = true;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField] private bool flightLoopEmitsContinuousAINoise;
    [SerializeField, Min(0f)] private float flightNoiseLoudness;
    [SerializeField, Min(0f)] private float hitNoiseLoudness;
    [SerializeField, Min(0f)] private float hitNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float spawnNoiseLoudness;
    [SerializeField, Min(0f)] private float spawnNoiseDuration = 0.1f;

    private FMODLoopHandle flightLoopHandle;
    private bool flightNoiseActive;

    private void Awake()
    {
        LoadDefaultEventsIfNeeded();
    }

    private void OnEnable()
    {
        if (playSpawnOnEnable)
        {
            PlaySpawn();
        }

        if (startFlightLoopOnEnable)
        {
            StartFlightLoop();
        }
    }

    private void OnDisable()
    {
        if (stopFlightLoopOnDisable)
        {
            StopFlightLoop();
        }
    }

    private void OnDestroy()
    {
        if (stopFlightLoopOnDestroy)
        {
            StopFlightLoop();
        }
    }

    public void PlaySpawn()
    {
        PlayOneShot(spawnSound, transform.position);
        EmitAINoise(spawnNoiseLoudness, spawnNoiseDuration);
    }

    public void StartFlightLoop()
    {
        if (flightLoopHandle != null && flightLoopHandle.IsPlaying)
        {
            return;
        }

        if (flightLoop != null)
        {
            flightLoopHandle = GameAudio.StartLoop3D(flightLoop, transform.position, transform);
        }

        if (flightLoopHandle != null && flightLoopHandle.IsValid && flightLoopEmitsContinuousAINoise)
        {
            StartFlightAINoise();
        }
    }

    public void StopFlightLoop()
    {
        if (flightLoopHandle != null)
        {
            GameAudio.Stop(flightLoopHandle);
            flightLoopHandle = null;
        }

        StopFlightAINoiseIfActive();
    }

    public void PlayHit() => PlayHit(transform.position);

    public void PlayHit(Vector3 hitPosition)
    {
        PlayOneShot(hitSound, hitPosition);
        EmitAINoise(hitNoiseLoudness, hitNoiseDuration);
    }

    public void PlayDespawn()
    {
        PlayOneShot(despawnSound, transform.position);
    }

    public void StopAllLoops() => StopFlightLoop();

    private void PlayOneShot(FMODSoundEvent sound, Vector3 position)
    {
        if (sound != null)
        {
            GameAudio.Play3D(sound, position);
        }
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }

    private void StartFlightAINoise()
    {
        if (aiNoiseBridge == null || flightNoiseLoudness <= 0f)
        {
            return;
        }

        aiNoiseBridge.StartContinuousNoise(flightNoiseLoudness);
        flightNoiseActive = true;
    }

    private void StopFlightAINoiseIfActive()
    {
        if (!flightNoiseActive)
        {
            return;
        }

        flightNoiseActive = false;
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.StopContinuousNoise();
        }
    }

    private void LoadDefaultEventsIfNeeded()
    {
        FMODDefaultEventsSO defaults = FMODDefaultEventsSO.Instance;
        if (defaults == null)
        {
            return;
        }

        if (spawnSound == null) spawnSound = defaults.projectileSpawn;
        if (flightLoop == null) flightLoop = defaults.projectileFlight;
        if (hitSound == null) hitSound = defaults.projectileHit;
        if (despawnSound == null) despawnSound = defaults.projectileDespawn;
    }
}
