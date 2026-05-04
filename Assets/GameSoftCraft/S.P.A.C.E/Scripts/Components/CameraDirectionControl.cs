using UnityEngine;

namespace GameSoftCraft
{
    [RequireComponent(typeof(Camera))]
    public class CameraDirectionControl : MonoBehaviour
    {
        Quaternion _startRotaton;

        Camera _camera;

        bool _isAutopanEnabled;

        private void Start ()
        {
            _camera = GetComponent<Camera>();
            _startRotaton = _camera.transform.rotation;
            _isAutopanEnabled = true;
        }

        private void Update () 
        {
            if (Input.GetKey(KeyCode.W)) {
                Pan(Vector2.left);
            }
            if (Input.GetKey(KeyCode.A)) {
                Pan(Vector2.down);
            }
            if (Input.GetKey(KeyCode.S)) {
                Pan(Vector2.right);
            }
            if (Input.GetKey(KeyCode.D)) {
                Pan(Vector2.up);
            }
        }

        private void FixedUpdate ()
        {
            if (_isAutopanEnabled) AutoPan();
        }

        private void Pan (Vector2 direction)
        {
            _isAutopanEnabled = false;
            TextVisibilityController.Hide = true;

            _camera.transform.Rotate(direction * .1f);
        }

        private void AutoPan ()
        {
            _camera.transform.Rotate(Vector3.up * .05f);
            _camera.transform.Rotate(Vector3.right * Mathf.Sin(Time.fixedTime * .125f) * .1f);
        }
    }
}
