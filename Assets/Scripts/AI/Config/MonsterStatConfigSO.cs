using UnityEngine;

namespace DZ_3C.AI.Config
{
    [CreateAssetMenu(menuName = "DZ_3C/AI/Monster Stat Config", fileName = "MonsterStatConfig")]
    public class MonsterStatConfigSO : ScriptableObject
    {
        [Header("Locomotion")]
        [Tooltip("是否为飞行怪物。开启后会关闭 CharacterBase 重力并维持巡航高度。")]
        public bool aerialMode = true;
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float turnSpeed = 540f;
        [Tooltip("转向平滑时间（秒）。大于 0 时在 MonsterCharacter 内用 SmoothDampAngle；0 表示不阻尼，仅用 turnSpeed 限幅。HTN 根行为/子方法切换时会重置角速度辅助量。")]
        [Min(0f)] public float turnSmoothTime = 0.12f;
        [Min(0.05f)] public float arriveRadius = 0.8f;
        [Min(0f)] public float startTurnWindowSeconds = 0.25f;
        [Min(-20f)] public float cruiseHeight = 2.2f;

        [Header("Constraints")]
        [Tooltip("飞行怪专用：约束世界坐标 Y 不低于该值（仅 aerialMode）。")]
        public bool enforceAerialMinWorldHeight = false;
        [Min(-500f)] public float aerialMinWorldHeight = 0.5f;
        [Tooltip("与玩家保持的最小水平距离（米）。0 表示不启用。优先用 Tag 找玩家，找不到则用仇恨目标且 IsPlayer。")]
        [Min(0f)] public float minHorizontalDistanceToPlayer = 0f;
        [Tooltip("用于查找玩家 Transform 的 Tag；留空则仅用仇恨目标（需 IsPlayer）。")]
        public string playerStandoffTag = "Player";

        [Header("Vertical")]
        [Min(0f)] public float ascendSpeed = 2f;
        [Min(0f)] public float descendSpeed = 2f;
        [Min(-20f)] public float alertPatrolTargetHeight = 1.5f;
        [Min(0f)] public float orbitVerticalAmplitude = 0.6f;
        [Min(0f)] public float orbitVerticalSpeed = 1.2f;
        [Min(0f)] public float hoverVerticalAmplitude = 0.3f;
        [Min(0f)] public float hoverVerticalSpeed = 1f;

        [Header("Orbit")]
        [Min(0.1f)] public float orbitRadius = 2.5f;
        [Min(0f)] public float orbitAngularSpeed = 120f;
        public bool faceCenter = true;
        [Min(0f)] public float interfereDuration = 3f;
        [Tooltip("飞行怪环绕时，随机触发上升/下降（与正弦起伏叠加）。地面怪不启用。")]
        [Range(0f, 1f)] public float orbitVerticalBurstChance = 0.35f;
        [Tooltip("每次随机纵向机动的大致位移（米），持续时间为 距离/速度。")]
        [Min(0f)] public float orbitVerticalBurstDistance = 0.75f;
        [Min(0.01f)] public float orbitVerticalBurstSpeed = 2.5f;
        [Tooltip("环绕随机升降的速度倍率曲线，X=归一化时间(0~1)，Y=速度倍率。")]
        public AnimationCurve orbitVerticalBurstSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [Min(0f)] public float orbitVerticalBurstCooldown = 0.45f;

        [Header("Dash")]
        [Min(0.1f)] public float dashDistance = 4f;
        [Min(0.1f)] public float dashSpeedMultiplier = 2.2f;
        [Min(0f)] public float dashAccelTime = 0.15f;
        [Min(0f)] public float dashDecelTime = 0.2f;
        public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Min(0f)] public float dashCooldown = 2f;

