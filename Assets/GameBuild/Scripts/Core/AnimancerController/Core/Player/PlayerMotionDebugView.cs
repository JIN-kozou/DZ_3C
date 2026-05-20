using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMotionDebugView : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private Vector2 anchor = new Vector2(20f, 120f);
    [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;

    private Vector3 lastPosition;
    private float smoothedSpeed;
    private float speedVelocity;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        if (player != null)
        {
            lastPosition = player.transform.position;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = player.transform.position - lastPosition;
        float currentSpeed = new Vector2(delta.x, delta.z).magnitude / dt;
        smoothedSpeed = Mathf.SmoothDamp(smoothedSpeed, currentSpeed, ref speedVelocity, smoothTime);
        lastPosition = player.transform.position;
    }

    private void OnGUI()
    {
        if (!showOnScreen || player == null)
        {
            return;
        }

        Vector3 forward = player.transform.forward;
        float yaw = player.transform.eulerAngles.y;
        Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

        Rect rect = new Rect(anchor.x, anchor.y, 380f, 90f);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label($"Speed: {smoothedSpeed:F2} m/s");
        GUILayout.Label($"Facing Yaw: {yaw:F1}°");
        GUILayout.Label($"Facing Dir: ({planarForward.x:F2}, {planarForward.z:F2})");
        GUILayout.EndArea();
    }
}
