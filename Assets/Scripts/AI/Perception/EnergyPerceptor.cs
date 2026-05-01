using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class EnergyPerceptor : BasePerceptor
    {
        private readonly List<TargetFact> results = new();
        private readonly HashSet<int> processedTargets = new();

        protected override float TickHz => config != null ? config.energyTickHz : 5f;

        protected override void Sense(float now)
        {
            if (config == null || blackboard == null) return;

            results.Clear();
            processedTargets.Clear();
            float total = 0f;
            Collider[] overlaps = Physics.OverlapSphere(owner.position, config.energyDetectRadius, config.energyTargetMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                AIEnergySource source = overlaps[i].GetComponentInParent<AIEnergySource>();
                AITargetable target = overlaps[i].GetComponentInParent<AITargetable>();
                if (target == null) continue;
                if (!processedTargets.Add(target.GetInstanceID())) continue;

                float rawEnergy = ResolveRawEnergy(target, source);
                if (rawEnergy <= 0f) continue;

                float distance = Vector3.Distance(owner.position, target.transform.position);
                float attenuated = rawEnergy / Mathf.Max(1f, Mathf.Pow(Mathf.Max(1f, distance), config.energyFalloff));
                total += attenuated;

                if (target.IsPlayer)
                {
                    blackboard.UpdatePlayerEnergy(target.PlayerId, rawEnergy);
                }

                results.Add(new TargetFact
                {
                    target = target,
                    distance = distance,
                    timestamp = now,
                    source = ThreatSource.None
                });
            }

            blackboard.CurrentPositionEnergy = total;
            blackboard.SetEnergyTargets(results);
        }

        private static float ResolveRawEnergy(AITargetable target, AIEnergySource source)
        {
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
    }
}
