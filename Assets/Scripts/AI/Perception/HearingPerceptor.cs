using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class HearingPerceptor : BasePerceptor
    {
        private readonly List<TargetFact> results = new();

        protected override float TickHz => config != null ? config.hearingTickHz : 8f;

        protected override void Sense(float now)
        {
            if (config == null || blackboard == null) return;

            results.Clear();
            Collider[] overlaps = Physics.OverlapSphere(owner.position, config.hearingDistance, config.hearingTargetMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                AITargetable target = overlaps[i].GetComponentInParent<AITargetable>();
                AINoiseEmitter noise = overlaps[i].GetComponentInParent<AINoiseEmitter>();
                if (target == null || noise == null || !noise.isEmitting || !target.IsAlive || target.transform == owner) continue;

                float distance = Vector3.Distance(owner.position, target.transform.position);
                float intensity = noise.loudness / Mathf.Max(1f, distance * distance);
                if (intensity < config.hearingThreshold) continue;

                results.Add(new TargetFact
                {
                    target = target,
                    distance = distance,
                    timestamp = now,
                    source = ThreatSource.None
                });
            }

            blackboard.SetTargets(results, ThreatSource.None);
        }
    }
}
