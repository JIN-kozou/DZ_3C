using System.Collections.Generic;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using DZ_3C.AI.Perception;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class PatrolPlanner : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private MonsterStatConfigSO monsterStat;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private List<Transform> spawnCenterCandidates = new();
        [SerializeField] private float patrolRadius = 20f;
        [SerializeField] private float patrolPrecision = 1.5f;
        [SerializeField] private float stayAtSpawnSeconds = 2f;
        [SerializeField] private List<PatrolPoint> patrolPoints = new();
        [SerializeField] private List<CheckpointCounter> checkpoints = new();

        private float spawnWaitRemaining;
        private bool checkpointDirty = true;
        private float checkpointReevaluateCooldown;
        private float patrolDwellUntil = -1f;
        private Transform activeSpawnCenter;
        private bool activeSpawnEvaluated;
        private MonsterStatConfigSO _monsterStatRuntime;
        private float _nextPatrolDiagLogTime;

        private Transform ActiveHome => activeSpawnCenter != null ? activeSpawnCenter : spawnCenter;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (spawnCenter == null) spawnCenter = transform;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null) checkpoints[i].OnPassCountChanged += HandleCheckpointChanged;
            }

            CacheMonsterStatFromCharacterIfMissing();
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

            EnsureActiveSpawnSelectedOnce();

            checkpointReevaluateCooldown -= Time.deltaTime;
            if (checkpointDirty && checkpointReevaluateCooldown <= 0f)
            {
                checkpointDirty = false;
                ReevaluateDesignatedCheckpoint();
            }

            UpdatePatrolTargets();
        }

        private void EnsureActiveSpawnSelectedOnce()
        {
            if (activeSpawnEvaluated) return;
            activeSpawnEvaluated = true;

            activeSpawnCenter = spawnCenter;
            if (config == null)
            {
                return;
            }

            var candidates = new List<Transform>(4 + spawnCenterCandidates.Count);
            void TryAdd(Transform t)
            {
                if (t == null) return;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] == t) return;
                }
                candidates.Add(t);
            }

            TryAdd(spawnCenter);
            for (int i = 0; i < spawnCenterCandidates.Count; i++)
            {
                TryAdd(spawnCenterCandidates[i]);
            }

            if (candidates.Count <= 1)
            {
                return;
            }

            Transform best = candidates[0];
            float bestEnergy = EnergyPerceptor.SampleTotalEnergyAt(best.position, config);
            for (int i = 1; i < candidates.Count; i++)
            {
                Transform t = candidates[i];
                float e = EnergyPerceptor.SampleTotalEnergyAt(t.position, config);
                if (e < bestEnergy)
                {
                    bestEnergy = e;
                    best = t;
                }
            }

            activeSpawnCenter = best;
        }

        private void UpdatePatrolTargets()
        {
            if (blackboard.PatrolTarget == null)
            {
                patrolDwellUntil = -1f;
                MaybeLogPatrolPointsMisconfigured();
                PatrolPoint first = FindNearestUnvisited(transform.position);
                blackboard.PatrolTarget = first != null ? first.transform : null;
                PatrolPoint firstNext = FindNearestUnvisited(blackboard.PatrolTarget != null ? blackboard.PatrolTarget.position : transform.position);
                blackboard.NextPatrolTarget = firstNext != null ? firstNext.transform : null;
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, blackboard.PatrolTarget.position);
            MonsterStatConfigSO stats = ResolveMonsterStat();
            float arrivalDistance = GetPatrolArrivalDistance(stats);
            float leaveSlack = GetPatrolLeaveSlack(stats);
            bool dwellInProgress = patrolDwellUntil > 0f && Time.time < patrolDwellUntil;

            if (distToTarget > arrivalDistance + leaveSlack)
            {
                patrolDwellUntil = -1f;
                return;
            }

            if (distToTarget > arrivalDistance)
            {
                if (dwellInProgress)
                {
                    return;
                }

                patrolDwellUntil = -1f;
                return;
            }

            PatrolPoint reached = ResolvePatrolPointForTarget(blackboard.PatrolTarget);
            if (reached != null)
            {
                float dwell = stats != null ? stats.patrolPointDwellSeconds : 0f;
                if (dwell > 0.0001f)
                {
                    if (patrolDwellUntil < 0f)
                    {
                        patrolDwellUntil = Time.time + dwell;
                    }

                    if (Time.time < patrolDwellUntil)
                    {
                        return;
                    }
                }
            }

            patrolDwellUntil = -1f;

            if (reached != null) reached.visited = true;

            if (AreAllVisitedWithinRadius())
            {
                Transform home = ActiveHome;
                if (home != null && Vector3.Distance(transform.position, home.position) > patrolPrecision)
                {
                    blackboard.PatrolTarget = home;
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

        private float GetPatrolArrivalDistance(MonsterStatConfigSO stats)
        {
            if (stats != null)
            {
                return Mathf.Max(patrolPrecision, stats.orbitRadius + stats.arriveRadius);
            }

            return patrolPrecision;
        }

        /// <summary>
        /// 环绕时 3D 距离会暂时超出「进入环带」阈值；只有超出该滞回值才视为真正离开，避免停留计时被每帧清掉。
        /// </summary>
        private float GetPatrolLeaveSlack(MonsterStatConfigSO stats)
        {
            if (stats != null)
            {
                return Mathf.Max(2f, stats.orbitVerticalAmplitude + stats.orbitRadius * 0.45f);
            }

            return 2f;
        }

        private void CacheMonsterStatFromCharacterIfMissing()
        {
            if (monsterStat != null)
            {
                _monsterStatRuntime = monsterStat;
                return;
            }

            var character = GetComponent<MonsterCharacter>();
            if (character != null && character.StatConfig != null)
            {
                _monsterStatRuntime = character.StatConfig;
            }
        }

        private MonsterStatConfigSO ResolveMonsterStat()
        {
            if (monsterStat != null)
            {
                return monsterStat;
            }

            if (_monsterStatRuntime != null)
            {
                return _monsterStatRuntime;
            }

            CacheMonsterStatFromCharacterIfMissing();
            return _monsterStatRuntime;
        }

        private PatrolPoint ResolvePatrolPointForTarget(Transform t)
        {
            if (t == null) return null;
            PatrolPoint p = t.GetComponent<PatrolPoint>();
            if (p != null) return p;
            p = t.GetComponentInParent<PatrolPoint>();
            if (p != null) return p;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null && patrolPoints[i].transform == t)
                {
                    return patrolPoints[i];
                }
            }

            return null;
        }

        private PatrolPoint FindNearestUnvisited(Vector3 from)
        {
            Transform home = ActiveHome;
            float sqrRadius = patrolRadius * patrolRadius;
            bool homeValid = home != null;

            PatrolPoint bestInRadius = null;
            float bestDistIn = float.MaxValue;
            PatrolPoint bestAny = null;
            float bestDistAny = float.MaxValue;

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                PatrolPoint point = patrolPoints[i];
                if (point == null || point.visited) continue;

                float distSq = (point.transform.position - from).sqrMagnitude;
                if (distSq < bestDistAny)
                {
                    bestDistAny = distSq;
                    bestAny = point;
                }

                if (homeValid && (point.transform.position - home.position).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                if (distSq < bestDistIn)
                {
                    bestDistIn = distSq;
                    bestInRadius = point;
                }
            }

            if (bestInRadius != null)
            {
                return bestInRadius;
            }

            if (bestAny != null && homeValid)
            {
                LogPatrolDiagOnce(
                    $"[{name}] No unvisited patrol point within patrolRadius ({patrolRadius:0.#}m) of home '{home.name}'. " +
                    "Using nearest unvisited anyway. Fix: move points inside radius, increase patrolRadius, or align Spawn Center (AISetup) with your points.");
                return bestAny;
            }

            return bestAny;
        }

        private void MaybeLogPatrolPointsMisconfigured()
        {
            if (patrolPoints == null || patrolPoints.Count == 0)
            {
                LogPatrolDiagOnce($"[{name}] PatrolPlanner has no patrol points — assign list on AISetup / PatrolPlanner and run Auto Setup.");
            }
        }

        private void LogPatrolDiagOnce(string message)
        {
            if (Time.time < _nextPatrolDiagLogTime) return;
            _nextPatrolDiagLogTime = Time.time + 5f;
            Debug.LogWarning(message, this);
        }

        private bool AreAllVisitedWithinRadius()
        {
            Transform home = ActiveHome;
            if (home == null) return true;

            float sqrRadius = patrolRadius * patrolRadius;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                PatrolPoint point = patrolPoints[i];
                if (point == null) continue;
                if ((point.transform.position - home.position).sqrMagnitude > sqrRadius) continue;
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
