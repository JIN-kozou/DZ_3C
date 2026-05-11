using Cinemachine;
using UnityEngine;

/// <summary>
/// Toggles Framing Transposer screen X between authoring default and an over-shoulder value, with smooth damping.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class OverShoulderFramingToggle : MonoBehaviour
{
    [SerializeField] private float overShoulderScreenX = 0.65f;
    [Tooltip("Smooth time for Screen X (SmoothDamp, seconds).")]
    [Min(0.0001f)]
    [SerializeField] private float smoothTime = 0.2f;

    private CinemachineFramingTransposer _framing;
    private float _initialScreenX;
    private bool _overShoulderActive;
    private float _screenXVelocity;

    private void Awake()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        _framing = vcam != null ? vcam.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
        if (_framing == null)
        {
            Debug.LogWarning($"{nameof(OverShoulderFramingToggle)} on {name}: no CinemachineFramingTransposer on virtual camera.", this);
            return;
        }

        _initialScreenX = _framing.m_ScreenX;
    }

    private void LateUpdate()
    {
        if (_framing == null)
        {
            return;
        }

        InputService input = InputService.Instance;
        if (input != null && input.ToggleOverShoulderWasPressedThisFrame)
        {
            _overShoulderActive = !_overShoulderActive;
        }

        float target = _overShoulderActive ? overShoulderScreenX : _initialScreenX;
        _framing.m_ScreenX = Mathf.SmoothDamp(
            _framing.m_ScreenX,
            target,
            ref _screenXVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime);
    }
}
