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
        [Tooltip("转向平滑时间（秒）。0 表示不阻尼，仅用 turnSpeed 硬限幅。")]
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
        [Min(0f)] public float alertDescendHoldSeconds = 0.8f;

        [Header("Patrol Idle")]
        [Min(0f)] public float patrolPauseSecondsMin = 0.3f;
        [Min(0f)] public float patrolPauseSecondsMax = 1.2f;
        [Min(0f)] public float checkpointOrbitDuration = 2f;

        [Header("Combat")]
        [Min(0.1f)] public float combatAttackDistance = 2f;
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
        public bool usePhysicsAoeDamage = true;
        public string buffId = string.Empty;

        [Header("Optional Character Numeric")]
        public PlayerNumericConfig playerNumericConfig;
    }
}
