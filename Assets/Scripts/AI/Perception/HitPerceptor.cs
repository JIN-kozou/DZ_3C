using System.Collections.Generic;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class HitPerceptor : MonoBehaviour
    {
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private Transform owner;

        private readonly List<TargetFact> results = new();

        private void Awake()
        {
            if (owner == null) owner = transform;
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        public void ReportDamage(GameObject attackerObject)
        {
            if (blackboard == null || attackerObject == null) return;
            AITargetable attacker = attackerObject.GetComponentInParent<AITargetable>();
            if (attacker == null || !attacker.IsAlive || attacker.transform == owner) return;

            results.Clear();
            results.Add(new TargetFact
            {
                target = attacker,
                distance = Vector3.Distance(owner.position, attacker.transform.position),
                timestamp = Time.time,
                source = ThreatSource.Attacker
            });
            blackboard.SetTargets(results, ThreatSource.Attacker);
        }
    }
}
