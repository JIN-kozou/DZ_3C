using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class VisionPerceptor : BasePerceptor
    {
        private readonly List<TargetFact> results = new();

        protected override float TickHz => config != null ? config.visionTickHz : 8f;

        protected override void Sense(float now)
        {
            if (config == null || blackboard == null) return;

            results.Clear();
            Vector3 origin = owner.position + Vector3.up * (config.visionHeight * 0.5f);
            Collider[] overlaps = Physics.OverlapSphere(origin, config.visionRadius, config.visionTargetMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < overlaps.Length; i++)
            {
                AITargetable target = overlaps[i].GetComponentInParent<AITargetable>();
                if (target == null || target.transform == owner || !target.IsAlive) continue;

                Vector3 toTarget = target.transform.position - owner.position;
                float planarDot = Vector3.Angle(owner.forward, Vector3.ProjectOnPlane(toTarget, Vector3.up));
                if (planarDot > config.visionAngle * 0.5f) continue;
                if (Mathf.Abs(toTarget.y) > config.visionHeight + config.visionThickness) continue;

                float distance = toTarget.magnitude;
                Vector3 rayDir = toTarget.normalized;
                if (Physics.Raycast(origin, rayDir, out RaycastHit hit, distance, config.visionObstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != target.transform && !hit.transform.IsChildOf(target.transform)) continue;
                }

                results.Add(new TargetFact
                {
                    target = target,
                    distance = distance,
                    timestamp = now,
                    source = ThreatSource.Sight
                });
            }

            blackboard.SetTargets(results, ThreatSource.Sight);
        }
    }
}
