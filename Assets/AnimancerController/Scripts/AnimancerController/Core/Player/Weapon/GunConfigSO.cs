using UnityEngine;

[CreateAssetMenu(menuName = "DZ_3C/Weapon/Gun Config", fileName = "GunConfig")]
public class GunConfigSO : ScriptableObject
{
    [Header("Damage / Fire")]
    public float damage = 10f;
    [Tooltip("每秒发射弹数（例如 8 = 每秒 8 发，间隔 0.125s）。不要填成「秒间隔」。")]
    [Min(0.01f)] public float fireRate = 8f;
    [Tooltip("勾选：按住开火键按射速连发。不勾选：点射，每次按下左键只会在该次按压的首帧尝试一发（按住不放不会连发）。")]
    public bool fullAuto = false;
    [Tooltip("单次 Update 内最多发射几发，防止卡帧/切窗后一次性补射过多；高射速全自动可适当调大。")]
    [Min(1)] public int maxShotsPerTick = 6;
    [Min(1)] public int magazineSize = 30;

    [Header("Projectile")]
    public Projectile bulletPrefab;
    [Tooltip("Viewport half-extents (0..0.5) for hipfire square spread around screen center.")]
    public Vector2 hipSpreadHalfSizeViewport = new Vector2(0.01f, 0.01f);

    [Header("ADS")]
    public float adsFieldOfView = 45f;
    public float adsCameraDistance = 1.2f;

    [Header("Viewkick (per shot, random in range)")]
    public Vector2 viewKickPitchDegRange = new Vector2(-0.8f, -0.2f);
    public Vector2 viewKickYawDegRange = new Vector2(-0.15f, 0.15f);
    public Vector2 viewKickRollDegRange = new Vector2(-0.2f, 0.2f);
    public Vector2 viewKickZOffsetRange = new Vector2(-0.08f, -0.02f);
    [Min(0.01f)] public float viewKickRecoveryHalfLife = 0.06f;

    [Header("Recoil pattern (viewport delta per shot vs previous shot center, cumulative)")]
    [Tooltip("Each element is added after a shot: new aim center = previous center + this delta (viewport 0..1 space, small values e.g. 0.002).")]
    public Vector2[] recoilPattern = { new Vector2(0f, 0.002f), new Vector2(0.0005f, 0.002f) };
    [Tooltip("后坐力骨架整体系数：每条 recoilPattern 的视口偏移会先乘该系数再累加（1=按表内数值）。")]
    [Min(0f)] public float recoilPatternGlobalScale = 1f;
    [Tooltip("相机上视口后坐力平滑追赶目标的时间（SmoothDamp，秒）。越小越接近瞬时跳变，越大越柔和。")]
    [Min(0.001f)] public float recoilAimSmoothTime = 0.07f;
    [Min(0f)] public float recoilRecoveryDelay = 0.08f;
    [Min(0.01f)] public float recoilRecoveryInterval = 0.04f;
    [Range(0f, 1f)] public float recoilRecoveryStep = 0.35f;

    [Header("Bullet physics (if prefab does not override)")]
    public float bulletMuzzleSpeed = 80f;
    public float bulletGravityScale = 1f;
    public float bulletLifetime = 6f;
}
