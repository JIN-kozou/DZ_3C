using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class DistancePerceptor : BasePerceptor
    {
        private readonly List<TargetFact> results = new();

        protected override float TickHz => config != null ? config.distanceTickHz : 10f;

        protected override void Sense(float now)
        {
            if (config == null || blackboard == null) return;

            results.Clear();
            Collider[] overlaps = Physics.OverlapSphere(owner.position, config.contactDistance, config.contactTargetMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                AITargetable target = overlaps[i].GetComponentInParent<AITargetable>();
                if (target == null || !target.IsAlive || target.transform == owner) continue;

                results.Add(new TargetFact
                {
                    target = target,
                    distance = Vector3.Distance(owner.position, target.transform.position),
                    timestamp = now,
                    source = ThreatSource.Contact
                });
            }

            blackboard.SetTargets(results, ThreatSource.Contact);
        }
    }
}
