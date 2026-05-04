using Cinemachine;
using UnityEngine;

/// <summary>
/// Changes Cinemachine FOV while crouching.
/// Attach this to the same object as CinemachineVirtualCamera.
/// Recomposer tilt/pan/dutch are written each frame; auto-created Recomposer must keep
/// Follow/LookAt attachment at 1 or Cinemachine will stop tracking the follow target.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
[DefaultExecutionOrder(-50)]
public class CrouchFovController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float crouchFov = 52f;
    [SerializeField] private float crouchDistance = 3f;
    [SerializeField] private float changeSpeed = 8f;
    [SerializeField] private float dizzyDistanceScale = 0.2f;

    private CinemachineVirtualCamera _virtualCamera;
    private float _standFov;
    private CameraController _cameraController;
    private float _dizzyTime;
    private CinemachineRecomposer _recomposer;
    private float _authoringRecomposerTilt;
    private float _authoringRecomposerPan;
    private float _authoringRecomposerDutch;
    private PlayerWeaponRuntime _weaponRuntime;
    private WeaponViewKickRig _weaponKick;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _standFov = _virtualCamera.m_Lens.FieldOfView;
        _cameraController = GetComponent<CameraController>();
        _recomposer = GetComponent<CinemachineRecomposer>();
        if (_recomposer == null)
        {
            _recomposer = gameObject.AddComponent<CinemachineRecomposer>();
            _recomposer.m_ApplyAfter = CinemachineCore.Stage.Finalize;
            _recomposer.m_ZoomScale = 1f;
            // AddComponent does not run Reset(): these default to 0 and break Follow/LookAt.
            _recomposer.m_FollowAttachment = 1f;
            _recomposer.m_LookAtAttachment = 1f;
        }

        if (_recomposer.m_FollowAttachment <= 0f)
        {
            _recomposer.m_FollowAttachment = 1f;
        }

        if (_recomposer.m_LookAtAttachment <= 0f)
        {
            _recomposer.m_LookAtAttachment = 1f;
        }

        _authoringRecomposerTilt = _recomposer.m_Tilt;
        _authoringRecomposerPan = _recomposer.m_Pan;
        _authoringRecomposerDutch = _recomposer.m_Dutch;

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        _weaponKick = GetComponent<WeaponViewKickRig>();
        if (_weaponKick == null)
        {
            _weaponKick = gameObject.AddComponent<WeaponViewKickRig>();
        }

        if (player != null)
        {
            ResolveWeaponRuntime();
        }
    }

    private void ResolveWeaponRuntime()
    {
        if (player == null)
        {
            return;
        }

        if (_weaponRuntime == null)
        {
            _weaponRuntime = player.WeaponRuntime;
        }

        if (_weaponRuntime == null)
        {
            _weaponRuntime = player.GetComponent<PlayerWeaponRuntime>();
        }
    }

    private void LateUpdate()
    {
        if (_virtualCamera == null || player == null || player.ReusableData == null)
        {
            return;
        }

        ResolveWeaponRuntime();

        // standValue: 1 = standing, 0 = crouching.
        float standValue = player.ReusableData.standValueParameter.CurrentValue;
        var buffSnapshot = player.ReusableData.buffSnapshot;
        bool isCrouching = standValue < 0.5f;
        _dizzyTime += Time.deltaTime * Mathf.Max(0f, buffSnapshot.dizzyFrequency);
        float dizzyWave = Mathf.Sin(_dizzyTime) * buffSnapshot.dizzyAmplitude;
        float dizzyFovOffset = dizzyWave + buffSnapshot.dizzyFovOffset;
        float baseFov = isCrouching ? crouchFov : _standFov;
        bool ads = _weaponRuntime != null && _weaponRuntime.IsAds && _weaponRuntime.GunConfig != null;
        float targetFov = ads
            ? _weaponRuntime.GunConfig.adsFieldOfView + dizzyFovOffset
            : baseFov + dizzyFovOffset;

        float t = 1f - Mathf.Exp(-changeSpeed * Time.deltaTime);

        var lens = _virtualCamera.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        _virtualCamera.m_Lens = lens;

        if (_recomposer != null)
        {
            float pitchShake = Mathf.Sin(_dizzyTime * buffSnapshot.dizzyShakePitchFrequencyScale + 0f) * buffSnapshot.dizzyShakePitchAmplitude;
            float yawShake = Mathf.Sin(_dizzyTime * buffSnapshot.dizzyShakeYawFrequencyScale + 2.1f) * buffSnapshot.dizzyShakeYawAmplitude;
            float rollShake = Mathf.Sin(_dizzyTime * buffSnapshot.dizzyShakeRollFrequencyScale + 4.2f) * buffSnapshot.dizzyShakeRollAmplitude;
            float vpTilt = 0f;
            float vpPan = 0f;
            if (_weaponRuntime != null)
            {
                Vector2 vpAim = _weaponRuntime.AimViewportRecoilOffset;
                if (vpAim.sqrMagnitude > 1e-14f)
                {
                    float aspect = Camera.main != null ? Camera.main.aspect : (16f / 9f);
                    WeaponViewportRecoilMath.ViewportOffsetToTiltPanDegrees(
                        vpAim,
                        lens.FieldOfView,
                        aspect,
                        out vpTilt,
                        out vpPan);
                }
            }

            float wTilt = _weaponKick != null ? _weaponKick.TiltKick : 0f;
            float wPan = _weaponKick != null ? _weaponKick.PanKick : 0f;
            float wDutch = _weaponKick != null ? _weaponKick.DutchKick : 0f;
            _recomposer.m_Tilt = _authoringRecomposerTilt + pitchShake + buffSnapshot.dizzyShakePitchOffset + vpTilt + wTilt;
            _recomposer.m_Pan = _authoringRecomposerPan + yawShake + buffSnapshot.dizzyShakeYawOffset + vpPan + wPan;
            _recomposer.m_Dutch = _authoringRecomposerDutch + rollShake + buffSnapshot.dizzyShakeRollOffset + wDutch;
        }

        if (_cameraController != null)
        {
            float dizzyDistanceOffset = dizzyWave * dizzyDistanceScale;
            float weaponZ = _weaponKick != null ? _weaponKick.ForwardZKick : 0f;
            _cameraController.SetExternalDistanceOffset(dizzyDistanceOffset + weaponZ);
            if (isCrouching)
            {
                _cameraController.SetOverrideDistance(crouchDistance);
            }
            else
            {
                _cameraController.ClearOverrideDistance();
            }

            if (ads)
            {
                _cameraController.SetWeaponDistanceOverride(_weaponRuntime.GunConfig.adsCameraDistance);
            }
            else
            {
                _cameraController.ClearWeaponDistanceOverride();
            }
        }

        _weaponKick?.StepDecay(Time.deltaTime);
    }
}
