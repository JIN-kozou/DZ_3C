using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    /// <summary>
    /// 将当前帧听觉目标按强度加权合成为【听到的目标】，并按配置频率刷新；超时未刷新则清除。
    /// </summary>
    [DisallowMultipleComponent]
    public class HeardFocusTracker : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private Transform owner;

        private float updateElapsed;

        private void Awake()
        {
            if (owner == null) owner = transform;
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        private void Update()
        {
            if (config == null || blackboard == null || owner == null) return;

            float now = Time.time;
            if (blackboard.HasHeardFocus &&
                now - blackboard.HeardFocusLastUpdatedTime >= config.heardFocusStaleSeconds)
            {
                blackboard.ClearHeardFocus();
            }

            float interval = Mathf.Max(0.01f, 1f / Mathf.Max(1f, config.heardFocusUpdateHz));
            updateElapsed += Time.deltaTime;
            if (updateElapsed < interval) return;
            updateElapsed = 0f;

            if (!config.TryComputeHeardFocus(
                    owner.position,
                    blackboard.HeardTargets,
                    out Vector3 worldPosition,
                    out Vector3 planarDirection,
                    out float totalIntensity))
            {
                return;
            }

            blackboard.SetHeardFocus(worldPosition, planarDirection, totalIntensity, now);
        }
    }
}
