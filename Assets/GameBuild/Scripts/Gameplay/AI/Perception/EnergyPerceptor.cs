using System.Collections.Generic;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class EnergyPerceptor : BasePerceptor
    {
        private readonly List<TargetFact> results = new();

        protected override float TickHz => config != null ? config.energyTickHz : 5f;

        protected override void Sense(float now)
        {
            if (config == null || blackboard == null) return;

            results.Clear();
            float total = GatherEnergyAtStatic(owner.position, now, results, blackboard, updatePlayerEnergy: true, config);
            blackboard.CurrentPositionEnergy = total;
            blackboard.SetEnergyTargets(results);
        }

        /// <summary>
        /// 在给定世界坐标用与感知相同的规则采样总能量（用于多出生点选取等，不写入黑板）。
        /// </summary>
        public static float SampleTotalEnergyAt(Vector3 worldPosition, AIConfigSO cfg)
        {
            if (cfg == null) return 0f;
            var tmp = new List<TargetFact>(8);
            return GatherEnergyAtStatic(worldPosition, Time.time, tmp, null, false, cfg);
        }

        /// <summary>
        /// 由当前帧能量源列表合成平面排斥方向（与 <see cref="Sense"/> 衰减公式一致）。
        /// </summary>
        public static Vector3 ComputePlanarEnergyRepel(
            Vector3 selfWorldPos,
            IReadOnlyList<TargetFact> energyTargets,
            AIConfigSO cfg)
        {
            if (cfg == null || energyTargets == null) return Vector3.zero;

            Vector3 acc = Vector3.zero;
            for (int i = 0; i < energyTargets.Count; i++)
            {
                TargetFact fact = energyTargets[i];
                if (!fact.IsValid) continue;

                Vector3 targetPos = fact.target.transform.position;
                Vector3 delta = Vector3.ProjectOnPlane(selfWorldPos - targetPos, Vector3.up);
                float d = delta.magnitude;
                if (d < 0.01f) continue;

                float raw = ResolveRawEnergy(fact.target, null);
                if (raw <= 0f) continue;

                float w = raw / Mathf.Max(1f, Mathf.Pow(Mathf.Max(1f, d), cfg.energyFalloff));
                acc += delta * (w / d);
            }

            return acc;
        }

        public static float ResolveRawEnergy(AITargetable target, AIEnergySource source)
        {
            if (target == null) return 0f;

            var providers = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < providers.Length; i++)
            {
                if (providers[i] is IAIReverseEnergyProvider energyProvider)
                {
                    return Mathf.Max(0f, energyProvider.ReverseEnergy);
                }
            }

            if (source != null) return Mathf.Max(0f, source.energy);
            return Mathf.Max(0f, target.FallbackReverseEnergy);
        }

        private static float GatherEnergyAtStatic(
            Vector3 worldPosition,
            float now,
            List<TargetFact> outResults,
            AIBlackboard bb,
            bool updatePlayerEnergy,
            AIConfigSO cfg)
        {
            if (cfg == null) return 0f;

            var processed = new HashSet<int>();
            float total = 0f;
            Collider[] overlaps = Physics.OverlapSphere(
                worldPosition,
                cfg.energyDetectRadius,
                cfg.energyTargetMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < overlaps.Length; i++)
            {
                AIEnergySource source = overlaps[i].GetComponentInParent<AIEnergySource>();
                AITargetable target = overlaps[i].GetComponentInParent<AITargetable>();
                if (target == null) continue;
                if (!processed.Add(target.GetInstanceID())) continue;

                float rawEnergy = ResolveRawEnergy(target, source);
                if (rawEnergy <= 0f) continue;

                float distance = Vector3.Distance(worldPosition, target.transform.position);
                float attenuated = rawEnergy / Mathf.Max(1f, Mathf.Pow(Mathf.Max(1f, distance), cfg.energyFalloff));
                total += attenuated;

                if (updatePlayerEnergy && bb != null && target.IsPlayer)
                {
                    bb.UpdatePlayerEnergy(target.PlayerId, rawEnergy);
                }

                outResults?.Add(new TargetFact
                {
                    target = target,
                    distance = distance,
                    timestamp = now,
                    source = ThreatSource.None
                });
            }

            return total;
        }
    }
}
