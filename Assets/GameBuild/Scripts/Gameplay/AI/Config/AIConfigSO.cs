using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Config
{
    [CreateAssetMenu(menuName = "DZ_3C/AI/AI Config", fileName = "AIConfig")]
    public class AIConfigSO : ScriptableObject
    {
        [Header("Threat")]
        [Min(0f)] public float lockWindowSeconds = 1.5f;
        [Tooltip("仇恨目标在视野列表中持续丢失超过该秒数后清空仇恨。")]
        [Min(0f)] public float hateLostSightClearSeconds = 5f;
        [Tooltip("仇恨存在且战斗方法为 Assault 时累计的时长超过该秒数后清空仇恨（切换到 Interfere 会暂停累计）。")]
        [Min(0f)] public float hateAttackTaskMaxSeconds = 12f;

        [Header("Perception Tick")]
        [Range(1f, 30f)] public float visionTickHz = 8f;
        [Range(1f, 30f)] public float hearingTickHz = 8f;
        [Range(1f, 30f)] public float distanceTickHz = 10f;
        [Range(1f, 30f)] public float energyTickHz = 5f;

        [Header("Vision")]
        [Min(0.1f)] public float visionRadius = 12f;
        [Range(1f, 179f)] public float visionAngle = 100f;
        [Min(0.1f)] public float visionHeight = 2.5f;
        [Min(0f)] public float visionThickness = 0.2f;
        public LayerMask visionTargetMask = ~0;
        public LayerMask visionObstacleMask = ~0;

        [Header("Hearing")]
        [Min(0.1f)] public float hearingDistance = 15f;
        [Min(0f)] public float hearingThreshold = 0.1f;
        [Tooltip("听觉灵敏度乘算系数。有效响度 = 声源 loudness × 本系数，再按距离平方衰减后与 hearingThreshold 比较。")]
        [Min(0f)] public float hearingSensitivity = 1f;
        [Tooltip("听到声音后优先警觉巡航的最长秒数；超时后若仍有【听到的目标】，则不再强制警觉巡航，空闲子状态按检查点 / 能量恐慌 / 巡逻选择。为 0 表示在仍有【听到的目标】时始终优先警觉巡航。")]
        [Min(0f)] public float alertPatrolPrioritySeconds = 8f;
        [Tooltip("【听到的目标】方位聚合更新频率（Hz）。")]
        [Range(1f, 30f)] public float heardFocusUpdateHz = 4f;
        [Tooltip("【听到的目标】超过该秒数未刷新则清除。")]
        [Min(0f)] public float heardFocusStaleSeconds = 3f;
        public LayerMask hearingTargetMask = ~0;

        [Header("Distance Contact")]
        [Min(0.1f)] public float contactDistance = 1f;
        public LayerMask contactTargetMask = ~0;

        [Header("Energy")]
        [Min(0.1f)] public float energyDetectRadius = 10f;
        [Min(0f)] public float energyFalloff = 1f;
        [Min(0f)] public float energyMinForAvoid = 1f;
        [Tooltip("当前位置总能量达到或超过该值时进入 EnergyAvoid（快速沿排斥方向脱离）；应大于 energyMinForAvoid。低于该值且高于 energyMinForAvoid 时巡逻仍可绕路。")]
        [Min(0f)] public float energyPanicThreshold = 3f;
        [Tooltip("巡逻朝目标前进时，排斥项相对寻路方向的最大权重（在 energyMinForAvoid 与 panic 之间随能量插值）。")]
        [Min(0f)] public float energyPatrolRepelBlendMax = 2.5f;
        [Min(0f)] public float energyMaxForDash = 3f;
        public LayerMask energyTargetMask = ~0;

        [Header("Combat/Behavior")]
        [Min(0f)] public float interfereEnergyThreshold = 20f;
        [Range(0f, 1f)] public float interfereChanceWhenLowEnergy = 0.8f;
        [Range(0f, 1f)] public float interfereChanceWhenNormalEnergy = 0.35f;
        [Min(0.05f)] public float combatMethodRollInterval = 0.4f;
        [Min(0f)] public float combatMethodMinHoldSeconds = 1.2f;
        [Min(0f)] public float retreatSelectRadius = 10f;

        /// <summary>
        /// 将声源响度换算为听觉强度（已含灵敏度系数与距离平方衰减）。
        /// </summary>
        public float EvaluateHearingIntensity(float loudness, float distance)
        {
            float effectiveLoudness = loudness * hearingSensitivity;
            return effectiveLoudness / Mathf.Max(1f, distance * distance);
        }

        /// <summary>
        /// 根据多个声源的方向与听觉强度，合成【听到的目标】世界坐标与平面朝向。
        /// </summary>
        public bool TryComputeHeardFocus(
            Vector3 ownerPosition,
            IReadOnlyList<TargetFact> sources,
            out Vector3 worldPosition,
            out Vector3 planarDirection,
            out float totalIntensity)
        {
            worldPosition = ownerPosition;
            planarDirection = Vector3.forward;
            totalIntensity = 0f;

            if (sources == null || sources.Count == 0) return false;

            Vector3 directionSum = Vector3.zero;
            float distanceWeightedSum = 0f;
            float weightSum = 0f;

            for (int i = 0; i < sources.Count; i++)
            {
                TargetFact fact = sources[i];
                if (!fact.IsValid || fact.intensity <= 0f) continue;

                Vector3 offset = fact.target.transform.position - ownerPosition;
                Vector3 planar = Vector3.ProjectOnPlane(offset, Vector3.up);
                if (planar.sqrMagnitude < 0.0001f) continue;

                Vector3 dir = planar.normalized;
                directionSum += dir * fact.intensity;
                distanceWeightedSum += fact.distance * fact.intensity;
                weightSum += fact.intensity;
            }

            if (weightSum <= 0f || directionSum.sqrMagnitude < 0.0001f) return false;

            planarDirection = directionSum.normalized;
            float probeDistance = distanceWeightedSum / weightSum;
            worldPosition = ownerPosition + planarDirection * probeDistance;
            totalIntensity = weightSum;
            return true;
        }
    }
}
