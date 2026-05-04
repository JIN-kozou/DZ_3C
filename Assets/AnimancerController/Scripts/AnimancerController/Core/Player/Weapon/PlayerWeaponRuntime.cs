using UnityEngine;

/// <summary>
/// Weapon firing, ADS state, recoil pattern, and view kick. Tick from <see cref="Player.Update"/> when allowed.
/// </summary>
public class PlayerWeaponRuntime : MonoBehaviour
{
    [SerializeField] private GunConfigSO gunConfig;
    [SerializeField] private WeaponViewKickRig viewKickRig;
    [Header("开火口（编辑器）")]
    [SerializeField, Tooltip("子弹从此 Transform 的世界坐标生成；飞行方向为相机「屏幕中心 + 后坐力/腰射散布」射线指向的远方。未指定时退化为相机射线起点前 0.5m。")]
    private Transform muzzleSocket;

    [SerializeField, Tooltip("可选。双手 IK 的 Target 建议指向此 Transform（与枪口独立）；仅用于场景/Prefab 配置参考，逻辑仍由 Animation Rigging 约束上绑定。")]
    private Transform gripSocket;

    public Transform GripSocket => gripSocket;

    private Player _player;
    private int _ammo;
    private Collider[] _ownerColliders;
    private float _nextFireTime = float.NegativeInfinity;
    private int _recoilShotIndex;
    private Vector2 _recoilScreenOffset;
    private Vector2 _recoilScreenOffsetSmoothed;
    private Vector2 _recoilSmoothVelocity;
    private float _timeSinceLastShot = 100f;
    private float _recoilRecoveryTimer;
    private bool _adsHeld;

    public GunConfigSO GunConfig => gunConfig;
    public bool IsAds => _adsHeld;
    public int CurrentAmmo => _ammo;

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
        if (_player == null || _player.ReusableData == null || _player.InputService == null)
        {
            return;
        }

        var rd = _player.ReusableData;
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

        _adsHeld = input.ADSHeld;

        bool presentationReady = _player.ArmedPresentation == null || _player.ArmedPresentation.IsUpperBodyReadyForWeapon;

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

        if (!wantFire)
        {
            SmoothRecoilVisual(deltaTime);
            return;
        }

        if (!presentationReady)
        {
            SmoothRecoilVisual(deltaTime);
            return;
        }

        float interval = GetSecondsPerShot(gunConfig.fireRate);
        if (gunConfig.fullAuto)
        {
            int cap = Mathf.Max(1, gunConfig.maxShotsPerTick);
            int firedThisTick = 0;
            while (_ammo > 0 && Time.time >= _nextFireTime && firedThisTick < cap)
            {
                if (!TryFireOneShot())
                {
                    break;
                }

                firedThisTick++;

                // NegativeInfinity + interval is still -Infinity (IEEE754); RefillMagazine resets to -Infinity, which would drain the whole mag in one frame.
                if (float.IsInfinity(_nextFireTime) || float.IsNaN(_nextFireTime))
                {
                    _nextFireTime = Time.time + interval;
                }
                else
                {
                    _nextFireTime += interval;
                }
            }
        }
        else if (_ammo > 0 && Time.time >= _nextFireTime && firePressedThisFrame)
        {
            if (TryFireOneShot())
            {
                if (float.IsInfinity(_nextFireTime) || float.IsNaN(_nextFireTime))
                {
                    _nextFireTime = Time.time + interval;
                }
                else
                {
                    _nextFireTime += interval;
                }
            }
        }

        SmoothRecoilVisual(deltaTime);
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
        _nextFireTime = float.NegativeInfinity;
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
        Vector3 aimDir = ray.direction.normalized;
        Vector3 spawnPos = muzzleSocket != null
            ? muzzleSocket.position
            : ray.origin + aimDir * 0.5f;
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
            gunConfig.bulletLifetime);

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
