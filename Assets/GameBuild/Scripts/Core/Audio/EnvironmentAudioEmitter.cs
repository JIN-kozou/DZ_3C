using UnityEngine;

public class EnvironmentAudioEmitter : MonoBehaviour
{
    [Header("One-Shot P0 Sounds")]
    [SerializeField] private FMODSoundEvent damagedVentGasSpray;
    [SerializeField] private FMODSoundEvent metalDeformationCreak;
    [SerializeField] private FMODSoundEvent electricWireSpark;
    [SerializeField] private FMODSoundEvent spaceshipAlarm;
    [SerializeField] private FMODSoundEvent safetyGateOpen;
    [SerializeField] private FMODSoundEvent safetyGateGasRelease;

    [Header("Looping P0 / Ambient Sounds")]
    [SerializeField] private FMODSoundEvent shipMachineryLoop;
    [SerializeField] private FMODSoundEvent helmetRadiationNoiseLoop;
    [SerializeField] private FMODSoundEvent giantMonsterLowGrowlLoop;
    [SerializeField] private FMODSoundEvent tireRollingLoop;
    [SerializeField] private bool startAssignedLoopsOnEnable = true;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float ventNoiseLoudness = 1.5f;
    [SerializeField, Min(0f)] private float ventNoiseDuration = 0.5f;
    [SerializeField, Min(0f)] private float metalCreakNoiseLoudness = 1.2f;
    [SerializeField, Min(0f)] private float metalCreakNoiseDuration = 0.5f;
    [SerializeField, Min(0f)] private float sparkNoiseLoudness = 1f;
    [SerializeField, Min(0f)] private float sparkNoiseDuration = 0.25f;
    [SerializeField, Min(0f)] private float alarmNoiseLoudness = 2.5f;
    [SerializeField, Min(0f)] private float alarmNoiseDuration = 1f;
    [SerializeField, Min(0f)] private float machineryNoiseLoudness = 1f;
    [SerializeField] private bool machineryEmitsContinuousNoise;
    [SerializeField, Min(0f)] private float helmetRadiationNoiseLoudness = 0.2f;
    [SerializeField] private bool helmetRadiationEmitsContinuousNoise;
    [SerializeField, Min(0f)] private float monsterGrowlNoiseLoudness = 2f;
    [SerializeField] private bool monsterGrowlEmitsContinuousNoise;
    [SerializeField, Min(0f)] private float tireRollingNoiseLoudness = 0.8f;
    [SerializeField] private bool tireRollingEmitsContinuousNoise;

    private FMODLoopHandle shipMachineryHandle;
    private FMODLoopHandle helmetRadiationNoiseHandle;
    private FMODLoopHandle giantMonsterLowGrowlHandle;
    private FMODLoopHandle tireRollingHandle;
    private bool machineryNoiseActive;
    private bool helmetRadiationNoiseActive;
    private bool monsterGrowlNoiseActive;
    private bool tireRollingNoiseActive;

    private void Awake()
    {
        LoadDefaultSoundsIfNeeded();
    }

    private void OnEnable()
    {
        if (!startAssignedLoopsOnEnable)
        {
            return;
        }

        if (shipMachineryLoop != null)
        {
            StartShipMachineryLoop();
        }

        if (helmetRadiationNoiseLoop != null)
        {
            StartHelmetRadiationNoise();
        }

        if (giantMonsterLowGrowlLoop != null)
        {
            StartGiantMonsterLowGrowl();
        }

        if (tireRollingLoop != null)
        {
            StartTireRolling();
        }
    }

    public void PlayDamagedVentGasSpray()
    {
        PlayOneShot(damagedVentGasSpray);
        EmitAINoise(ventNoiseLoudness, ventNoiseDuration);
    }

    public void PlayMetalDeformationCreak()
    {
        PlayOneShot(metalDeformationCreak);
        EmitAINoise(metalCreakNoiseLoudness, metalCreakNoiseDuration);
    }

    public void PlayElectricWireSpark()
    {
        PlayOneShot(electricWireSpark);
        EmitAINoise(sparkNoiseLoudness, sparkNoiseDuration);
    }

