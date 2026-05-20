using System.Collections.Generic;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class RetreatController : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private HTNMethodSelector selector;
        [SerializeField] private List<PatrolPoint> patrolPoints = new();
        [SerializeField] private float arriveDistance = 1f;

        private AITargetable previousHate;
        private Transform retreatTarget;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
        }

        private void OnEnable()
        {
            if (blackboard != null) blackboard.OnHateTargetChanged += HandleHateChanged;
        }

        private void OnDisable()
        {
            if (blackboard != null) blackboard.OnHateTargetChanged -= HandleHateChanged;
        }

        private void Update()
        {
            if (!blackboard.IsInRetreat || retreatTarget == null) return;
            if (Vector3.Distance(transform.position, retreatTarget.position) > arriveDistance) return;

            selector.NotifyRetreatArrived();
            retreatTarget = null;
        }

        private void HandleHateChanged(AITargetable current)
        {
            if (current == null && previousHate != null)
            {
                retreatTarget = PickRetreatTarget();
                if (retreatTarget != null)
                {
                    blackboard.PatrolTarget = retreatTarget;
                    selector.NotifyHateClearedEnterRetreat(retreatTarget);
                }
            }

            previousHate = current;
        }

        private Transform PickRetreatTarget()
        {
            float radius = config != null ? config.retreatSelectRadius : 10f;
            float sqr = radius * radius;
            var candidates = new List<PatrolPoint>(8);
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                PatrolPoint point = patrolPoints[i];
                if (point == null) continue;
                if ((point.transform.position - transform.position).sqrMagnitude > sqr) continue;
                candidates.Add(point);
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)].transform;
        }
    }
}
