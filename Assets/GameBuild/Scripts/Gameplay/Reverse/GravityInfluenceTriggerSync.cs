using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Keeps a sphere trigger aligned with <see cref="ShaderPosition"/> (world center =
    /// root.position + offset + triggerExtraOffset) and radius matching reveal radius (accounts for lossy scale).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public class GravityInfluenceTriggerSync : MonoBehaviour
    {
        [Tooltip("Usually the root that owns ShaderPosition (bullet / ReverseArray root). Empty = parent.")]
        [SerializeField] private Transform rootOverride;

        [Tooltip("Extra world-space offset for physics vs shader tuning.")]
        [SerializeField] private Vector3 triggerExtraOffset;

        private SphereCollider sphere;
        private ShaderPosition shaderPosition;

        public Vector3 TriggerExtraOffset
        {
            get => triggerExtraOffset;
            set => triggerExtraOffset = value;
        }

        private void Awake()
        {
            sphere = GetComponent<SphereCollider>();
            ResolveShaderPosition();
        }

        private void OnEnable()
        {
            ResolveShaderPosition();
        }

        private void ResolveShaderPosition()
        {
            Transform root = rootOverride != null ? rootOverride : transform.parent;
            if (root == null)
            {
                shaderPosition = GetComponentInParent<ShaderPosition>();
                return;
            }

            shaderPosition = root.GetComponent<ShaderPosition>();
        }

        private void LateUpdate()
        {
            if (shaderPosition == null || sphere == null)
            {
                return;
            }

            Transform root = shaderPosition.transform;
            Vector3 center = root.position + shaderPosition.offset + triggerExtraOffset;
            transform.position = center;

            float uniform = MaxUniform(transform.lossyScale);
            sphere.radius = shaderPosition.radius / Mathf.Max(uniform, 1e-4f);
        }

        private static float MaxUniform(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
    }
}
