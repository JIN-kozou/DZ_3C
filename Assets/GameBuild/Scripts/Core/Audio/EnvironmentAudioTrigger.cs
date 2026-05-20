using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentAudioTrigger : MonoBehaviour
{
    public enum EnvironmentSound
    {
        DamagedVentGasSpray,
        MetalDeformationCreak,
        ElectricWireSpark,
        SpaceshipAlarm,
        SafetyGateOpen,
        SafetyGateGasRelease,
        ShipMachineryLoop,
        HelmetRadiationNoiseLoop,
        GiantMonsterLowGrowlLoop,
        TireRollingLoop
    }

    [SerializeField] private EnvironmentAudioEmitter emitter;
    [SerializeField] private EnvironmentSound sound;
    [SerializeField] private bool playOnEnable;
    [SerializeField] private bool playOnTriggerEnter = true;
    [SerializeField] private bool playOnCollisionEnter;
    [SerializeField, Min(0f)] private float repeatIntervalSeconds;
    [SerializeField] private bool onlyPlayerCanTrigger;

    private float nextPlayTime;

    private void Awake()
    {
        ResolveEmitter();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }

        nextPlayTime = Time.time + repeatIntervalSeconds;
    }

    private void Update()
    {
        if (repeatIntervalSeconds <= 0f || Time.time < nextPlayTime)
        {
            return;
        }

        nextPlayTime = Time.time + repeatIntervalSeconds;
        Play();
    }

    private void OnDisable()
    {
        if (IsLoopSound(sound))
        {
            Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnTriggerEnter || !CanTrigger(other))
        {
            return;
        }

        Play();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!playOnCollisionEnter || collision == null || !CanTrigger(collision.collider))
        {
            return;
        }

        Play();
    }

    public void Play()
    {
        ResolveEmitter();
        if (emitter == null)
        {
            return;
        }

        switch (sound)
        {
            case EnvironmentSound.DamagedVentGasSpray:
                emitter.PlayDamagedVentGasSpray();
                break;
            case EnvironmentSound.MetalDeformationCreak:
                emitter.PlayMetalDeformationCreak();
                break;
            case EnvironmentSound.ElectricWireSpark:
                emitter.PlayElectricWireSpark();
                break;
            case EnvironmentSound.SpaceshipAlarm:
                emitter.PlaySpaceshipAlarm();
                break;
            case EnvironmentSound.SafetyGateOpen:
                emitter.PlaySafetyGateOpen();
                break;
            case EnvironmentSound.SafetyGateGasRelease:
                emitter.PlaySafetyGateGasRelease();
                break;
            case EnvironmentSound.ShipMachineryLoop:
                emitter.StartShipMachineryLoop();
                break;
            case EnvironmentSound.HelmetRadiationNoiseLoop:
                emitter.StartHelmetRadiationNoise();
                break;
            case EnvironmentSound.GiantMonsterLowGrowlLoop:
                emitter.StartGiantMonsterLowGrowl();
                break;
            case EnvironmentSound.TireRollingLoop:
                emitter.StartTireRolling();
                break;
        }
    }

    public void Stop()
    {
        ResolveEmitter();
        if (emitter == null)
        {
            return;
        }

        switch (sound)
        {
            case EnvironmentSound.ShipMachineryLoop:
                emitter.StopShipMachineryLoop();
                break;
            case EnvironmentSound.HelmetRadiationNoiseLoop:
                emitter.StopHelmetRadiationNoise();
                break;
            case EnvironmentSound.GiantMonsterLowGrowlLoop:
                emitter.StopGiantMonsterLowGrowl();
                break;
            case EnvironmentSound.TireRollingLoop:
                emitter.StopTireRolling();
                break;
        }
    }

    private void ResolveEmitter()
    {
        if (emitter != null)
        {
            return;
        }

        emitter = GetComponent<EnvironmentAudioEmitter>();
        if (emitter == null)
        {
            emitter = GetComponentInParent<EnvironmentAudioEmitter>();
        }
        if (emitter == null)
        {
            emitter = GetComponentInChildren<EnvironmentAudioEmitter>(true);
        }
    }

    private bool CanTrigger(Collider other)
    {
        if (!onlyPlayerCanTrigger)
        {
            return true;
        }

        return other != null && (other.GetComponent<Player>() != null || other.GetComponentInParent<Player>() != null);
    }

    private static bool IsLoopSound(EnvironmentSound value)
    {
        return value == EnvironmentSound.ShipMachineryLoop ||
               value == EnvironmentSound.HelmetRadiationNoiseLoop ||
               value == EnvironmentSound.GiantMonsterLowGrowlLoop ||
               value == EnvironmentSound.TireRollingLoop;
    }
}
