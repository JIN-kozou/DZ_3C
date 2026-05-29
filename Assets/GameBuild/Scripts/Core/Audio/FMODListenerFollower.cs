using FMODUnity;
using UnityEngine;

/// <summary>
/// Keeps the FMOD listener aligned with the player camera.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class FMODListenerFollower : MonoBehaviour
{
    [SerializeField] private Transform listenerTransform;
    [SerializeField] private Rigidbody listenerRigidbody;
    [SerializeField] private bool autoBindPlayerCamera = true;

    private StudioListener studioListener;

    private void Awake()
    {
        studioListener = GetComponent<StudioListener>();
        if (studioListener == null)
        {
            studioListener = gameObject.AddComponent<StudioListener>();
        }

        if (listenerTransform == null && autoBindPlayerCamera)
        {
            TryBindPlayerCamera();
        }
    }

    private void LateUpdate()
    {
        if (listenerTransform == null && autoBindPlayerCamera)
        {
            TryBindPlayerCamera();
        }

        if (listenerTransform == null)
        {
            return;
        }

        transform.SetPositionAndRotation(listenerTransform.position, listenerTransform.rotation);
    }

    public void Bind(Transform cameraTransform, Rigidbody rigidbody = null)
    {
        listenerTransform = cameraTransform;
        listenerRigidbody = rigidbody;
    }

    private void TryBindPlayerCamera()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null && player.camTransform != null)
        {
            Bind(player.camTransform, player.GetComponent<Rigidbody>());
            return;
        }

        Camera main = Camera.main;
        if (main != null)
        {
            Bind(main.transform, main.GetComponent<Rigidbody>());
        }
    }
}
