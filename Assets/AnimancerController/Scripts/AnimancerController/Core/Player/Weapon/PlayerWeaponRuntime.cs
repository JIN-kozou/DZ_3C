using DZ_3C.Reverse;
using UnityEngine;

/// <summary>
/// Weapon firing, ADS state, recoil pattern, and view kick. Tick from <see cref="Player.Update"/> when allowed.
/// </summary>
public class PlayerWeaponRuntime : MonoBehaviour
{
    [SerializeField] private GunConfigSO gunConfig;
    [SerializeField] private WeaponViewKickRig viewKickRig;
    [Header("开火口（编辑器）")]
    [SerializeField, Tooltip("子弹从此 Transform 的世界坐标生成；飞行方向为从该点指向「相机准星射线」上的参考远点（与纯相机 forward 相比更对准十字线）。未指定时退化为相机射线起点前 0.5m。")]
    private Transform muzzleSocket;

    [SerializeField, Min(10f), Tooltip("计算枪口→准星方向时，在相机准星射线上取的参考距离（米）。越大方向越接近与射线平行，一般 500～5000 即可。")]
    private float aimCrosshairReferenceDistance = 2000f;

    [SerializeField, Tooltip("可选。双手 IK 的 Target 建议指向此 Transform（与枪口独立）；仅用于场景/Prefab 配置参考，逻辑仍由 Animation Rigging 约束上绑定。")]
    private Transform gripSocket;

    public Transform GripSocket => gripSocket;

    private Player _player;
    private int _ammo;
    private Collider[] _ownerColliders;
    /// <summary>射速时钟：&lt;=0 表示允许击发；每帧减去 deltaTime。未想开火时不应累积负值「欠账」，否则半自动快速点击会快于配置射速。</summary>
    private float _fireCooldown;
    /// <summary>半自动待击发截止时间（<see cref="Time.unscaledTime"/>）；&lt;0 表示无缓冲。</summary>
    private float _semiBufferDeadlineUnscaled = -1f;
    private int _recoilShotIndex;
    private Vector2 _recoilScreenOffset;
    private Vector2 _recoilScreenOffsetSmoothed;
    private Vector2 _recoilSmoothVelocity;
    private float _timeSinceLastShot = 100f;
    private float _recoilRecoveryTimer;
    private bool _adsHeld;
    private float _ammoRegenAccumulator;
    private ReverseCoreStack _reverseCoreStack;

    public GunConfigSO GunConfig => gunConfig;
    public bool IsAds => _adsHeld;
    public int CurrentAmmo => _ammo;

    /// <summary>
    /// 0~1：当前这一发被动回弹的进度（未满弹且开启间隔回弹时）；满弹或未配置回弹为 0。
    /// UI 可用来画下一格将恢复的填充动画。
    /// </summary>
    public float AmmoRegenProgressNormalized
    {
        get
        {
            if (gunConfig == null || gunConfig.ammoRegenIntervalSeconds <= 0f)
            {
                return 0f;
            }

            if (_ammo >= gunConfig.magazineSize)
            {
                return 0f;
            }

            float interval = Mathf.Max(0.0001f, gunConfig.ammoRegenIntervalSeconds);
            return Mathf.Clamp01(_ammoRegenAccumulator / interval);
        }
    }

    /// <summary>本帧 <see cref="Tick"/> 内 <see cref="TryFireOneShot"/> 成功次数；在 <see cref="Tick"/> 末尾（开火逻辑之后）写入，供 ADS 开火动画等与真实击发对齐。</summary>
    public int SuccessfulShotsLastTick { get; private set; }

    /// <summary>Smoothed viewport offset for camera (eases toward recoil target each frame).</summary>
    public Vector2 AimViewportRecoilOffset => _recoilScreenOffsetSmoothed;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (gunConfig == null)
        {
            gunConfig = Resources.Load<GunConfigSO>("Config/Weapon/DefaultGun");
        }

