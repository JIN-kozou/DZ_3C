using UnityEngine;

/// <summary>
/// 根据相机俯仰角调整本物体 Y（武器/肩位随视角上下），并可选用地与「持枪模式」本地位移合成，
/// 全部在同一 LateUpdate 内计算，避免与另一脚本互相覆盖 localPosition。
/// </summary>
public class CameraPitchYOffset : MonoBehaviour
{
    public enum YSpace
    {
        /// <summary>在初始 localPosition.y 上叠加俯仰偏移，再叠加持枪本地位移（若有）。</summary>
        Local,
        /// <summary>在初始 position.y（世界）上叠加俯仰偏移；持枪本地位移在此模式下不生效。</summary>
        World
    }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private YSpace ySpace = YSpace.Local;
    [Tooltip("俯仰每变化 1 度，Y 增加多少（单位与 Local/World 一致）。")]
    [SerializeField] private float yPerPitchDegree = 0.01f;
    [Tooltip("可选：限制俯仰参与计算的区间（度），超出部分按边界计。")]
    [SerializeField] private Vector2 pitchClampDegrees = new Vector2(-89f, 89f);
    [Tooltip("0 为不平滑；越大越跟手。")]
    [SerializeField, Min(0f)] private float smoothSpeed = 20f;

    [Header("持枪本地位移（与 pitch 同事一帧合成）")]
    [Tooltip("为空则依次尝试 GetComponentInParent<Player>、FindObjectOfType<Player>。")]
    [SerializeField] private Player armedPlayer;
    [Tooltip("持枪模式为开时在本地空间叠加的位移。")]
    [SerializeField] private Vector3 armedLocalOffset;
    [Tooltip("趋近持枪目标偏移的大致时间（秒）。")]
    [SerializeField, Min(0.01f)] private float armedSmoothTime = 0.2f;
    [Tooltip("勾选后仅在 AllowsArmedWeaponActions 为真时应用持枪偏移。")]
    [SerializeField] private bool requireStandingForArmedOffset;
    [Tooltip(
        "强制每帧使用「当前 localPosition − 上一帧持枪平滑量」作为基底。" +
        "不勾选时（默认）：若本帧读到的位置与上一帧本脚本写入几乎相同，则认为动画未覆盖、先减掉上一帧 armed 防止 x 每帧累加；" +
        "若差异较大则认为动画已重写 localPosition、直接用当前值，避免误减导致跳回。")]
    [SerializeField] private bool subtractPreviousArmedFromLocalPosition;
    [Tooltip("自动模式下：与上一帧写入的 localPosition 平方距离小于此值视为「动画未覆盖」，走防累减分支。")]
    [SerializeField, Min(1e-12f)] private float sameAsLastWrittenSqrEpsilon = 1e-6f;

    private float _initialLocalY;
    private float _initialWorldY;
    private float _currentYOffset;

    private Vector3 _smoothedArmedOffset;
    private Vector3 _armedSmoothVelocity;
    private Vector3 _lastWrittenLocalPos;
    private bool _hasLastWrittenLocalPos;

    private void Awake()
    {
        CacheBaseline();
        _smoothedArmedOffset = Vector3.zero;
        _armedSmoothVelocity = Vector3.zero;
        ResolveArmedPlayer();
    }

    private void OnEnable()
    {
        CacheBaseline();
        _currentYOffset = 0f;
        _smoothedArmedOffset = Vector3.zero;
        _armedSmoothVelocity = Vector3.zero;
        _hasLastWrittenLocalPos = false;
    }

    private void OnDisable()
    {
        // 仅移除持枪叠加，避免禁用组件后仍残留 armed 分量（pitch 仍由原逻辑在启用时重算 baseline）。
        if (ySpace == YSpace.Local && _smoothedArmedOffset.sqrMagnitude > 0f)
        {
            transform.localPosition -= _smoothedArmedOffset;
        }

        _smoothedArmedOffset = Vector3.zero;
        _armedSmoothVelocity = Vector3.zero;
        _hasLastWrittenLocalPos = false;
    }

    private void LateUpdate()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        float pitch = SignedEulerPitch(cam.transform.eulerAngles.x);
        pitch = Mathf.Clamp(pitch, pitchClampDegrees.x, pitchClampDegrees.y);
        float targetYOffset = pitch * yPerPitchDegree;

