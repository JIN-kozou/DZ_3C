using UnityEngine;

public class ReverseArrayAudio : MonoBehaviour
{
    [Header("One-Shot SFX")]
    [SerializeField] private GameObject placeReverseArray;
    [SerializeField] private GameObject startupEnergyActivation;
    [SerializeField] private GameObject recallReverseArray;
    [SerializeField] private GameObject coreDepleted;

    [Header("Looping SFX")]
    [SerializeField] private GameObject warningLoop;
    [SerializeField] private GameObject coreChargeLoop;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float placeNoiseLoudness = 2f;
    [SerializeField, Min(0f)] private float placeNoiseDuration = 0.5f;
    [SerializeField, Min(0f)] private float startupNoiseLoudness = 3f;
    [SerializeField, Min(0f)] private float startupNoiseDuration = 1f;
    [SerializeField, Min(0f)] private float recallNoiseLoudness = 2f;
    [SerializeField, Min(0f)] private float recallNoiseDuration = 0.5f;
    [SerializeField, Min(0f)] private float warningNoiseLoudness = 2f;
    [SerializeField] private bool warningEmitsContinuousNoise;
    [SerializeField, Min(0.05f)] private float warningPulseSeconds = 1.5f;

    private AudioSource warningSource;
    private AudioSource coreChargeSource;
    private bool warningNoiseActive;

    private void Awake()
    {
        if (placeReverseArray == null)
        {
            placeReverseArray = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyBlackHoleMissileSND.prefab");
        }

        if (startupEnergyActivation == null)
        {
            startupEnergyActivation = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyLightningMissileSND.prefab");
        }

        if (recallReverseArray == null)
        {
            recallReverseArray = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyStormMissileSND.prefab");
        }
    }

    public void PlayPlace() { PlayOneShot(placeReverseArray); EmitAINoise(placeNoiseLoudness, placeNoiseDuration); }
    public void PlayStartup() { PlayOneShot(startupEnergyActivation); EmitAINoise(startupNoiseLoudness, startupNoiseDuration); }
    public void PlayRecall() { PlayOneShot(recallReverseArray); EmitAINoise(recallNoiseLoudness, recallNoiseDuration); }
    public void PlayCoreDepleted() => PlayOneShot(coreDepleted);
    public void StartCoreCharge() => coreChargeSource = StartLoop(coreChargeLoop, coreChargeSource);
    public void StopCoreCharge() => StopLoop(ref coreChargeSource);
    public void PlayWarningPulse()
    {
        StartWarning();
        CancelInvoke(nameof(StopWarning));
        Invoke(nameof(StopWarning), warningPulseSeconds);
    }

    public void StartWarning()
    {
        warningSource = StartLoop(warningLoop, warningSource);
        if (warningEmitsContinuousNoise)
        {
            warningNoiseActive = true;
            StartAINoise(warningNoiseLoudness);
        }
    }

    public void StopWarning()
    {
        StopLoop(ref warningSource);
        if (warningNoiseActive)
        {
            warningNoiseActive = false;
            StopAINoise();
        }
    }

    public void StopAllLoops()
    {
        StopWarning();
        StopCoreCharge();
    }

    private void OnDisable() => StopAllLoops();
    private void OnDestroy() => StopAllLoops();
    private void PlayOneShot(GameObject soundPrefab) => AudioPrefabPlayer.Play(soundPrefab, transform.position);

    private AudioSource StartLoop(GameObject soundPrefab, AudioSource currentSource)
    {
        return currentSource != null && currentSource.isPlaying
            ? currentSource
            : AudioPrefabPlayer.Play(soundPrefab, transform.position, transform, true);
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
}
