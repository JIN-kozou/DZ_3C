using Cinemachine;
using UnityEngine;

/// <summary>
/// Changes Cinemachine FOV while crouching.
/// Attach this to the same object as CinemachineVirtualCamera.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CrouchFovController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float crouchFov = 52f;
    [SerializeField] private float crouchDistance = 3f;
    [SerializeField] private float changeSpeed = 8f;

    private CinemachineVirtualCamera _virtualCamera;
    private float _standFov;
    private CameraController _cameraController;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _standFov = _virtualCamera.m_Lens.FieldOfView;
        _cameraController = GetComponent<CameraController>();

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void LateUpdate()
    {
        if (_virtualCamera == null || player == null || player.ReusableData == null)
        {
            return;
        }

        // standValue: 1 = standing, 0 = crouching.
        float standValue = player.ReusableData.standValueParameter.CurrentValue;
        bool isCrouching = standValue < 0.5f;
        float targetFov = isCrouching ? crouchFov : _standFov;
        float t = 1f - Mathf.Exp(-changeSpeed * Time.deltaTime);

        var lens = _virtualCamera.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        _virtualCamera.m_Lens = lens;

        if (_cameraController != null)
        {
            if (isCrouching)
            {
                _cameraController.SetOverrideDistance(crouchDistance);
            }
            else
            {
                _cameraController.ClearOverrideDistance();
            }
        }
    }
}
