using System.Collections.Generic;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class PatrolPlanner : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private float patrolRadius = 20f;
        [SerializeField] private float patrolPrecision = 1.5f;
        [SerializeField] private float stayAtSpawnSeconds = 2f;
        [SerializeField] private List<PatrolPoint> patrolPoints = new();
        [SerializeField] private List<CheckpointCounter> checkpoints = new();

        private float spawnWaitRemaining;
        private bool checkpointDirty = true;
        private float checkpointReevaluateCooldown;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (spawnCenter == null) spawnCenter = transform;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null) checkpoints[i].OnPassCountChanged += HandleCheckpointChanged;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null) checkpoints[i].OnPassCountChanged -= HandleCheckpointChanged;
            }
        }

        private void Update()
        {
            if (blackboard == null) return;

            checkpointReevaluateCooldown -= Time.deltaTime;
            if (checkpointDirty && checkpointReevaluateCooldown <= 0f)
            {
                checkpointDirty = false;
                ReevaluateDesignatedCheckpoint();
            }

            UpdatePatrolTargets();
        }

        private void UpdatePatrolTargets()
        {
            if (blackboard.PatrolTarget == null)
            {
                PatrolPoint first = FindNearestUnvisited(transform.position);
                blackboard.PatrolTarget = first != null ? first.transform : null;
                PatrolPoint firstNext = FindNearestUnvisited(blackboard.PatrolTarget != null ? blackboard.PatrolTarget.position : transform.position);
                blackboard.NextPatrolTarget = firstNext != null ? firstNext.transform : null;
                return;
            }

            if (Vector3.Distance(transform.position, blackboard.PatrolTarget.position) > patrolPrecision) return;

            PatrolPoint reached = blackboard.PatrolTarget.GetComponent<PatrolPoint>();
            if (reached != null) reached.visited = true;

            if (AreAllVisitedWithinRadius())
            {
                if (Vector3.Distance(transform.position, spawnCenter.position) > patrolPrecision)
                {
                    blackboard.PatrolTarget = spawnCenter;
                    blackboard.NextPatrolTarget = null;
                    return;
                }

                spawnWaitRemaining += Time.deltaTime;
                if (spawnWaitRemaining < stayAtSpawnSeconds) return;

                spawnWaitRemaining = 0f;
                ResetVisited();
            }

            if (blackboard.NextPatrolTarget != null)
            {
                blackboard.PatrolTarget = blackboard.NextPatrolTarget;
            }
            else
            {
                PatrolPoint next = FindNearestUnvisited(transform.position);
                blackboard.PatrolTarget = next != null ? next.transform : null;
            }
            PatrolPoint finalNext = FindNearestUnvisited(blackboard.PatrolTarget != null ? blackboard.PatrolTarget.position : transform.position);
            blackboard.NextPatrolTarget = finalNext != null ? finalNext.transform : null;
        }

        private PatrolPoint FindNearestUnvisited(Vector3 from)
        {
            PatrolPoint best = null;
            float bestDist = float.MaxValue;
            float sqrRadius = patrolRadius * patrolRadius;

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                PatrolPoint point = patrolPoints[i];
                if (point == null || point.visited) continue;
                if ((point.transform.position - spawnCenter.position).sqrMagnitude > sqrRadius) continue;

                float dist = (point.transform.position - from).sqrMagnitude;
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = point;
            }

            return best;
        }

        private bool AreAllVisitedWithinRadius()
        {
            float sqrRadius = patrolRadius * patrolRadius;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                PatrolPoint point = patrolPoints[i];
                if (point == null) continue;
                if ((point.transform.position - spawnCenter.position).sqrMagnitude > sqrRadius) continue;
                if (!point.visited) return false;
            }
            return true;
        }

        private void ResetVisited()
        {
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null) patrolPoints[i].visited = false;
            }
        }

        private void HandleCheckpointChanged(CheckpointCounter _)
        {
            checkpointDirty = true;
            checkpointReevaluateCooldown = 0.02f;
        }

        private void ReevaluateDesignatedCheckpoint()
        {
            CheckpointCounter best = null;
            float bestDist = float.MaxValue;
            int maxCount = int.MinValue;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                CheckpointCounter cp = checkpoints[i];
                if (cp == null) continue;

                if (cp.PassCount > maxCount)
                {
                    best = cp;
                    maxCount = cp.PassCount;
                    bestDist = Vector3.Distance(transform.position, cp.transform.position);
                    continue;
                }

                if (cp.PassCount == maxCount)
                {
                    float dist = Vector3.Distance(transform.position, cp.transform.position);
                    if (dist < bestDist)
                    {
                        best = cp;
                        bestDist = dist;
                    }
                }
            }

            blackboard.DesignatedCheckpoint = best != null ? best.transform : null;
        }
    }
}
