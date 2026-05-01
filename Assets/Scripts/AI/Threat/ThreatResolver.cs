using System.Collections.Generic;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Threat
{
    [DisallowMultipleComponent]
    public class ThreatResolver : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private Transform owner;

        private void Awake()
        {
            if (owner == null) owner = transform;
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        private void Update()
        {
            if (config == null || blackboard == null) return;
            float now = Time.time;

            EvaluateThreat(now);
            EvaluateClearRules();
        }

        private void EvaluateThreat(float now)
        {
            var candidate = PickBestCandidate(now);
            if (candidate.target == null) return;

            if (blackboard.HateTarget == null)
            {
                ApplyNewHate(candidate, now);
                return;
            }

            bool lockExpired = now >= blackboard.HateLockedUntil;
            bool higherPriority = candidate.priority > blackboard.HatePriority;
            bool samePriority = candidate.priority == blackboard.HatePriority;

            if (higherPriority || (lockExpired && samePriority && candidate.target != blackboard.HateTarget))
            {
                ApplyNewHate(candidate, now);
            }
        }

        private void EvaluateClearRules()
        {
            AITargetable hate = blackboard.HateTarget;
            if (hate == null) return;

            if (!hate)
            {
                ClearHate();
                return;
            }

            if (!hate.IsAlive)
            {
                ClearHate();
                return;
            }

            bool inSight = ContainsTarget(blackboard.InSightTargets, hate);
            blackboard.OutOfSightElapsed = inSight ? 0f : blackboard.OutOfSightElapsed + Time.deltaTime;

            if (blackboard.OutOfSightElapsed >= config.hateLostSightClearSeconds)
            {
                ClearHate();
                return;
            }

            if (blackboard.AssaultTaskElapsed >= config.hateAttackTaskMaxSeconds)
            {
                ClearHate();
            }
        }

        private void ClearHate()
        {
            blackboard.ClearHateTarget();
        }

        private void ApplyNewHate((AITargetable target, ThreatSource source, int priority) candidate, float now)
        {
            blackboard.SetHateTarget(candidate.target, candidate.source, candidate.priority, now + config.lockWindowSeconds);
            blackboard.IsInCombat = true;
            blackboard.OutOfSightElapsed = 0f;
            blackboard.AssaultTaskElapsed = 0f;
        }

        private (AITargetable target, ThreatSource source, int priority) PickBestCandidate(float now)
        {
            var best = (target: (AITargetable)null, source: ThreatSource.None, priority: 0, distance: float.MaxValue, lastSeen: float.MinValue);
            EvaluateBucket(blackboard.ContactTargets, 3, ThreatSource.Contact, now, ref best);
            EvaluateBucket(blackboard.Attackers, 2, ThreatSource.Attacker, now, ref best);
            EvaluateBucket(blackboard.InSightTargets, 1, ThreatSource.Sight, now, ref best);
            return (best.target, best.source, best.priority);
        }

        private void EvaluateBucket(
            IReadOnlyList<TargetFact> bucket,
            int priority,
            ThreatSource source,
            float now,
            ref (AITargetable target, ThreatSource source, int priority, float distance, float lastSeen) best)
        {
            for (int i = 0; i < bucket.Count; i++)
            {
                TargetFact fact = bucket[i];
                if (!fact.IsValid || !fact.target.IsAlive) continue;

                bool isBetter = priority > best.priority;
                if (!isBetter && priority == best.priority)
                {
                    if (fact.distance < best.distance - 0.001f) isBetter = true;
                    else if (Mathf.Abs(fact.distance - best.distance) <= 0.001f && fact.timestamp > best.lastSeen) isBetter = true;
                    else if (Mathf.Abs(fact.distance - best.distance) <= 0.001f && Mathf.Abs(fact.timestamp - best.lastSeen) <= 0.001f)
                    {
                        int currentId = fact.target.PlayerId;
                        int bestId = best.target != null ? best.target.PlayerId : int.MaxValue;
                        isBetter = currentId < bestId;
                    }
                }

                if (!isBetter) continue;
                best.target = fact.target;
                best.source = source;
                best.priority = priority;
                best.distance = fact.distance;
                best.lastSeen = fact.timestamp;
            }
        }

        private static bool ContainsTarget(IReadOnlyList<TargetFact> bucket, AITargetable target)
        {
            for (int i = 0; i < bucket.Count; i++)
            {
                if (bucket[i].target == target) return true;
            }
            return false;
        }
    }
}
