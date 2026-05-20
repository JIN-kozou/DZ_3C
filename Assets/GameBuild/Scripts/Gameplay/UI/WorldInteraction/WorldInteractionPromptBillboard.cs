using UnityEngine;

namespace DZ_3C.UI.WorldInteraction
{
  [DisallowMultipleComponent]
  public sealed class WorldInteractionPromptBillboard : MonoBehaviour
  {
    [SerializeField] private float worldScale = 0.0035f;
    [SerializeField] private Vector3 localOffset;
    [Tooltip("绕世界 Y 轴额外旋转（度）。World Space UI 面向相机时文案镜像可设为 180。")]
    [SerializeField] private float yawOffsetDegrees = 180f;
    [SerializeField] private Camera targetCamera;

    private RectTransform _rect;

    public float WorldScale
    {
      get => worldScale;
      set
      {
        worldScale = value;
        ApplyScale();
      }
    }

    public Vector3 LocalOffset
    {
      get => localOffset;
      set => localOffset = value;
    }

    private void Awake()
    {
      _rect = GetComponent<RectTransform>();
      ApplyScale();
    }

    private void LateUpdate()
    {
      if (_rect == null)
      {
        _rect = GetComponent<RectTransform>();
      }

      if (_rect == null)
      {
        return;
      }

      _rect.localPosition = localOffset;

      Camera cam = ResolveCamera();
      if (cam == null)
      {
        return;
      }

      Vector3 toCam = cam.transform.position - _rect.position;
      if (toCam.sqrMagnitude > 1e-8f)
      {
        Quaternion faceCamera = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        _rect.rotation = faceCamera * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
      }
    }

    private void ApplyScale()
    {
      if (_rect == null)
      {
        _rect = GetComponent<RectTransform>();
      }

      if (_rect != null)
      {
        _rect.localScale = Vector3.one * worldScale;
      }
    }

    private Camera ResolveCamera()
    {
      if (targetCamera != null)
      {
        return targetCamera;
      }

      return Camera.main;
    }
  }
}