        if (smoothSpeed <= 0f)
        {
            _currentYOffset = targetYOffset;
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _currentYOffset = Mathf.Lerp(_currentYOffset, targetYOffset, t);
        }

        if (ySpace == YSpace.Local)
        {
            ResolveArmedPlayer();
            TryConsumePendingArmedHardStripFromPlayer();
            Vector3 prevArmed = _smoothedArmedOffset;
            Vector3 targetArmed = ShouldApplyArmedOffset() ? armedLocalOffset : Vector3.zero;
            float holsterXMul = 1f;
            if (armedPlayer != null && armedPlayer.ArmedPresentation != null)
            {
                holsterXMul = armedPlayer.ArmedPresentation.HolsterArmedOffsetXMultiplier;
            }

            targetArmed.x *= holsterXMul;
            _smoothedArmedOffset = Vector3.SmoothDamp(
                prevArmed,
                targetArmed,
                ref _armedSmoothVelocity,
                armedSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);

            Vector3 read = transform.localPosition;
            Vector3 lp;
            if (subtractPreviousArmedFromLocalPosition)
            {
                lp = read - prevArmed;
            }
            else if (_hasLastWrittenLocalPos &&
                     (read - _lastWrittenLocalPos).sqrMagnitude < sameAsLastWrittenSqrEpsilon)
            {
                // 与上一帧本脚本写入几乎一致 → 动画很可能未覆盖，先去掉上一帧 armed，否则会每帧再累加一次。
                lp = read - prevArmed;
            }
            else
            {
                lp = read;
            }

            lp.y = _initialLocalY + _currentYOffset;
            lp += _smoothedArmedOffset;
            transform.localPosition = lp;
            _lastWrittenLocalPos = lp;
            _hasLastWrittenLocalPos = true;
        }
        else
        {
            Vector3 p = transform.position;
            p.y = _initialWorldY + _currentYOffset;
            transform.position = p;
        }
    }

    private void CacheBaseline()
    {
        _initialLocalY = transform.localPosition.y;
        _initialWorldY = transform.position.y;
    }

    /// <summary>Unity 欧拉角 X 转为有符号俯仰（抬头负、低头正等与常见相机一致）。</summary>
    private static float SignedEulerPitch(float eulerX)
    {
        return eulerX > 180f ? eulerX - 360f : eulerX;
    }

    private void ResolveArmedPlayer()
    {
        if (armedPlayer != null)
        {
            return;
        }

        armedPlayer = GetComponentInParent<Player>();
        if (armedPlayer == null)
        {
            armedPlayer = FindObjectOfType<Player>();
        }
    }

    private bool ShouldApplyArmedOffset()
    {
        if (armedPlayer == null || armedPlayer.ReusableData == null)
        {
            return false;
        }

        var rd = armedPlayer.ReusableData;
        if (!rd.armedModeActive)
        {
            rd.suppressCameraArmedLocalOffset = false;
            return false;
        }

        if (rd.suppressCameraArmedLocalOffset)
        {
            return false;
        }

        if (requireStandingForArmedOffset && !rd.AllowsArmedWeaponActions())
        {
            return false;
        }

        return true;
    }

    private void TryConsumePendingArmedHardStripFromPlayer()
    {
        if (armedPlayer == null || armedPlayer.ReusableData == null)
        {
            return;
        }

        if (!armedPlayer.ReusableData.pendingCameraPitchArmedOffsetHardStrip)
        {
            return;
        }

        armedPlayer.ReusableData.pendingCameraPitchArmedOffsetHardStrip = false;
        StripArmedOffsetAndResetState();
    }

    /// <summary>仅清零持枪平滑状态，不修改 Transform。</summary>
    public void ClearArmedOffsetState()
    {
        _smoothedArmedOffset = Vector3.zero;
        _armedSmoothVelocity = Vector3.zero;
        _hasLastWrittenLocalPos = false;
    }

    /// <summary>从当前 localPosition 去掉已叠加的持枪偏移并清零状态（换挂点用）。仅 Local 模式有意义。</summary>
    public void StripArmedOffsetAndResetState()
    {
        if (ySpace == YSpace.Local)
        {
            transform.localPosition -= _smoothedArmedOffset;
        }

        ClearArmedOffsetState();
    }
}
