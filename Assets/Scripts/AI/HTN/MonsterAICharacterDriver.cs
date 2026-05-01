using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class MonsterAICharacterDriver : MonoBehaviour
    {
        [SerializeField] private MonsterStatConfigSO monsterStat;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private HTNMethodSelector selector;
        [SerializeField] private MonsterCharacter character;
        [SerializeField] private MonoBehaviour attackHandler;

        private IMonsterAttack attackHandlerInterface;

        private enum AtomicTask
        {
            None,
            HorizontalMove,
            Ascend,
            Descend,
            Hover,
            Orbit,
            Dash,
            Strafe,
            Attack
        }

        private struct BurstMotion
        {
            public bool active;
            public Vector3 direction;
            public float remainingDistance;
            public float elapsed;
            public float totalTime;
            public float accelTime;
            public float decelTime;
            public float maxSpeed;
            public AnimationCurve speedCurve;

            public void Start(Vector3 dir, float distance, float speed, float accel, float decel, AnimationCurve curve)
            {
                direction = dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.zero;
                remainingDistance = Mathf.Max(0f, distance);
                maxSpeed = Mathf.Max(0.01f, speed);
                accelTime = Mathf.Max(0f, accel);
                decelTime = Mathf.Max(0f, decel);
                totalTime = remainingDistance / maxSpeed;
                elapsed = 0f;
                speedCurve = curve;
                active = remainingDistance > 0.001f && direction.sqrMagnitude > 0.000001f;
            }
        }

        public string CurrentAtomicTaskDebug => currentAtomicTask.ToString();
        public string CurrentRootDebug => selector != null ? selector.CurrentRoot.ToString() : "None";
        public string CurrentMethodDebug => selector == null
            ? "None"
            : selector.CurrentRoot == RootBehavior.Combat
                ? selector.CurrentCombatMethod.ToString()
                : selector.CurrentRoot == RootBehavior.Retreat
                    ? "RetreatMove"
                    : selector.CurrentIdleMethod.ToString();

        private float patrolPauseUntil;
        private float nextAttackTime;
        private float nextDashTime;
        private float nextStrafeTime;
        private float orbitAngle;
        private float interfereUntil;
        private float checkpointOrbitUntil;
        private float alertDescendUntil;
        private float postAttackBackoffUntil;
        private float energyAvoidTurnSign = 1f;
        private bool pendingAscendAfterCombat;
        private float nextAttackIntervalManeuverTime;
        private float attackIntervalVerticalUntil;
        private float attackIntervalVerticalSign = 1f;
        private float nextOrbitVerticalBurstTime;
        private float orbitVerticalBurstUntil;
        private float orbitVerticalBurstSign = 1f;
        private float orbitVerticalBurstStartTime;
        private float orbitVerticalBurstDuration;

        private AtomicTask currentAtomicTask;
        private BurstMotion dashBurst;
        private BurstMotion strafeBurst;
        private Vector3 hoverSeedOffset;
        private float methodEnterTime;
        private RootBehavior lastRoot;
        private IdleMethod lastIdleMethod;
        private CombatMethod lastCombatMethod;

        private Vector3 framePlanarDelta;
        private float frameVerticalDelta;
        private Vector3 lookDirection;
        private bool hasLookDirection;
        private Transform cachedPlayerStandoffTransform;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
            if (character == null) character = GetComponent<MonsterCharacter>();
            attackHandlerInterface = attackHandler as IMonsterAttack;

            if (character != null && monsterStat != null)
            {
                character.SetStatConfig(monsterStat);
            }

            float hoverRadius = monsterStat != null ? monsterStat.hoverRadius : 0.5f;
            hoverSeedOffset = new Vector3(
                Random.Range(-hoverRadius, hoverRadius),
                0f,
                Random.Range(-hoverRadius, hoverRadius));
            lastRoot = selector != null ? selector.CurrentRoot : RootBehavior.Idle;
            lastIdleMethod = selector != null ? selector.CurrentIdleMethod : IdleMethod.Patrol;
            lastCombatMethod = selector != null ? selector.CurrentCombatMethod : CombatMethod.Assault;
        }

        private void Update()
        {
            if (monsterStat == null || blackboard == null || selector == null || character == null) return;

            BeginFrame();

            DetectMethodTransition();
            ConsumeBurstMovement();

            ApplyPostCombatAscendVertical();

            switch (selector.CurrentRoot)
            {
                case RootBehavior.Combat:
                    TickCombat();
                    break;
                case RootBehavior.Retreat:
                    TickRetreat();
                    break;
                default:
                    TickIdle();
                    break;
            }

            TrackAssaultTaskDuration();

            if (monsterStat.aerialMode)
            {
                MaintainCruiseHeight();
            }

            ApplyFrameMotion();
        }

        private void BeginFrame()
        {
            framePlanarDelta = Vector3.zero;
            frameVerticalDelta = 0f;
            lookDirection = Vector3.zero;
            hasLookDirection = false;
        }

        private void ApplyFrameMotion()
        {
            Vector3 planar = Vector3.ProjectOnPlane(framePlanarDelta, Vector3.up);
            character.MoveBy(planar + Vector3.up * frameVerticalDelta);

            EnforceAerialFloorConstraint();
            EnforcePlayerHorizontalStandoff();

            if (hasLookDirection)
            {
                Vector3 planarLook = Vector3.ProjectOnPlane(lookDirection, Vector3.up);
                if (planarLook.sqrMagnitude > 0.000001f)
                {
                    character.RotateTowards(planarLook, monsterStat.turnSpeed);
                }
            }
        }

        private void SetDominant(AtomicTask task)
        {
            currentAtomicTask = task;
        }

        private void AccumulatePlanar(Vector3 worldDelta)
        {
            framePlanarDelta += worldDelta;
        }

        private void AccumulateVertical(float deltaY)
        {
            frameVerticalDelta += deltaY;
        }

        private void EnforceAerialFloorConstraint()
        {
            if (monsterStat == null || !monsterStat.aerialMode || !monsterStat.enforceAerialMinWorldHeight) return;

            float floorY = monsterStat.aerialMinWorldHeight;
            float y = transform.position.y;
            if (y < floorY)
            {
                character.MoveBy(Vector3.up * (floorY - y));
            }
        }

        private Transform TryGetPlayerTransformForStandoff()
        {
            if (!string.IsNullOrEmpty(monsterStat.playerStandoffTag))
            {
                if (cachedPlayerStandoffTransform == null)
                {
                    GameObject tagged = GameObject.FindGameObjectWithTag(monsterStat.playerStandoffTag);
                    if (tagged != null) cachedPlayerStandoffTransform = tagged.transform;
                }
                if (cachedPlayerStandoffTransform != null)
                    return cachedPlayerStandoffTransform;
            }

            if (blackboard.HateTarget != null && blackboard.HateTarget.IsPlayer)
                return blackboard.HateTarget.transform;

            return null;
        }

        private void EnforcePlayerHorizontalStandoff()
        {
            if (monsterStat == null || monsterStat.minHorizontalDistanceToPlayer <= 0.001f) return;

            Transform playerT = TryGetPlayerTransformForStandoff();
            if (playerT == null) return;

            Vector3 m = transform.position;
            Vector3 p = playerT.position;
            Vector3 d = Vector3.ProjectOnPlane(m - p, Vector3.up);
            float dist = d.magnitude;
            float r = monsterStat.minHorizontalDistanceToPlayer;
            if (dist >= r - 0.02f) return;

            Vector3 outward = dist > 0.0001f
                ? d / dist
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.0001f) outward = Vector3.forward;

            float push = r - dist;
            character.MoveBy(new Vector3(outward.x * push, 0f, outward.z * push));
        }

        private void SetLook(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude <= 0.000001f) return;
            lookDirection = worldDirection;
            hasLookDirection = true;
        }

        private void TickIdle()
        {
            switch (selector.CurrentIdleMethod)
            {
                case IdleMethod.AlertPatrol:
                    TickAlertPatrol();
                    break;
                case IdleMethod.Checkpoint:
                    TickCheckpoint();
                    break;
                case IdleMethod.EnergyAvoid:
                    TickEnergyAvoid();
                    break;
                default:
                    TickPatrol();
                    break;
            }
        }

        private void TickCombat()
        {
            AITargetable target = blackboard.HateTarget;
            if (target == null) return;

            if (selector.CurrentCombatMethod == CombatMethod.Interfere)
            {
                TickInterfere(target);
            }
            else
            {
                TickAssault(target);
            }
        }

        private void TrackAssaultTaskDuration()
        {
            if (blackboard.HateTarget == null)
            {
                blackboard.AssaultTaskElapsed = 0f;
                return;
            }

            if (selector.CurrentRoot != RootBehavior.Combat)
            {
                blackboard.AssaultTaskElapsed = 0f;
                return;
            }

            if (selector.CurrentCombatMethod != CombatMethod.Assault)
            {
                return;
            }

            blackboard.AssaultTaskElapsed += Time.deltaTime;
        }

        private void TickRetreat()
        {
            SetDominant(AtomicTask.HorizontalMove);
            PlanarSeekTransform(blackboard.PatrolTarget, monsterStat.moveSpeed);
            AccumulateHoverDrift(monsterStat.hoverRadius * 0.25f);
        }

        private void TickPatrol()
        {
            Transform target = blackboard.PatrolTarget;
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= monsterStat.arriveRadius)
            {
                if (Time.time < patrolPauseUntil)
                {
                    SetDominant(AtomicTask.Hover);
                    AccumulateHoverDrift(monsterStat.hoverRadius);
                    return;
                }

                float pause = Random.Range(monsterStat.patrolPauseSecondsMin, Mathf.Max(monsterStat.patrolPauseSecondsMin, monsterStat.patrolPauseSecondsMax));
                patrolPauseUntil = Time.time + pause;
                SetDominant(AtomicTask.Hover);
                AccumulateHoverDrift(monsterStat.hoverRadius);
                return;
            }

            SetDominant(AtomicTask.HorizontalMove);
            PlanarSeekWorld(target.position, monsterStat.moveSpeed);
        }

        private void TickAlertPatrol()
        {
            if (blackboard.HeardTargets.Count == 0) return;
            TargetFact heard = blackboard.HeardTargets[0];
            if (!heard.IsValid) return;

            Vector3 targetPos = heard.target.transform.position;
            SetDominant(AtomicTask.HorizontalMove);
            PlanarSeekWorld(targetPos, monsterStat.moveSpeed);

            bool shouldDescend = transform.position.y > monsterStat.alertPatrolTargetHeight + 0.05f || Time.time < alertDescendUntil;
            if (shouldDescend)
            {
                SetDominant(AtomicTask.Descend);
                AccumulateVerticalTowards(monsterStat.alertPatrolTargetHeight, monsterStat.descendSpeed);
            }
        }

        private void TickCheckpoint()
        {
            Transform checkpoint = blackboard.DesignatedCheckpoint;
            if (checkpoint == null) return;

            float distance = Vector3.Distance(transform.position, checkpoint.position);
            if (distance > monsterStat.orbitRadius + monsterStat.arriveRadius)
            {
                SetDominant(AtomicTask.HorizontalMove);
                PlanarSeekWorld(checkpoint.position, monsterStat.moveSpeed);
                return;
            }

            if (Time.time < checkpointOrbitUntil)
            {
                SetDominant(AtomicTask.Orbit);
                AccumulateOrbitAround(checkpoint, true);
                return;
            }

            SetDominant(AtomicTask.Hover);
            AccumulateHoverDrift(monsterStat.hoverRadius * 0.6f);
        }

        private void TickEnergyAvoid()
        {
            SetDominant(AtomicTask.HorizontalMove);
            if (blackboard.CurrentPositionEnergy >= monsterStat.energyAvoidDashTrigger)
            {
                QueueDash(-transform.forward);
            }

            Vector3 direction = Quaternion.Euler(0f, energyAvoidTurnSign * Random.Range(60f, 90f), 0f) * transform.forward;
            SetLook(direction);
            AccumulatePlanar(transform.forward * monsterStat.moveSpeed * Time.deltaTime);
        }

        private void TickInterfere(AITargetable target)
        {
            SetDominant(AtomicTask.Orbit);
            AccumulateOrbitAround(target.transform, true);

            if (Time.time <= interfereUntil && Time.time >= nextStrafeTime && Random.value < monsterStat.strafeChance)
            {
                nextStrafeTime = Time.time + monsterStat.strafeCooldown;
                QueueStrafe(Random.value > 0.5f ? transform.right : -transform.right);
            }
        }

        private void TickAssault(AITargetable target)
        {
            if (Time.time < postAttackBackoffUntil)
            {
                SetDominant(AtomicTask.Dash);
                AccumulatePlanar(-transform.forward * monsterStat.postAttackBackoffSpeed * Time.deltaTime);
                SetLook(target.transform.position - transform.position);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            float attackDistance = Mathf.Max(monsterStat.combatAttackDistance, 0.1f);

            if (distance > attackDistance)
            {
                SetDominant(AtomicTask.HorizontalMove);
                PlanarSeekWorld(target.transform.position, monsterStat.moveSpeed);
                SetLook(target.transform.position - transform.position);

                if (Time.time >= nextDashTime)
                {
                    nextDashTime = Time.time + monsterStat.dashCooldown;
                    QueueDash(GetPlanarDirection(target.transform.position - transform.position));
                }
            }
            else
            {
                SetLook(target.transform.position - transform.position);

                if (Time.time < nextAttackTime)
                {
                    TickAttackIntervalManeuver(target);
                }
                else
                {
                    SetDominant(AtomicTask.Attack);
                    nextAttackTime = Time.time + monsterStat.attackInterval;
                    attackHandlerInterface?.PerformAttack(target, monsterStat.baseDamage, monsterStat.aoeRadius, monsterStat.buffId);
                    ApplyPhysicsAoeDamage(target.transform.position);
                    postAttackBackoffUntil = Time.time + monsterStat.postAttackBackoffSeconds;
                }
            }
        }

        private void PlanarSeekTransform(Transform target, float speed)
        {
            if (target == null) return;
            PlanarSeekWorld(target.position, speed);
        }

        private void PlanarSeekWorld(Vector3 worldTarget, float speed)
        {
            Vector3 direction = GetPlanarDirection(worldTarget - transform.position);
            if (direction.sqrMagnitude <= 0.000001f) return;
            SetLook(direction);
            AccumulatePlanar(direction * speed * Time.deltaTime);
        }

        private void AccumulateOrbitAround(Transform center, bool addVerticalNoise)
        {
            if (center == null) return;

            orbitAngle += monsterStat.orbitAngularSpeed * Time.deltaTime;
            Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * monsterStat.orbitRadius;
            Vector3 point = center.position + offset;
            if (addVerticalNoise)
            {
                point.y += Mathf.Sin(Time.time * monsterStat.orbitVerticalSpeed) * monsterStat.orbitVerticalAmplitude;
            }

            Vector3 toPoint = point - transform.position;
            Vector3 planar = Vector3.ProjectOnPlane(toPoint, Vector3.up);
            if (planar.sqrMagnitude <= 0.000001f) return;

            Vector3 dir = planar.normalized;
            SetLook(monsterStat.faceCenter ? center.position - transform.position : dir);
            AccumulatePlanar(dir * monsterStat.moveSpeed * Time.deltaTime);

            float vertical = toPoint.y;
            if (Mathf.Abs(vertical) > 0.0001f)
            {
                float step = Mathf.Sign(vertical) * Mathf.Min(Mathf.Abs(vertical), monsterStat.ascendSpeed * Time.deltaTime);
                AccumulateVertical(step);
            }

            if (addVerticalNoise && monsterStat.aerialMode)
            {
                TickOrbitVerticalBurst();
            }
        }

        private void TickOrbitVerticalBurst()
        {
            if (monsterStat.orbitVerticalBurstChance <= 0f || monsterStat.orbitVerticalBurstDistance <= 0f) return;

            if (Time.time >= nextOrbitVerticalBurstTime)
            {
                nextOrbitVerticalBurstTime = Time.time + monsterStat.orbitVerticalBurstCooldown;
                if (Random.value < monsterStat.orbitVerticalBurstChance)
                {
                    orbitVerticalBurstSign = Random.value > 0.5f ? 1f : -1f;
                    orbitVerticalBurstDuration = Mathf.Max(
                        0.05f,
                        monsterStat.orbitVerticalBurstDistance / Mathf.Max(0.01f, monsterStat.orbitVerticalBurstSpeed));
                    orbitVerticalBurstStartTime = Time.time;
                    orbitVerticalBurstUntil = orbitVerticalBurstStartTime + orbitVerticalBurstDuration;
                }
            }

            if (Time.time < orbitVerticalBurstUntil)
            {
                float normalized = orbitVerticalBurstDuration <= 0.0001f
                    ? 1f
                    : Mathf.Clamp01((Time.time - orbitVerticalBurstStartTime) / orbitVerticalBurstDuration);
                float curveFactor = monsterStat.orbitVerticalBurstSpeedCurve != null
                    ? Mathf.Max(0f, monsterStat.orbitVerticalBurstSpeedCurve.Evaluate(normalized))
                    : 1f;
                SetDominant(orbitVerticalBurstSign > 0f ? AtomicTask.Ascend : AtomicTask.Descend);
                AccumulateVertical(orbitVerticalBurstSign * monsterStat.orbitVerticalBurstSpeed * curveFactor * Time.deltaTime);
            }
        }

        private void AccumulateVerticalTowards(float targetHeight, float speed)
        {
            float delta = targetHeight - transform.position.y;
            if (Mathf.Abs(delta) < 0.01f) return;

            float step = Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), speed * Time.deltaTime);
            AccumulateVertical(step);
        }

        private void AccumulateHoverDrift(float radiusScale)
        {
            float r = Mathf.Max(0.05f, radiusScale);
            Vector3 hoverPlanar = new Vector3(
                Mathf.Sin(Time.time * monsterStat.hoverSpeed) * r,
                0f,
                Mathf.Cos(Time.time * monsterStat.hoverSpeed) * r);
            Vector3 vertical = Vector3.up * Mathf.Sin(Time.time * monsterStat.hoverVerticalSpeed) * monsterStat.hoverVerticalAmplitude;
            AccumulatePlanar((hoverPlanar + hoverSeedOffset * 0.05f) * Time.deltaTime);
            AccumulateVertical(vertical.y * Time.deltaTime);
        }

        private void QueueDash(Vector3 direction)
        {
            dashBurst.Start(direction, monsterStat.dashDistance, monsterStat.moveSpeed * monsterStat.dashSpeedMultiplier, monsterStat.dashAccelTime, monsterStat.dashDecelTime, monsterStat.dashSpeedCurve);
        }

        private void QueueStrafe(Vector3 direction)
        {
            strafeBurst.Start(direction, monsterStat.strafeDistance, monsterStat.strafeSpeed, monsterStat.strafeAccelTime, monsterStat.strafeDecelTime, monsterStat.strafeSpeedCurve);
        }

        private static Vector3 GetPlanarDirection(Vector3 worldDirection)
        {
            return Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
        }

        private void ConsumeBurstMovement()
        {
            ConsumeSingleBurst(ref dashBurst, AtomicTask.Dash);
            ConsumeSingleBurst(ref strafeBurst, AtomicTask.Strafe);
        }

        private void ConsumeSingleBurst(ref BurstMotion burst, AtomicTask task)
        {
            if (!burst.active) return;
            SetDominant(task);

            burst.elapsed += Time.deltaTime;
            float accelFactor = burst.accelTime <= 0.0001f ? 1f : Mathf.Clamp01(burst.elapsed / burst.accelTime);
            float decelFactor = burst.decelTime <= 0.0001f ? 1f : Mathf.Clamp01((burst.totalTime - burst.elapsed) / burst.decelTime);
            float speedFactor = Mathf.Min(accelFactor, decelFactor);
            if (burst.speedCurve != null && burst.speedCurve.length > 0)
            {
                float normalized = burst.totalTime <= 0.0001f ? 1f : Mathf.Clamp01(burst.elapsed / burst.totalTime);
                speedFactor *= Mathf.Max(0f, burst.speedCurve.Evaluate(normalized));
            }
            if (speedFactor <= 0.01f) speedFactor = 0.01f;

            float step = Mathf.Min(burst.remainingDistance, burst.maxSpeed * speedFactor * Time.deltaTime);
            AccumulatePlanar(burst.direction * step);
            burst.remainingDistance -= step;
            if (burst.remainingDistance <= 0.001f) burst.active = false;
        }

        private void DetectMethodTransition()
        {
            bool changed =
                selector.CurrentRoot != lastRoot ||
                selector.CurrentIdleMethod != lastIdleMethod ||
                selector.CurrentCombatMethod != lastCombatMethod;

            if (!changed) return;

            methodEnterTime = Time.time;
            if (selector.CurrentIdleMethod == IdleMethod.Checkpoint)
            {
                checkpointOrbitUntil = Time.time + monsterStat.checkpointOrbitDuration;
            }
            if (selector.CurrentIdleMethod == IdleMethod.AlertPatrol)
            {
                alertDescendUntil = Time.time + monsterStat.alertDescendHoldSeconds;
            }
            if (selector.CurrentIdleMethod == IdleMethod.EnergyAvoid)
            {
                energyAvoidTurnSign = Random.value > 0.5f ? 1f : -1f;
            }
            if (selector.CurrentCombatMethod == CombatMethod.Interfere)
            {
                interfereUntil = Time.time + monsterStat.interfereDuration;
            }
            if (selector.CurrentCombatMethod == CombatMethod.Assault)
            {
                nextAttackIntervalManeuverTime = Time.time;
            }

            if (lastRoot == RootBehavior.Combat && selector.CurrentRoot != RootBehavior.Combat && monsterStat.aerialMode)
            {
                pendingAscendAfterCombat = true;
            }

            lastRoot = selector.CurrentRoot;
            lastIdleMethod = selector.CurrentIdleMethod;
            lastCombatMethod = selector.CurrentCombatMethod;
        }

        private void MaintainCruiseHeight()
        {
            if (pendingAscendAfterCombat) return;
            if (Time.time < orbitVerticalBurstUntil) return;
            AccumulateVerticalTowards(monsterStat.cruiseHeight, monsterStat.ascendSpeed);
        }

        private void ApplyPostCombatAscendVertical()
        {
            if (!pendingAscendAfterCombat || !monsterStat.aerialMode) return;

            float delta = monsterStat.cruiseHeight - transform.position.y;
            if (delta <= 0.02f)
            {
                pendingAscendAfterCombat = false;
                return;
            }

            SetDominant(AtomicTask.Ascend);
            AccumulateVerticalTowards(monsterStat.cruiseHeight, monsterStat.ascendSpeed);
        }

        private void TickAttackIntervalManeuver(AITargetable target)
        {
            SetDominant(AtomicTask.Strafe);

            if (Time.time >= nextAttackIntervalManeuverTime)
            {
                nextAttackIntervalManeuverTime = Time.time + monsterStat.attackIntervalManeuverCooldown;

                if (Random.value < monsterStat.attackIntervalStrafeChance)
                {
                    QueueStrafe(Random.value > 0.5f ? transform.right : -transform.right);
                }

                if (Random.value < monsterStat.attackIntervalVerticalChance)
                {
                    attackIntervalVerticalSign = Random.value > 0.5f ? 1f : -1f;
                    attackIntervalVerticalUntil = Time.time + Mathf.Max(
                        0.05f,
                        monsterStat.attackIntervalVerticalDistance / Mathf.Max(0.01f, monsterStat.attackIntervalVerticalSpeed));
                }
            }

            if (Time.time < attackIntervalVerticalUntil)
            {
                SetDominant(attackIntervalVerticalSign > 0f ? AtomicTask.Ascend : AtomicTask.Descend);
                AccumulateVertical(attackIntervalVerticalSign * monsterStat.attackIntervalVerticalSpeed * Time.deltaTime);
            }

            SetLook(target.transform.position - transform.position);
        }

        private void ApplyPhysicsAoeDamage(Vector3 center)
        {
            if (!monsterStat.usePhysicsAoeDamage || monsterStat.aoeRadius <= 0f) return;

            Collider[] hits = Physics.OverlapSphere(center, monsterStat.aoeRadius, monsterStat.attackTargetMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var targetable = hits[i].GetComponentInParent<AITargetable>();
                if (targetable == null || targetable.gameObject == gameObject) continue;
                if (blackboard.HateTarget != null && targetable != blackboard.HateTarget) continue;

                var receivers = targetable.GetComponentsInParent<MonoBehaviour>(true);
                for (int r = 0; r < receivers.Length; r++)
                {
                    if (receivers[r] is IAIHurtReceiver hurtReceiver)
                    {
                        hurtReceiver.ReceiveAIDamage(monsterStat.baseDamage, monsterStat.buffId, this);
                        break;
                    }
                }
            }
        }
    }
}
