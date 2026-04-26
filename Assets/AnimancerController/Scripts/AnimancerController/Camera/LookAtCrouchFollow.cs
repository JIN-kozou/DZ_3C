using UnityEngine;

/// <summary>
/// Keeps the camera LookAt target moving down while crouching.
/// Attach this to the LookAt transform used by Cinemachine.
/// </summary>
public class LookAtCrouchFollow : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private float crouchOffsetY = -0.5f;
    [SerializeField] private float smoothSpeed = 10f;

    private Vector3 _standLocalPosition;

    private void Awake()
    {
        _standLocalPosition = transform.localPosition;

        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }
    }

    private void LateUpdate()
    {
        if (player == null || player.ReusableData == null)
        {
            return;
        }

        // standValue: 1 = standing, 0 = crouching.
        float standValue = player.ReusableData.standValueParameter.CurrentValue;
        float crouchWeight = 1f - standValue;

        Vector3 targetLocalPosition = _standLocalPosition + Vector3.up * (crouchOffsetY * crouchWeight);
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, t);
    }
}