        if (viewKickRig == null)
        {
            var cfc = FindObjectOfType<CrouchFovController>();
            if (cfc != null)
            {
                viewKickRig = cfc.GetComponent<WeaponViewKickRig>();
                if (viewKickRig == null)
                {
                    viewKickRig = cfc.gameObject.AddComponent<WeaponViewKickRig>();
                }
            }
        }

        RefreshOwnerColliders();
        RefillMagazine();
    }

    private void OnEnable()
    {
        _reverseCoreStack = GetComponent<ReverseCoreStack>();
        if (_reverseCoreStack != null)
        {
            _reverseCoreStack.OnRespawned += OnReverseRespawned;
        }
    }

    private void OnDisable()
    {
        if (_reverseCoreStack != null)
        {
            _reverseCoreStack.OnRespawned -= OnReverseRespawned;
            _reverseCoreStack = null;
        }
    }

    private void OnReverseRespawned(Vector3 _)
    {
        RefillMagazine();
    }

    private void RefreshOwnerColliders()
    {
        if (_player != null)
        {
            _ownerColliders = _player.GetComponentsInChildren<Collider>();
        }
    }

    public void Tick(float deltaTime)
    {
        int shotsThisTick = 0;
        try
        {
            if (_player == null || _player.ReusableData == null)
            {
                return;
            }

            var rd = _player.ReusableData;

            if (gunConfig != null)
            {
                TickAmmoRegen(deltaTime);
            }

            if (_player.InputService == null)
            {
                return;
            }

            var input = _player.InputService;

            if (gunConfig == null)
            {
                _adsHeld = false;
                return;
            }

            bool fireHeld = input.FireHeld;
            bool firePressedThisFrame = input.FireWasPressedThisFrame;
            bool wantFire = gunConfig.fullAuto ? fireHeld : firePressedThisFrame;

            if (!CanProcessWeapon(rd))
            {
                _adsHeld = false;
                if (!rd.armedModeActive)
                {
                    ResetRecoilState();
                }

                SmoothRecoilVisual(deltaTime);
                return;
            }

            bool adsFromInput = input.ADSHeld;
            if (_player.ArmedPresentation != null && !_player.ArmedPresentation.IsAdsInputAllowed)
            {
                adsFromInput = false;
            }

            _adsHeld = adsFromInput;

            bool presentationReady = _player.ArmedPresentation == null || _player.ArmedPresentation.IsWeaponFireAllowed;

            _timeSinceLastShot += deltaTime;
            if (_timeSinceLastShot > gunConfig.recoilRecoveryDelay)
            {
                _recoilRecoveryTimer += deltaTime;
                while (_recoilRecoveryTimer >= gunConfig.recoilRecoveryInterval && _recoilScreenOffset.sqrMagnitude > 1e-8f)
                {
                    _recoilRecoveryTimer -= gunConfig.recoilRecoveryInterval;
                    _recoilScreenOffset = Vector2.Lerp(_recoilScreenOffset, Vector2.zero, gunConfig.recoilRecoveryStep);
                }
            }

            float interval = GetSecondsPerShot(gunConfig.fireRate);
            _fireCooldown -= deltaTime;
            // 未按住/未点下开火时不要把冷却扣成大额负值；负值只应在全自动同帧补射循环内短暂出现。
            if (!wantFire && _fireCooldown < 0f)
            {
                _fireCooldown = 0f;
            }

            if (!wantFire)
            {
                SmoothRecoilVisual(deltaTime);
                return;
            }

            if (!presentationReady)
            {
                if (!gunConfig.fullAuto &&
                    firePressedThisFrame &&
                    gunConfig.semiAutoPressBufferSeconds > 0f)
                {
                    _semiBufferDeadlineUnscaled = Time.unscaledTime + gunConfig.semiAutoPressBufferSeconds;
                }

                SmoothRecoilVisual(deltaTime);
                return;
            }

            int maxCatchUp = Mathf.Max(1, gunConfig.maxCatchUpShotsPerTick);
            if (gunConfig.fullAuto)
            {
                int firedThisTick = 0;
                while (_ammo > 0 && firedThisTick < maxCatchUp && wantFire)
                {
                    if (_fireCooldown > 0f)
                    {
                        break;
                    }

                    if (!TryFireOneShot())
                    {
                        break;
                    }

                    shotsThisTick++;
                    firedThisTick++;
                    _fireCooldown += interval;
                }
            }
            else
            {
                bool buffered = gunConfig.semiAutoPressBufferSeconds > 0f &&
                                _semiBufferDeadlineUnscaled >= Time.unscaledTime;
                bool semiTrigger = firePressedThisFrame || buffered;
                if (semiTrigger && _ammo > 0 && _fireCooldown <= 0f)
                {
                    if (TryFireOneShot())
                    {
                        shotsThisTick++;
                        // 半自动：整格间隔，不沿用「欠账」；避免快速连点快于 fireRate。
                        _fireCooldown = interval;
                        _semiBufferDeadlineUnscaled = -1f;
                    }
                }
            }

            SmoothRecoilVisual(deltaTime);
        }
        finally
        {
            SuccessfulShotsLastTick = shotsThisTick;
        }
    }

    private void ResetRecoilState()
    {
        _recoilScreenOffset = Vector2.zero;
        _recoilScreenOffsetSmoothed = Vector2.zero;
        _recoilSmoothVelocity = Vector2.zero;
        _recoilShotIndex = 0;
    }

    private void SmoothRecoilVisual(float deltaTime)
    {
        if (gunConfig == null)
        {
            return;
        }

        float smoothTime = Mathf.Max(0.001f, gunConfig.recoilAimSmoothTime);
        _recoilScreenOffsetSmoothed = Vector2.SmoothDamp(
            _recoilScreenOffsetSmoothed,
            _recoilScreenOffset,
            ref _recoilSmoothVelocity,
            smoothTime,
            Mathf.Infinity,
            deltaTime);
    }

    /// <summary>
    /// Returns seconds between shots. <paramref name="fireRate"/> is rounds per second (RPS), minimum 0.01 RPS.
    /// </summary>
    private static float GetSecondsPerShot(float fireRate)
    {
        return 1f / Mathf.Max(0.01f, fireRate);
    }

    public void RefillMagazine()
    {
        if (gunConfig == null)
        {
            return;
        }

        _ammo = gunConfig.magazineSize;
        _fireCooldown = 0f;
        _semiBufferDeadlineUnscaled = -1f;
        _ammoRegenAccumulator = 0f;
    }

    private void TickAmmoRegen(float deltaTime)
    {
        if (gunConfig.ammoRegenIntervalSeconds <= 0f)
        {
            return;
        }

        int cap = gunConfig.magazineSize;
        if (_ammo >= cap)
        {
            _ammoRegenAccumulator = 0f;
            return;
        }

        float interval = gunConfig.ammoRegenIntervalSeconds;
        _ammoRegenAccumulator += deltaTime;
        while (_ammo < cap && _ammoRegenAccumulator >= interval)
        {
            _ammoRegenAccumulator -= interval;
            _ammo++;
        }
    }

    private bool CanProcessWeapon(PlayerReusableData rd)
    {
        if (gunConfig == null)
        {
            return false;
        }

        if (!rd.armedModeActive)
        {
            return false;
        }

        if (!rd.AllowsArmedWeaponActions())
        {
            return false;
        }

        var stateName = rd.currentState.Value;
        if (stateName != null && stateName.Contains("Climb"))
        {
            return false;
        }

        return true;
    }

    private static Camera ResolveFireCamera(Player player)
    {
        if (player.camTransform != null)
        {
            var cam = player.camTransform.GetComponent<Camera>();
            if (cam != null)
            {
                return cam;
            }

            cam = player.camTransform.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                return cam;
            }
        }

        return Camera.main;
    }

    /// <returns>False if shot could not be taken (no camera, etc.).</returns>
    private bool TryFireOneShot()
    {
        if (_ammo <= 0)
        {
            return false;
        }

        var cam = ResolveFireCamera(_player);
        if (cam == null)
        {
            return false;
        }

        Vector2 vp = new Vector2(0.5f, 0.5f) + _recoilScreenOffset;
        if (!_adsHeld && gunConfig.hipSpreadHalfSizeViewport.sqrMagnitude > 0f)
        {
            vp.x += Random.Range(-gunConfig.hipSpreadHalfSizeViewport.x, gunConfig.hipSpreadHalfSizeViewport.x);
            vp.y += Random.Range(-gunConfig.hipSpreadHalfSizeViewport.y, gunConfig.hipSpreadHalfSizeViewport.y);
        }

        var ray = cam.ViewportPointToRay(new Vector3(vp.x, vp.y, 0f));
        Vector3 spawnPos = muzzleSocket != null
            ? muzzleSocket.position
            : ray.origin + ray.direction.normalized * 0.5f;

        float refDist = Mathf.Max(10f, aimCrosshairReferenceDistance);
        Vector3 aimOnCrosshairRay = ray.GetPoint(refDist);
        Vector3 toCrosshair = aimOnCrosshairRay - spawnPos;
        Vector3 aimDir = toCrosshair.sqrMagnitude > 1e-8f
            ? toCrosshair.normalized
            : ray.direction.normalized;
        Quaternion spawnRot = Quaternion.LookRotation(aimDir);

        if (_ownerColliders == null || _ownerColliders.Length == 0)
        {
            RefreshOwnerColliders();
        }

        var ownerColliders = _ownerColliders;
        var proj = gunConfig.bulletPrefab != null
            ? Instantiate(gunConfig.bulletPrefab, spawnPos, spawnRot)
            : CreateRuntimeProjectile(spawnPos, spawnRot);
        proj.Launch(
            _player.transform,
            ownerColliders ?? System.Array.Empty<Collider>(),
            aimDir * gunConfig.bulletMuzzleSpeed,
            gunConfig.damage,
            gunConfig.bulletGravityScale,
            gunConfig.bulletLifetime,
            gunConfig.damageableTags,
            gunConfig.hurtBuffId,
            gunConfig.destroyOnHit,
            gunConfig.maxHitDistance,
            gunConfig.bulletVisualTeardownDelay);

        if (gunConfig.recoilPattern != null && gunConfig.recoilPattern.Length > 0)
        {
            float s = Mathf.Max(0f, gunConfig.recoilPatternGlobalScale);
            _recoilScreenOffset += gunConfig.recoilPattern[_recoilShotIndex % gunConfig.recoilPattern.Length] * s;
            _recoilShotIndex++;
        }

        ApplyViewKick();

        _ammo--;
        _timeSinceLastShot = 0f;
        _recoilRecoveryTimer = 0f;
        return true;
    }

    private void ApplyViewKick()
    {
        if (viewKickRig == null || gunConfig == null)
        {
            return;
        }

        float roll = Random.Range(gunConfig.viewKickRollDegRange.x, gunConfig.viewKickRollDegRange.y);
        float z = Random.Range(gunConfig.viewKickZOffsetRange.x, gunConfig.viewKickZOffsetRange.y);
        bool usePatternForAim = gunConfig.recoilPattern != null && gunConfig.recoilPattern.Length > 0;
        if (usePatternForAim)
        {
            viewKickRig.ApplyKick(new Vector3(0f, 0f, roll), z, gunConfig.viewKickRecoveryHalfLife);
            return;
        }

        float pitch = Random.Range(gunConfig.viewKickPitchDegRange.x, gunConfig.viewKickPitchDegRange.y);
        float yaw = Random.Range(gunConfig.viewKickYawDegRange.x, gunConfig.viewKickYawDegRange.y);
        viewKickRig.ApplyKick(new Vector3(pitch, yaw, roll), z, gunConfig.viewKickRecoveryHalfLife);
    }

    private static Projectile CreateRuntimeProjectile(Vector3 worldPosition, Quaternion worldRotation)
    {
        var go = new GameObject("RuntimeBullet");
        go.transform.SetPositionAndRotation(worldPosition, worldRotation);
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.05f;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.02f;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return go.AddComponent<Projectile>();
    }
}
