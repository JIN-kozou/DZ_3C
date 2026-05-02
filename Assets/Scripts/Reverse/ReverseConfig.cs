using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 逆重核心栈机制的全部数值配置。
    /// 资产位置：Assets/Resources/Config/Reverse/ReverseConfig.asset。
    /// 所有 Reverse 组件共享同一份配置，方便策划在 Inspector 里调手感。
    /// </summary>
    [CreateAssetMenu(fileName = "ReverseConfig", menuName = "DZ_3C/Reverse/ReverseConfig", order = 0)]
    public class ReverseConfig : ScriptableObject
    {
        // ---------- 基础数量 ----------

        [Header("Stack Capacity (核心栈容量)")]
        [Tooltip("玩家身上同时持有的核心数量上限。也等于场景里允许同时部署的阵列数量上限。默认 3。")]
        [Min(1)] public int maxCoreCount = 3;

        [Tooltip("单个核心的最大血量。三个核心共 60 血，配合 anchorMaxHealth=100 总共 160 血池。")]
        [Min(1f)] public float coreMaxHealth = 20f;

        [Tooltip("锚的最大血量。锚血就是 Player.ReusableData.health.Value 的最大值（与 Player.maxHealth 应保持一致）。默认 100。")]
        [Min(1f)] public float anchorMaxHealth = 100f;

        [Tooltip("启动时校验 anchorMaxHealth 是否与 Player.MaxHealth 一致，不一致打 warning。便于发现 SO 与 Player 数值脱节。")]
        public bool validateAnchorHealthSync = true;

        // ---------- 持续伤害 ----------

        [Header("Continuous Damage (持续伤害)")]
        [Tooltip("呼吸掉血速度（每秒）。默认 1.33 表示满状态 160 血纯呼吸约 2 分钟阵亡。")]
        [Min(0f)] public float breathDamagePerSecond = 1.33f;

        // ---------- 内流速度 ----------

        [Header("Health Flow (血量内流速度)")]
        [Tooltip("血量从外侧流向内侧的最大速率（每秒）。每帧每条相邻链路上的搬运量上限 = flowRatePerSecond * deltaTime。值越大补血越敏感。")]
        [Min(0f)] public float flowRatePerSecond = 0.2f;

        [Tooltip("浮点比较的容差。绝对值 < epsilon 视为 0。判定 IsFull / IsEmpty 时使用，避免帧间抖动。")]
        [Min(0f)] public float fullnessEpsilon = 0.001f;

        // ---------- 部署/收回 ----------

        [Header("Deployment (部署/收回)")]
        [Tooltip("E 部署阵列时，阵列默认携带的能量（仅做展示与远程收回时返还核心血量比例的依据；本期收回直接补满，不用此值）。")]
        [Min(0f)] public float arrayDefaultEnergy = 20f;

        [Tooltip("部署位置相对玩家脚下的偏移。默认 (0, 0, 1) 表示玩家正前方 1m。")]
        public Vector3 deployOffset = new Vector3(0f, 0f, 1f);

        // ---------- 视野 ----------

        [Header("Vision (视野半径)")]
        [Tooltip("锚自带的最小视野半径（米）。即使核心全空，玩家也至少能看见 anchorViewRadius 范围。默认 5。")]
        [Min(0f)] public float anchorViewRadius = 5f;

        [Tooltip("每个 满血 核心额外提供的视野半径（米）。FullCoreCount * coreViewBonus 加在 anchorViewRadius 上。默认 5。")]
        [Min(0f)] public float coreViewBonus = 5f;

        [Tooltip("每个部署到场景的阵列固定视野半径（米）。固定值，不随阵列剩余能量变化。默认 5。")]
        [Min(0f)] public float arrayViewRadius = 5f;

        [Tooltip("玩家视野半径从旧值过渡到新值所需时间（秒）。0 表示瞬间切换。")]
        [Min(0f)] public float viewRadiusTransitionDuration = 0.25f;

        [Tooltip("玩家视野半径过渡曲线。X=归一化时间(0~1)，Y=归一化插值(0~1)。")]
        public AnimationCurve viewRadiusTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ---------- 输入键位 ----------

        [Header("Input Keys (键位)")]
        [Tooltip("E：从最内层往外扫描第一个满血核心，部署为阵列。")]
        public KeyCode deployKey = KeyCode.E;

        [Tooltip("Q：远程拿回最后一次部署的阵列（栈顶 LIFO）。")]
        public KeyCode retrieveLastKey = KeyCode.Q;

        [Tooltip("数字键 1/2/3：远程拿回对应槽位的阵列（按部署顺序的槽位号，槽 1 = 第一次部署的位置）。")]
        public KeyCode[] retrieveBySlotKeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

        // ---------- 复活 ----------

        [Header("Respawn (复活)")]
        [Tooltip("锚血 <= 该阈值视为死亡触发复活流程。0 表示严格归零才死。")]
        [Min(0f)] public float deathThreshold = 0f;

        [Tooltip("复活后身上的核心池是否也补满（默认 true，与设计文档 RefillExistingToFull 一致）。关闭则只补锚血，核心保持死亡前残血/空池。")]
        public bool refillCoresOnRespawn = true;

        [Tooltip("复活后阵列是否仍然保留在场景里（默认 true）。即便玩家死在某阵列上，那个阵列也不会消失。")]
        public bool keepArraysOnRespawn = true;

        [Tooltip("复活后短暂的无敌时间（秒），呼吸/外伤都被跳过。0 表示无无敌期。")]
        [Min(0f)] public float respawnInvincibleSeconds = 1.5f;
    }
}