    public void PlaySpaceshipAlarm()
    {
        PlayOneShot(spaceshipAlarm);
        EmitAINoise(alarmNoiseLoudness, alarmNoiseDuration);
    }

    public void PlaySafetyGateOpen()
    {
        PlayOneShot(safetyGateOpen);
    }

    public void PlaySafetyGateGasRelease()
    {
        PlayOneShot(safetyGateGasRelease);
    }

    public void StartShipMachineryLoop()
    {
        shipMachineryHandle = StartLoop(shipMachineryLoop, shipMachineryHandle);
        if (machineryEmitsContinuousNoise)
        {
            machineryNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopShipMachineryLoop()
    {
        StopLoop(ref shipMachineryHandle);
        machineryNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartHelmetRadiationNoise()
    {
        helmetRadiationNoiseHandle = StartLoop(helmetRadiationNoiseLoop, helmetRadiationNoiseHandle);
        if (helmetRadiationEmitsContinuousNoise)
        {
            helmetRadiationNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopHelmetRadiationNoise()
    {
        StopLoop(ref helmetRadiationNoiseHandle);
        helmetRadiationNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartGiantMonsterLowGrowl()
    {
        giantMonsterLowGrowlHandle = StartLoop(giantMonsterLowGrowlLoop, giantMonsterLowGrowlHandle);
        if (monsterGrowlEmitsContinuousNoise)
        {
            monsterGrowlNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopGiantMonsterLowGrowl()
    {
        StopLoop(ref giantMonsterLowGrowlHandle);
        monsterGrowlNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartTireRolling()
    {
        tireRollingHandle = StartLoop(tireRollingLoop, tireRollingHandle);
        if (tireRollingEmitsContinuousNoise)
        {
            tireRollingNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopTireRolling()
    {
        StopLoop(ref tireRollingHandle);
        tireRollingNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StopAllLoops()
    {
        StopShipMachineryLoop();
        StopHelmetRadiationNoise();
        StopGiantMonsterLowGrowl();
        StopTireRolling();
    }

    private void OnDisable()
    {
        StopAllLoops();
    }

    private void OnDestroy()
    {
        StopAllLoops();
    }

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

        if (sound == null)
        {
            return null;
        }

        return GameAudio.StartLoop3D(sound, transform.position, transform);
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge == null)
        {
            return;
        }

        aiNoiseBridge.EmitNoise(loudness, duration);
    }

    private void RefreshContinuousAINoise()
    {
        if (aiNoiseBridge == null)
        {
            return;
        }

        float loudestNoise = 0f;
        if (machineryNoiseActive)
        {
            loudestNoise = Mathf.Max(loudestNoise, machineryNoiseLoudness);
        }

        if (helmetRadiationNoiseActive)
        {
            loudestNoise = Mathf.Max(loudestNoise, helmetRadiationNoiseLoudness);
        }

        if (monsterGrowlNoiseActive)
        {
            loudestNoise = Mathf.Max(loudestNoise, monsterGrowlNoiseLoudness);
        }

        if (tireRollingNoiseActive)
        {
            loudestNoise = Mathf.Max(loudestNoise, tireRollingNoiseLoudness);
        }

        if (loudestNoise > 0f)
        {
            aiNoiseBridge.StartContinuousNoise(loudestNoise);
        }
        else
        {
            aiNoiseBridge.StopContinuousNoise();
        }
    }

    private void StopLoop(ref FMODLoopHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        GameAudio.Stop(handle);
        handle = null;
    }

    private void LoadDefaultSoundsIfNeeded()
    {
        FMODDefaultEventsSO defaults = FMODDefaultEventsSO.Instance;
        if (defaults == null)
        {
            return;
        }

        if (damagedVentGasSpray == null) damagedVentGasSpray = defaults.envVentGas;
        if (electricWireSpark == null) electricWireSpark = defaults.envSpark;
        if (safetyGateGasRelease == null) safetyGateGasRelease = defaults.envGateGas;
        if (shipMachineryLoop == null) shipMachineryLoop = defaults.envMachineryLoop;
    }
}
