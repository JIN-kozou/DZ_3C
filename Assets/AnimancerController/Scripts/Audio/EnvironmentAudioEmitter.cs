using UnityEngine;

public class EnvironmentAudioEmitter : MonoBehaviour
{
    [Header("One-Shot P0 Sounds")]
    [SerializeField] private GameObject damagedVentGasSpray;
    [SerializeField] private GameObject metalDeformationCreak;
    [SerializeField] private GameObject electricWireSpark;
    [SerializeField] private GameObject spaceshipAlarm;
    [SerializeField] private GameObject safetyGateOpen;
    [SerializeField] private GameObject safetyGateGasRelease;

    [Header("Looping P0 / Ambient Sounds")]
    [SerializeField] private GameObject shipMachineryLoop;
    [SerializeField] private GameObject helmetRadiationNoiseLoop;
    [SerializeField] private GameObject giantMonsterLowGrowlLoop;
    [SerializeField] private GameObject tireRollingLoop;
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

    private AudioSource shipMachinerySource;
    private AudioSource helmetRadiationNoiseSource;
    private AudioSource giantMonsterLowGrowlSource;
    private AudioSource tireRollingSource;
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
        shipMachinerySource = StartLoop(shipMachineryLoop, shipMachinerySource);
        if (machineryEmitsContinuousNoise)
        {
            machineryNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopShipMachineryLoop()
    {
        StopLoop(ref shipMachinerySource);
        machineryNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartHelmetRadiationNoise()
    {
        helmetRadiationNoiseSource = StartLoop(helmetRadiationNoiseLoop, helmetRadiationNoiseSource);
        if (helmetRadiationEmitsContinuousNoise)
        {
            helmetRadiationNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopHelmetRadiationNoise()
    {
        StopLoop(ref helmetRadiationNoiseSource);
        helmetRadiationNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartGiantMonsterLowGrowl()
    {
        giantMonsterLowGrowlSource = StartLoop(giantMonsterLowGrowlLoop, giantMonsterLowGrowlSource);
        if (monsterGrowlEmitsContinuousNoise)
        {
            monsterGrowlNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopGiantMonsterLowGrowl()
    {
        StopLoop(ref giantMonsterLowGrowlSource);
        monsterGrowlNoiseActive = false;
        RefreshContinuousAINoise();
    }

    public void StartTireRolling()
    {
        tireRollingSource = StartLoop(tireRollingLoop, tireRollingSource);
        if (tireRollingEmitsContinuousNoise)
        {
            tireRollingNoiseActive = true;
            RefreshContinuousAINoise();
        }
    }

    public void StopTireRolling()
    {
        StopLoop(ref tireRollingSource);
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

    private void PlayOneShot(GameObject soundPrefab)
    {
        if (soundPrefab == null)
        {
            return;
        }

        AudioPrefabPlayer.Play(soundPrefab, transform.position);
    }

    private AudioSource StartLoop(GameObject soundPrefab, AudioSource currentSource)
    {
        if (currentSource != null && currentSource.isPlaying)
        {
            return currentSource;
        }

        if (soundPrefab == null)
        {
            return null;
        }

        return AudioPrefabPlayer.Play(soundPrefab, transform.position, transform, false);
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

    private void StopLoop(ref AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        AudioPrefabPlayer.Stop(source);

        source = null;
    }

    private void LoadDefaultSoundsIfNeeded()
    {
        if (damagedVentGasSpray == null)
        {
            damagedVentGasSpray = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolySmokeGrenadeExplosionSND.prefab");
        }

        if (electricWireSpark == null)
        {
            electricWireSpark = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyLightningMissileSND.prefab");
        }

        if (safetyGateGasRelease == null)
        {
            safetyGateGasRelease = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolySmokeGrenadeExplosionSND.prefab");
        }

        if (shipMachineryLoop == null)
        {
            shipMachineryLoop = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyLaserMissileSND.prefab");
        }
    }
}
