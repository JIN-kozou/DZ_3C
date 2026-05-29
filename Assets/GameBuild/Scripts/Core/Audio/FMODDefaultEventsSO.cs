using UnityEngine;

/// <summary>
/// Default FMOD event references used when per-component fields are left empty.
/// </summary>
[CreateAssetMenu(fileName = "FMODDefaultEvents", menuName = "Audio/FMOD Default Events")]
public class FMODDefaultEventsSO : ScriptableObject
{
    private const string ResourcePath = "Config/Audio/FMODDefaultEvents";

    [Header("Weapons")]
    public FMODSoundEvent gunShoot;
    public FMODSoundEvent gunHit;
    public FMODSoundEvent gunBulletFly;

    [Header("Enemy")]
    public FMODSoundEvent enemyLaser;
    public FMODSoundEvent enemyHitReaction;
    public FMODSoundEvent enemySpawn;
    public FMODSoundEvent enemyTargetHowl;
    public FMODSoundEvent enemyPatrolSonar;
    public FMODSoundEvent enemyPatrolBreathing;

    [Header("Reverse")]
    public FMODSoundEvent reversePlace;
    public FMODSoundEvent reverseStartup;
    public FMODSoundEvent reverseRecall;

    [Header("Environment")]
    public FMODSoundEvent envVentGas;
    public FMODSoundEvent envSpark;
    public FMODSoundEvent envGateGas;
    public FMODSoundEvent envMachineryLoop;

    [Header("Projectile")]
    public FMODSoundEvent projectileSpawn;
    public FMODSoundEvent projectileFlight;
    public FMODSoundEvent projectileHit;
    public FMODSoundEvent projectileDespawn;

    private static FMODDefaultEventsSO cached;

    public static FMODDefaultEventsSO Instance
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<FMODDefaultEventsSO>(ResourcePath);
            }

            return cached;
        }
    }
}
