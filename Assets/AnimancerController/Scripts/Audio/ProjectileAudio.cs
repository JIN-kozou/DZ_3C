using UnityEngine;

public class ProjectileAudio : MonoBehaviour
{
    [Header("Projectile SFX")]
    [SerializeField] private GameObject spawnSound;
    [SerializeField] private GameObject flightLoop;
    [SerializeField] private GameObject hitSound;
    [SerializeField] private GameObject despawnSound;

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

    private AudioSource flightLoopSource;
    private bool flightNoiseActive;

    private void Awake()
    {
        if (spawnSound == null)
        {
            spawnSound = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyBlackHoleMissileSND.prefab");
        }

        if (flightLoop == null)
        {
            flightLoop = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyLaserMissileSND.prefab");
        }

        if (hitSound == null)
        {
            hitSound = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolyBulletExplosionSND.prefab");
        }

        if (despawnSound == null)
        {
            despawnSound = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolySmokeGrenadeExplosionSND.prefab");
        }
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

    private void Update()
    {
        if (flightLoopSource == null)
        {
            return;
        }

        if (!flightLoopSource.isPlaying)
        {
            flightLoopSource = null;
            StopFlightAINoiseIfActive();
            return;
        }

        flightLoopSource.transform.position = transform.position;
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
        if (flightLoopSource != null && flightLoopSource.isPlaying)
        {
            return;
        }

        flightLoopSource = AudioPrefabPlayer.Play(flightLoop, transform.position, transform, true);
        if (flightLoopSource != null && flightLoopEmitsContinuousAINoise)
        {
            StartFlightAINoise();
        }
    }

    public void StopFlightLoop()
    {
        AudioPrefabPlayer.Stop(flightLoopSource);
        flightLoopSource = null;
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
    private void PlayOneShot(GameObject soundPrefab, Vector3 position) => AudioPrefabPlayer.Play(soundPrefab, position);

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
}
