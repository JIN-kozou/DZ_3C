using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using DZ_3C.AI.Perception;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class MonsterAICharacterDriver : MonoBehaviour
    {
        [SerializeField] private MonsterStatConfigSO monsterStat;
        [SerializeField] private AIConfigSO aiConfig;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private HTNMethodSelector selector;
        [SerializeField] private MonsterCharacter character;
        [SerializeField] private MonoBehaviour attackHandler;
        [SerializeField] private EnemyAudio enemyAudio;

        [Header("Laser attack visual (prefab)")]
        [Tooltip("若为 true 且指定了 Prefab，则在攻击时沿射线方向实例化视觉并作为本物体子级，寿命结束后销毁。")]
        [SerializeField] private bool showAttackRayVisual = true;
        [SerializeField] private GameObject laserAttackVisualPrefab;
        [Tooltip("可选。指定时在此 Transform 的世界位置生成；否则使用与射线检测相同的 origin（见 MonsterStat 的 attackRayOriginYOffset）。")]
        [SerializeField] private Transform laserAttackVisualSpawnPoint;
        [Min(0.01f)]
        [SerializeField] private float laserAttackVisualLifetimeSeconds = 0.12f;

        private IMonsterAttack attackHandlerInterface;
        private MonsterHurtReceiver _hurtReceiver;

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

        private float nextAttackTime;
        [SerializeField, Min(0.05f)] private float movementAudioIntervalSeconds = 6f;
        private float nextMovementAudioTime;
        [SerializeField, Min(0.1f)] private float patrolSonarIntervalSeconds = 3f;
        private float nextPatrolSonarTime;
        /// <summary>Assault 战前悬停结束时刻（与 Time.time 比较）；负值表示未进入悬停。</summary>
        private float assaultPreAttackHoverEndTime = -1f;
        /// <summary>Assault 射线射出后的悬停结束时刻；负值表示未在射后悬停中。</summary>
        private float assaultPostRayHoverEndTime = -1f;
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
        /// <summary>本帧 Assault 射后悬停：仅转向，不产生任何主动位移。</summary>
        private bool _frameAssaultPostRayHoverLock;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
            if (character == null) character = GetComponent<MonsterCharacter>();
            if (enemyAudio == null) enemyAudio = GetComponent<EnemyAudio>();
            attackHandlerInterface = attackHandler as IMonsterAttack;
            _hurtReceiver = GetComponent<MonsterHurtReceiver>();

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
            if (_hurtReceiver != null && _hurtReceiver.IsDead)
            {
                return;
            }

            if (monsterStat == null || blackboard == null || selector == null || character == null) return;

            BeginFrame();

            _frameAssaultPostRayHoverLock =
                assaultPostRayHoverEndTime > 0f
                && Time.time < assaultPostRayHoverEndTime
                && selector.CurrentRoot == RootBehavior.Combat
                && selector.CurrentCombatMethod == CombatMethod.Assault;

            DetectMethodTransition();
            if (!_frameAssaultPostRayHoverLock)
            {
                ConsumeBurstMovement();
            }

            if (!_frameAssaultPostRayHoverLock)
            {
                ApplyPostCombatAscendVertical();
            }

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

            if (monsterStat.aerialMode && !_frameAssaultPostRayHoverLock)
            {
                MaintainCruiseHeight();
            }

            TickMovementAudio();
            TickPatrolSonarAudio();
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
            if (!_frameAssaultPostRayHoverLock)
            {
                Vector3 planar = Vector3.ProjectOnPlane(framePlanarDelta, Vector3.up);
                character.MoveBy(planar + Vector3.up * frameVerticalDelta);

                EnforceAerialFloorConstraint();
                EnforcePlayerHorizontalStandoff();
            }

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

        private void TickMovementAudio()
        {
            if (enemyAudio == null || Time.time < nextMovementAudioTime)
            {
                return;
            }

            bool isMovingTask =
                currentAtomicTask == AtomicTask.HorizontalMove ||
                currentAtomicTask == AtomicTask.Orbit ||
                currentAtomicTask == AtomicTask.Dash ||
                currentAtomicTask == AtomicTask.Strafe ||
                currentAtomicTask == AtomicTask.Ascend ||
                currentAtomicTask == AtomicTask.Descend;

            if (!isMovingTask)
            {
                return;
            }

            nextMovementAudioTime = Time.time + movementAudioIntervalSeconds;
            enemyAudio.PlayMovement();
        }

        private void TickPatrolSonarAudio()
        {
            if (enemyAudio == null || Time.time < nextPatrolSonarTime)
            {
                return;
            }

            if (selector.CurrentRoot != RootBehavior.Idle || selector.CurrentIdleMethod != IdleMethod.Patrol)
            {
                return;
            }

            nextPatrolSonarTime = Time.time + patrolSonarIntervalSeconds;
            enemyAudio.PlayPatrolSonar();
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
            if (target == null)
            {
                assaultPostRayHoverEndTime = -1f;
                return;
            }

            if (selector.CurrentCombatMethod == CombatMethod.Interfere)
            {
                assaultPostRayHoverEndTime = -1f;
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
            if (target == null)
            {
                SetDominant(AtomicTask.Hover);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > monsterStat.orbitRadius + monsterStat.arriveRadius)
            {
                SetDominant(AtomicTask.HorizontalMove);
                PlanarSeekWorldWithOptionalEnergyAvoid(target.position, monsterStat.moveSpeed);
                return;
            }

            SetDominant(AtomicTask.Orbit);
            AccumulateOrbitAround(target, true);
        }

        private void TickAlertPatrol()
        {
            if (!blackboard.HasHeardFocus) return;

            Vector3 targetPos = blackboard.HeardFocusWorldPosition;
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
            float panicMult = Mathf.Max(0.1f, monsterStat.energyAvoidPanicMoveSpeedMultiplier);
            float moveSpeed = monsterStat.moveSpeed * panicMult;

            if (aiConfig == null)
            {
                if (blackboard.CurrentPositionEnergy >= monsterStat.energyAvoidDashTrigger && Time.time >= nextDashTime)
                {
                    nextDashTime = Time.time + monsterStat.dashCooldown;
                    QueueDash(-transform.forward);
                }

                Vector3 direction = Quaternion.Euler(0f, energyAvoidTurnSign * Random.Range(60f, 90f), 0f) * transform.forward;
                SetLook(direction);
                AccumulatePlanar(transform.forward * moveSpeed * Time.deltaTime);
                return;
            }

            Vector3 repelRaw = EnergyPerceptor.ComputePlanarEnergyRepel(transform.position, blackboard.EnergyTargets, aiConfig);
            Vector3 repelPlanar = Vector3.ProjectOnPlane(repelRaw, Vector3.up);

            Vector3 moveDir;
            if (repelPlanar.sqrMagnitude > 0.0001f)
            {
                moveDir = repelPlanar.normalized;
            }
            else
            {
                moveDir = Quaternion.Euler(0f, energyAvoidTurnSign * Random.Range(60f, 90f), 0f) * transform.forward;
                moveDir = GetPlanarDirection(moveDir);
                if (moveDir.sqrMagnitude <= 0.000001f) return;
            }

            if (blackboard.CurrentPositionEnergy >= monsterStat.energyAvoidDashTrigger && Time.time >= nextDashTime)
            {
                nextDashTime = Time.time + monsterStat.dashCooldown;
                QueueDash(moveDir);
            }

            SetLook(moveDir);
            AccumulatePlanar(moveDir * moveSpeed * Time.deltaTime);
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
            if (assaultPostRayHoverEndTime > 0f && Time.time < assaultPostRayHoverEndTime)
            {
                SetDominant(AtomicTask.Hover);
                SetLook(target.transform.position - transform.position);
                return;
            }

            if (Time.time >= assaultPostRayHoverEndTime && assaultPostRayHoverEndTime > 0f)
            {
                assaultPostRayHoverEndTime = -1f;
            }

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
                assaultPreAttackHoverEndTime = -1f;
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
                    assaultPreAttackHoverEndTime = -1f;
                    TickAttackIntervalManeuver(target);
                }
                else
                {
                    if (monsterStat.assaultPreAttackHoverSeconds > 0.0001f)
                    {
                        if (assaultPreAttackHoverEndTime < 0f)
                        {
                            assaultPreAttackHoverEndTime = Time.time + monsterStat.assaultPreAttackHoverSeconds;
                        }

                        if (Time.time < assaultPreAttackHoverEndTime)
                        {
                            SetDominant(AtomicTask.Hover);
                            AccumulateHoverDrift(monsterStat.hoverRadius);
                            return;
                        }
                    }

                    assaultPreAttackHoverEndTime = -1f;
                    SetDominant(AtomicTask.Attack);
                    nextAttackTime = Time.time + monsterStat.attackInterval;
                    enemyAudio?.PlayLaserAttack();
                    attackHandlerInterface?.PerformAttack(target, monsterStat.baseDamage, monsterStat.aoeRadius, monsterStat.buffId);
                    TryApplyAttackRayDamageToPlayer(target);

                    float postHover = Mathf.Max(0f, monsterStat.attackRayPostHoverSeconds);
                    if (postHover > 0.0001f)
                    {
                        dashBurst.active = false;
                        strafeBurst.active = false;
                        assaultPostRayHoverEndTime = Time.time + postHover;
                        postAttackBackoffUntil = assaultPostRayHoverEndTime + monsterStat.postAttackBackoffSeconds;
                    }
                    else
                    {
                        assaultPostRayHoverEndTime = -1f;
                        postAttackBackoffUntil = Time.time + monsterStat.postAttackBackoffSeconds;
                    }
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

        private void PlanarSeekWorldWithOptionalEnergyAvoid(Vector3 worldTarget, float speed)
        {
            if (aiConfig == null || blackboard.CurrentPositionEnergy <= aiConfig.energyMinForAvoid)
            {
                PlanarSeekWorld(worldTarget, speed);
                return;
            }

            float panic = Mathf.Max(aiConfig.energyPanicThreshold, aiConfig.energyMinForAvoid + 0.01f);
            Vector3 seekDir = GetPlanarDirection(worldTarget - transform.position);
            if (seekDir.sqrMagnitude <= 0.000001f)
            {
                PlanarSeekWorld(worldTarget, speed);
                return;
            }

            Vector3 repelRaw = EnergyPerceptor.ComputePlanarEnergyRepel(transform.position, blackboard.EnergyTargets, aiConfig);
            Vector3 repelPlanar = Vector3.ProjectOnPlane(repelRaw, Vector3.up);
            Vector3 repelDir = repelPlanar.sqrMagnitude > 0.0001f ? repelPlanar.normalized : Vector3.zero;

            float energyBlendT = Mathf.Clamp01(Mathf.InverseLerp(aiConfig.energyMinForAvoid, panic, blackboard.CurrentPositionEnergy));
            float repelWeight = energyBlendT * aiConfig.energyPatrolRepelBlendMax;

            Vector3 combined = seekDir + repelDir * repelWeight;
            if (combined.sqrMagnitude <= 0.000001f)
            {
                PlanarSeekWorld(worldTarget, speed);
                return;
            }

            Vector3 dir = combined.normalized;
            SetLook(dir);
            AccumulatePlanar(dir * speed * Time.deltaTime);
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

            character.ResetTurnAssistState();

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
                if (lastCombatMethod != CombatMethod.Interfere)
                {
                    enemyAudio?.PlayTargetAcquisitionHowl();
                }
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

        /// <summary>
        /// 朝仇恨目标瞄准点（目标位置 + <see cref="MonsterStatConfigSO.attackRayTargetYOffset"/>）方向打射线；
        /// 若命中玩家（且与仇恨目标一致）则造成伤害并传入 buffId（由 <see cref="IAIHurtReceiver"/> 处理）。
        /// </summary>
        private void TryApplyAttackRayDamageToPlayer(AITargetable target)
        {
            if (monsterStat == null || target == null)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up * monsterStat.attackRayOriginYOffset;
            Vector3 aimPoint = target.transform.position + Vector3.up * monsterStat.attackRayTargetYOffset;
            Vector3 toTarget = aimPoint - origin;
            float maxDist = Mathf.Max(0.5f, monsterStat.attackRayMaxDistance);
            if (toTarget.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            int mask = monsterStat.attackTargetMask.value != 0
                ? monsterStat.attackTargetMask
                : Physics.DefaultRaycastLayers;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Collide))
            {
                var hitTargetable = hit.collider.GetComponentInParent<AITargetable>();
                if (hitTargetable != null && hitTargetable.IsPlayer && hitTargetable.gameObject != gameObject)
                {
                    if (blackboard.HateTarget == null || hitTargetable == blackboard.HateTarget)
                    {
                        var receivers = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
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

            SpawnLaserAttackVisual(origin, direction);
        }

        /// <summary>
        /// 沿攻击射线方向在指定生成点（或射线 origin）实例化 Prefab，父级为本怪物，延迟销毁。
        /// </summary>
        private void SpawnLaserAttackVisual(Vector3 rayOrigin, Vector3 direction)
        {
            if (!showAttackRayVisual || laserAttackVisualPrefab == null)
            {
                return;
            }

            Vector3 worldPos = laserAttackVisualSpawnPoint != null
                ? laserAttackVisualSpawnPoint.position
                : rayOrigin;

            Quaternion rotation = GetLaserVisualRotation(direction);
            GameObject instance = Instantiate(laserAttackVisualPrefab, worldPos, rotation, transform);
            Destroy(instance, Mathf.Max(0.01f, laserAttackVisualLifetimeSeconds));
        }

        private Quaternion GetLaserVisualRotation(Vector3 direction)
        {
            Vector3 dir = direction.sqrMagnitude > 0.000001f ? direction.normalized : transform.forward;
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.98f)
            {
                up = transform.up;
                if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.98f)
                {
                    up = Vector3.forward;
                }
            }

            return Quaternion.LookRotation(dir, up);
        }
    }
}
