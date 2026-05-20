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

        private readonly Dictionary<int, float> attackerOutOfSightElapsed = new();

        private void Awake()
        {
            if (owner == null) owner = transform;
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        private void Update()
        {
            if (config == null || blackboard == null) return;
            float now = Time.time;

            EvaluateAttackerBucketClearRules();
            EvaluateThreat(now);
            EvaluateHateClearRules();
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

        private void EvaluateAttackerBucketClearRules()
        {
            IReadOnlyList<TargetFact> bucket = blackboard.Attackers;
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                TargetFact fact = bucket[i];
                AITargetable target = fact.target;
                if (ShouldClearThreatTarget(target, out float updatedOutOfSight))
                {
                    SetOutOfSightElapsed(target, updatedOutOfSight);
                    blackboard.RemoveAttackerTarget(target);
                    ForgetAttackerOutOfSight(target);
                    continue;
                }

                SetOutOfSightElapsed(target, updatedOutOfSight);
            }

            PruneStaleAttackerOutOfSightKeys();
        }

        private void EvaluateHateClearRules()
        {
            AITargetable hate = blackboard.HateTarget;
            if (hate == null) return;

            if (ShouldClearThreatTarget(hate, out float updatedOutOfSight))
            {
                SetOutOfSightElapsed(hate, updatedOutOfSight);
                ClearHate(hate);
                return;
            }

            blackboard.OutOfSightElapsed = updatedOutOfSight;
        }

        private bool ShouldClearThreatTarget(AITargetable target, out float updatedOutOfSightElapsed)
        {
            updatedOutOfSightElapsed = 0f;
            if (target == null) return true;
            if (!target) return true;
            if (!target.IsAlive) return true;

            bool inSight = ContainsTarget(blackboard.InSightTargets, target);
            float elapsed = GetOutOfSightElapsed(target);
            updatedOutOfSightElapsed = inSight ? 0f : elapsed + Time.deltaTime;

            if (updatedOutOfSightElapsed >= config.hateLostSightClearSeconds) return true;

            if (target == blackboard.HateTarget && blackboard.AssaultTaskElapsed >= config.hateAttackTaskMaxSeconds)
            {
                return true;
            }

            return false;
        }

        private float GetOutOfSightElapsed(AITargetable target)
        {
            if (target == blackboard.HateTarget) return blackboard.OutOfSightElapsed;

            int id = target.GetInstanceID();
            return attackerOutOfSightElapsed.TryGetValue(id, out float elapsed) ? elapsed : 0f;
        }

        private void SetOutOfSightElapsed(AITargetable target, float elapsed)
        {
            if (target == null) return;

            if (target == blackboard.HateTarget)
            {
                blackboard.OutOfSightElapsed = elapsed;
                return;
            }

            attackerOutOfSightElapsed[target.GetInstanceID()] = elapsed;
        }

        private void ForgetAttackerOutOfSight(AITargetable target)
        {
            if (target == null) return;
            attackerOutOfSightElapsed.Remove(target.GetInstanceID());
        }

        private void PruneStaleAttackerOutOfSightKeys()
        {
            if (attackerOutOfSightElapsed.Count == 0) return;

            var staleKeys = new List<int>(4);
            foreach (KeyValuePair<int, float> pair in attackerOutOfSightElapsed)
            {
                if (IsAttackerOutOfSightKeyActive(pair.Key)) continue;
                staleKeys.Add(pair.Key);
            }

            for (int i = 0; i < staleKeys.Count; i++)
            {
                attackerOutOfSightElapsed.Remove(staleKeys[i]);
            }
        }

        private bool IsAttackerOutOfSightKeyActive(int instanceId)
        {
            IReadOnlyList<TargetFact> bucket = blackboard.Attackers;
            for (int i = 0; i < bucket.Count; i++)
            {
                TargetFact fact = bucket[i];
                if (fact.IsValid && fact.target.GetInstanceID() == instanceId) return true;
            }

            AITargetable hate = blackboard.HateTarget;
            return hate != null && hate && hate.GetInstanceID() == instanceId;
        }

        private void ClearHate(AITargetable targetToRemoveFromAttackers)
        {
            blackboard.ClearHateTarget();
            if (targetToRemoveFromAttackers != null)
            {
                blackboard.RemoveAttackerTarget(targetToRemoveFromAttackers);
                ForgetAttackerOutOfSight(targetToRemoveFromAttackers);
            }
        }

        private void ApplyNewHate((AITargetable target, ThreatSource source, int priority) candidate, float now)
        {
            blackboard.SetHateTarget(candidate.target, candidate.source, candidate.priority, now + config.lockWindowSeconds);
            blackboard.IsInCombat = true;
            blackboard.OutOfSightElapsed = 0f;
            blackboard.AssaultTaskElapsed = 0f;
            ForgetAttackerOutOfSight(candidate.target);
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