        [Header("Strafe")]
        [Min(0.1f)] public float strafeDistance = 2f;
        [Min(0.1f)] public float strafeSpeed = 4f;
        [Min(0f)] public float strafeAccelTime = 0.08f;
        [Min(0f)] public float strafeDecelTime = 0.08f;
        public AnimationCurve strafeSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Min(0f)] public float strafeCooldown = 1.5f;
        [Min(0f)] public float strafeChance = 0.35f;

        [Header("Hover")]
        [Min(0f)] public float hoverRadius = 0.8f;
        [Min(0f)] public float hoverSpeed = 1.5f;
        [Min(0f)] public float energyAvoidDashTrigger = 3f;
        [Tooltip("EnergyAvoid 状态下沿排斥方向移动的移速倍率（快速脱离高能量区）。")]
        [Min(0.1f)] public float energyAvoidPanicMoveSpeedMultiplier = 1.35f;
        [Min(0f)] public float alertDescendHoldSeconds = 0.8f;

        [Header("Patrol Idle")]
        [Tooltip("已不使用：巡逻点旁改为环绕 orbitRadius。保留以免旧资源反序列化丢字段。")]
        [Min(0f)] public float patrolPauseSecondsMin = 0.3f;
        [Tooltip("已不使用：同上。")]
        [Min(0f)] public float patrolPauseSecondsMax = 1.2f;
        [Min(0f)] public float checkpointOrbitDuration = 2f;
        [Tooltip("到达巡逻点（进入环绕带）后，停留并环绕的秒数；之后才标记该点已访问并选下一点。0 表示到达后立即换点。")]
        [Min(0f)] public float patrolPointDwellSeconds = 0f;

        [Header("Combat")]
        [Min(0.1f)] public float combatAttackDistance = 2f;
        [Tooltip("Assault：进入攻击距离且攻击冷却结束后，先悬停该秒数再执行攻击；0 表示不悬停。")]
        [Min(0f)] public float assaultPreAttackHoverSeconds = 0f;
        [Tooltip("Assault 攻击：从怪物指向仇恨目标发射射线，命中玩家则结算伤害与 buffId；最大长度（米）。")]
        [Min(0.5f)] public float attackRayMaxDistance = 40f;
        [Tooltip("射线起点相对 transform.position 的世界向上偏移（米）。")]
        public float attackRayOriginYOffset = 0.35f;
        [Tooltip("射线瞄准点：在仇恨目标 transform.position 上沿世界 Y 轴抬高（米），例如对准胸口/头；与起点偏移独立可调。")]
        [Min(0f)] public float attackRayTargetYOffset = 0.65f;
        [Tooltip("Assault：打出射线后仅悬停（Hover）的时长（秒），之后再进入后撤/攻击间隔机动等；0 表示不额外悬停。")]
        [Min(0f)] public float attackRayPostHoverSeconds = 1f;
        [Min(0f)] public float aoeRadius = 1.5f;
        [Min(0f)] public float baseDamage = 10f;
        [Min(0f)] public float attackInterval = 1f;
        [Min(0f)] public float attackIntervalStrafeChance = 0.7f;
        [Min(0f)] public float attackIntervalVerticalChance = 0.6f;
        [Min(0f)] public float attackIntervalVerticalDistance = 0.8f;
        [Min(0f)] public float attackIntervalVerticalSpeed = 2.5f;
        [Min(0f)] public float attackIntervalManeuverCooldown = 0.25f;
        [Min(0f)] public float postAttackBackoffSeconds = 0.6f;
        [Min(0f)] public float postAttackBackoffSpeed = 4f;
        public LayerMask attackTargetMask = ~0;
        [Tooltip("已废弃：伤害改为射线检测，见 attackRayMaxDistance。保留字段以免旧资源反序列化丢失。")]
        public bool usePhysicsAoeDamage = false;
        public string buffId = string.Empty;

        [Header("Vitality")]
        [Min(1f)] public float maxHealth = 100f;

        [Header("Optional Character Numeric")]
        public PlayerNumericConfig playerNumericConfig;
    }
}
