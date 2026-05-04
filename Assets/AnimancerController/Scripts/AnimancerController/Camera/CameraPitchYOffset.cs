using UnityEngine;

/// <summary>
/// 根据相机俯仰角（绕 X 轴的旋转）调整本物体 Y：用于武器/肩位等随视角上下微调。
/// 俯仰角取 <see cref="Camera"/> 的欧拉角 X，并转换为 -180~180 度有符号角度。
/// </summary>
public class CameraPitchYOffset : MonoBehaviour
{
    public enum YSpace
    {
        /// <summary>在初始 localPosition.y 上叠加偏移。</summary>
        Local,
        /// <summary>在初始 position.y（世界）上叠加偏移。</summary>
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

    private float _initialLocalY;
    private float _initialWorldY;
    private float _currentYOffset;

    private void Awake()
    {
        CacheBaseline();
    }

    private void OnEnable()
    {
        CacheBaseline();
        _currentYOffset = 0f;
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
            Vector3 lp = transform.localPosition;
            lp.y = _initialLocalY + _currentYOffset;
            transform.localPosition = lp;
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
}
