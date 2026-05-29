using UnityEngine;

public class ReverseArrayAudio : MonoBehaviour
{
    [Header("One-Shot SFX")]
    [SerializeField] private FMODSoundEvent placeReverseArray;
    [SerializeField] private FMODSoundEvent startupEnergyActivation;
    [SerializeField] private FMODSoundEvent recallReverseArray;
    [SerializeField] private FMODSoundEvent coreDepleted;

    [Header("Looping SFX")]
    [SerializeField] private FMODSoundEvent warningLoop;
    [SerializeField] private FMODSoundEvent coreChargeLoop;

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

    private FMODLoopHandle warningHandle;
    private FMODLoopHandle coreChargeHandle;
    private bool warningNoiseActive;

    private void Awake()
    {
        LoadDefaultEventsIfNeeded();
    }

    public void PlayPlace() { PlayOneShot(placeReverseArray); EmitAINoise(placeNoiseLoudness, placeNoiseDuration); }
    public void PlayStartup() { PlayOneShot(startupEnergyActivation); EmitAINoise(startupNoiseLoudness, startupNoiseDuration); }
    public void PlayRecall() { PlayOneShot(recallReverseArray); EmitAINoise(recallNoiseLoudness, recallNoiseDuration); }
    public void PlayCoreDepleted() => PlayOneShot(coreDepleted);
    public void StartCoreCharge() => coreChargeHandle = StartLoop(coreChargeLoop, coreChargeHandle);
    public void StopCoreCharge() => StopLoop(ref coreChargeHandle);
    public void PlayWarningPulse()
    {
        StartWarning();
        CancelInvoke(nameof(StopWarning));
        Invoke(nameof(StopWarning), warningPulseSeconds);
    }

    public void StartWarning()
    {
        warningHandle = StartLoop(warningLoop, warningHandle);
        if (warningEmitsContinuousNoise)
        {
            warningNoiseActive = true;
            StartAINoise(warningNoiseLoudness);
        }
    }

    public void StopWarning()
    {
        StopLoop(ref warningHandle);
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

    private void PlayOneShot(FMODSoundEvent sound)
    {
        if (sound != null)
        {
            GameAudio.Play3D(sound, transform.position, transform);
        }
    }

    private FMODLoopHandle StartLoop(FMODSoundEvent sound, FMODLoopHandle current)
    {
        if (current != null && current.IsPlaying)
        {
            return current;
        }

        return sound != null ? GameAudio.StartLoop3D(sound, transform.position, transform) : null;
    }

    private void StopLoop(ref FMODLoopHandle handle)
    {
        if (handle != null)
        {
            GameAudio.Stop(handle);
            handle = null;
        }
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

    private void LoadDefaultEventsIfNeeded()
    {
        FMODDefaultEventsSO defaults = FMODDefaultEventsSO.Instance;
        if (defaults == null)
        {
            return;
        }

        if (placeReverseArray == null) placeReverseArray = defaults.reversePlace;
        if (startupEnergyActivation == null) startupEnergyActivation = defaults.reverseStartup;
        if (recallReverseArray == null) recallReverseArray = defaults.reverseRecall;
    }
}
