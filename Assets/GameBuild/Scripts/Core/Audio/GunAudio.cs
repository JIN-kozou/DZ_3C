using UnityEngine;

public class GunAudio : MonoBehaviour
{
    [SerializeField] private FMODSoundEvent draw;
    [SerializeField] private FMODSoundEvent holster;
    [SerializeField] private FMODSoundEvent shoot;
    [SerializeField] private FMODSoundEvent hit;
    [SerializeField] private FMODSoundEvent bulletFly;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float drawNoiseLoudness = 0.2f;
    [SerializeField, Min(0f)] private float drawNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float holsterNoiseLoudness = 0.2f;
    [SerializeField, Min(0f)] private float holsterNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float shootNoiseLoudness = 8f;
    [SerializeField, Min(0f)] private float shootNoiseDuration = 0.5f;

    private void Awake()
    {
        LoadDefaultEventsIfNeeded();
    }

    public void OnDraw()
    {
        PlayAtSelf(draw);
        EmitAINoise(drawNoiseLoudness, drawNoiseDuration);
    }

    public void OnHolster()
    {
        PlayAtSelf(holster);
        EmitAINoise(holsterNoiseLoudness, holsterNoiseDuration);
    }

    public void OnShoot()
    {
        PlayAtSelf(shoot);
        PlayAtSelf(bulletFly);
        EmitAINoise(shootNoiseLoudness, shootNoiseDuration);
    }

    public void OnHit(Vector3 hitPosition)
    {
        if (hit != null)
        {
            GameAudio.Play3D(hit, hitPosition);
        }
    }

    private void PlayAtSelf(FMODSoundEvent sound)
    {
        if (sound != null)
        {
            GameAudio.Play3D(sound, transform.position, transform);
        }
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }

    private void LoadDefaultEventsIfNeeded()
    {
        FMODDefaultEventsSO defaults = FMODDefaultEventsSO.Instance;
        if (defaults == null)
        {
            return;
        }

        if (shoot == null) shoot = defaults.gunShoot;
        if (hit == null) hit = defaults.gunHit;
        if (bulletFly == null) bulletFly = defaults.gunBulletFly;
    }
}
