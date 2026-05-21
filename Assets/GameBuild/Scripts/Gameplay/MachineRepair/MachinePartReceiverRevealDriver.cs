using UnityEngine;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// Scales receiver <see cref="ShaderPosition.radius"/> by item-weighted repair progress,
    /// using the same transition timing/curve as player reverse vision.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MachinePartReceiver), typeof(ShaderPosition), typeof(ShaderPositionRadiusTransition))]
    public class MachinePartReceiverRevealDriver : MonoBehaviour
    {
        [SerializeField] private MachinePartReceiver receiver;
        [SerializeField] private ShaderPosition shaderPosition;
        [SerializeField] private ShaderPositionRadiusTransition radiusTransition;

        [Tooltip("进度为 0 时的揭示半径（米）。满进度仍为 ShaderPosition.radius。")]
        [Min(0f)]
        [SerializeField] private float minRadius = 2f;

        private float baseMaxRadius;
        private float effectiveMinRadius;

        private void Awake()
        {
            if (receiver == null) receiver = GetComponent<MachinePartReceiver>();
            if (shaderPosition == null) shaderPosition = GetComponent<ShaderPosition>();
            if (radiusTransition == null) radiusTransition = GetComponent<ShaderPositionRadiusTransition>();

            if (shaderPosition != null)
            {
                baseMaxRadius = Mathf.Max(0f, shaderPosition.radius);
                effectiveMinRadius = Mathf.Clamp(minRadius, 0f, baseMaxRadius);
            }
        }

        private void Start()
        {
            radiusTransition?.ApplyRadiusImmediate(ComputeTargetRadius());
        }

        public void RefreshFromRequirements()
        {
            if (radiusTransition == null)
            {
                return;
            }

            radiusTransition.SetTargetRadius(ComputeTargetRadius());
        }

        private float ComputeTargetRadius()
        {
            if (receiver == null)
            {
                return effectiveMinRadius;
            }

            float ratio = receiver.GetItemWeightedCompletionRatio();
            return Mathf.Lerp(effectiveMinRadius, baseMaxRadius, ratio);
        }
    }
}
