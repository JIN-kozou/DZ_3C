using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 逆重核心栈的中央协调器。
    /// 持有：
    ///   - cores：当前玩家身上的核心列表（顺序：cores[0]=最内侧 / 紧贴锚，cores[N-1]=最外侧）
    ///   - anchor：锚（Player.ReusableData.health 的轻代理）
    ///   - registry：场上所有阵列
    ///   - config：数值配置
    /// 帧内顺序由 UnifiedTick(dt) 显式控制：
    ///   1. TickFlow（外→内血量内流）
    ///   2. TickBreath（呼吸掉血——也是从最外侧非空池子开始）
    ///   3. 视野半径变化检测 → 触发 OnViewRadiusChanged
    /// 注：buff 系统每秒发的回血由 Player.RecoverResource 路由进来（IReverseRecoverTarget），
    ///     入口在 Player.Update -> BuffSystem.Tick；BuffSystem.Tick 在本组件 UnifiedTick 之前执行，
    ///     所以同一帧 buff 给的血会先到达，再被 TickFlow 内化。
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public class ReverseCoreStack : MonoBehaviour, IReverseRecoverTarget
    {
        [Header("References (引用)")]
        [SerializeField] private ReverseConfig config;
        [SerializeField] private ReverseAnchor anchor;
        [SerializeField] private ReverseArrayRegistry registry;
        [SerializeField] private Player player;

        [Tooltip("是否在 Awake 自动初始化 maxCoreCount 个满血核心。关掉后可由外部代码自定义初始状态。")]
        [SerializeField] private bool autoInitOnAwake = true;

        [Tooltip("是否在 Update 里自动调用 UnifiedTick。默认 true。如果想由外部 Player 驱动可关掉。")]
        [SerializeField] private bool autoTick = true;

        private readonly List<ReverseCore> cores = new List<ReverseCore>();
        private float lastViewRadius;
        private float invincibleSecondsRemaining;
        private bool isDying;

        // ---------- 事件 ----------

        /// <summary>视野半径变化（newRadius）。每帧检测，仅在数值跳变时触发。</summary>
        public event Action<float> OnViewRadiusChanged;

        /// <summary>玩家死亡（锚归零，无法找到复活点 / 找到复活点的瞬间都会先发这个）。</summary>
        public event Action OnDeath;

        /// <summary>找到复活点并复活（newPosition）。</summary>
        public event Action<Vector3> OnRespawned;

        /// <summary>没有任何阵列，复活失败（GameOver）。</summary>
        public event Action OnGameOver;

        /// <summary>核心列表内容变化（部署 / 收回 / 数值流动均可能触发）。仅作 UI 刷新用。</summary>
        public event Action OnCoresChanged;

        // ---------- 暴露给输入 / 调试 / 视野 ----------

        public IReadOnlyList<ReverseCore> Cores => cores;
        public ReverseAnchor Anchor => anchor;
        public ReverseConfig Config => config;
        public ReverseArrayRegistry Registry => registry;
        public Player Player => player;
        public float CurrentViewRadius => lastViewRadius;
        public bool IsInvincible => invincibleSecondsRemaining > 0f;

        /// <summary>
        /// 清除复活后的无敌时间。仅用于测试/调试（例如连续按 F9 验证死亡流程）。
        /// </summary>
        public void ClearRespawnInvincibility()
        {
            invincibleSecondsRemaining = 0f;
        }

        public int FullCoreCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < cores.Count; i++)
                {
                    if (cores[i] != null && cores[i].IsFull) n++;
                }
                return n;
            }
        }

        // ---------- 生命周期 ----------

        private void Awake()
        {
            if (anchor == null) anchor = GetComponent<ReverseAnchor>();
            if (player == null) player = GetComponent<Player>();
            if (registry == null)
            {
                registry = GetComponentInChildren<ReverseArrayRegistry>();
                if (registry == null) registry = FindObjectOfType<ReverseArrayRegistry>();
            }
            if (registry != null && config != null) registry.SetConfig(config);
            if (autoInitOnAwake) InitializeCoresFull();
        }

        private void OnEnable()
        {
            if (anchor != null) anchor.OnAnchorDied += HandleAnchorDied;
        }

        private void OnDisable()
        {
            if (anchor != null) anchor.OnAnchorDied -= HandleAnchorDied;
        }

        private void Start()
        {
            lastViewRadius = ComputeViewRadius();
            OnViewRadiusChanged?.Invoke(lastViewRadius);
        }

        private void Update()
        {
            if (autoTick) UnifiedTick(Time.deltaTime);
        }

        // ---------- 初始化 ----------

        public void InitializeCoresFull()
        {
            cores.Clear();
            int target = config != null ? Mathf.Max(0, config.maxCoreCount) : 3;
            float maxHp = config != null ? config.coreMaxHealth : 20f;
            float eps = config != null ? config.fullnessEpsilon : 0.001f;
            for (int i = 0; i < target; i++)
            {
                cores.Add(new ReverseCore(maxHp, eps));
            }
            OnCoresChanged?.Invoke();
        }

        // ---------- 帧调度入口 ----------

        public void UnifiedTick(float dt)
        {
            if (dt <= 0f) return;

            if (invincibleSecondsRemaining > 0f)
            {
                invincibleSecondsRemaining = Mathf.Max(0f, invincibleSecondsRemaining - dt);
            }

            TickFlow(dt);
            if (!IsInvincible)
            {
                TickBreath(dt);
            }
            DetectViewRadiusChange();
        }

        // ---------- 内流（Bucket Brigade 模型） ----------

        /// <summary>
        /// 血量从外侧流向内侧。每一个"内侧 layer"（锚或某个 core）若有缺口，
        /// 都会从更外侧的第一个非空 layer 拉血。空池子充当"导管"——它会被填充，但同帧
        /// 也会被更内侧 layer 从上面把血抽走（净流量 ≈ 0），整体效果是血逐层往锚走。
        /// </summary>
        public void TickFlow(float dt)
        {
            if (cores.Count == 0) return;
            float ratePerChannel = (config != null ? config.flowRatePerSecond : 0.2f) * dt;
            if (ratePerChannel <= 0f) return;

            // 1) 先填锚：从最近的有 HP 的核心（从 cores[0] 起向外扫）拉血。
            if (anchor != null && anchor.MissingHealth > 0f)
            {
                int srcIndex = FindFirstNonEmptyCoreIndex(0);
                if (srcIndex >= 0)
                {
                    float take = Mathf.Min(ratePerChannel, anchor.MissingHealth, cores[srcIndex].Health);
                    if (take > 0f)
                    {
                        cores[srcIndex].Drain(take);
                        anchor.Fill(take);
                    }
                }
            }

            // 2) 再让每个 core[i] 从更外侧（i+1...N-1）的第一个非空 core 拉血。
            //    顺序：内→外，保证靠内的 layer 先被填，导管效果出现。
            for (int i = 0; i < cores.Count; i++)
            {
                var target = cores[i];
                if (target == null || target.MissingHealth <= 0f) continue;
                int srcIndex = FindFirstNonEmptyCoreIndex(i + 1);
                if (srcIndex < 0) continue;
                float take = Mathf.Min(ratePerChannel, target.MissingHealth, cores[srcIndex].Health);
                if (take > 0f)
                {
                    cores[srcIndex].Drain(take);
                    target.Fill(take);
                }
            }
        }

        /// <summary>从 startIndex（含）向外扫，返回第一个 health > 0 的 cores 索引；找不到返回 -1。</summary>
        private int FindFirstNonEmptyCoreIndex(int startIndex)
        {
            for (int i = Mathf.Max(0, startIndex); i < cores.Count; i++)
            {
                if (cores[i] != null && !cores[i].IsEmpty) return i;
            }
            return -1;
        }

        // ---------- 呼吸掉血 ----------

        public void TickBreath(float dt)
        {
            float dps = config != null ? config.breathDamagePerSecond : 1.33f;
            if (dps <= 0f) return;
            ApplyDamage(dps * dt);
        }

        // ---------- 受伤（外伤或呼吸） ----------

        /// <summary>
        /// 优先扣最外侧非空池子；全部空了才扣锚。
        /// 注意：HP 归零的核心仍留在 cores 里（持久池语义），不会被移除。
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f) return;
            if (IsInvincible) return;
            float remaining = amount;
            for (int i = cores.Count - 1; i >= 0 && remaining > 0f; i--)
            {
                var c = cores[i];
                if (c == null || c.IsEmpty) continue;
                float taken = c.Drain(remaining);
                remaining -= taken;
            }
            if (remaining > 0f && anchor != null)
            {
                anchor.Drain(remaining);
            }
        }

        // ---------- 部署（E 键） ----------

        /// <summary>
        /// 内→外扫描第一个满血核心，从 cores 列表移除并通过 out 返回。
        /// 移除后外侧核心整体内移一格（List.RemoveAt 自然完成）。
        /// </summary>
        public bool TryDeployInnermostFull(out ReverseCore deployed)
        {
            for (int i = 0; i < cores.Count; i++)
            {
                if (cores[i] != null && cores[i].IsFull)
                {
                    deployed = cores[i];
                    cores.RemoveAt(i);
                    OnCoresChanged?.Invoke();
                    return true;
                }
            }
            deployed = null;
            return false;
        }

        // ---------- 收回（Q / 1/2/3 键） ----------

        /// <summary>把一个满血核心追加到 cores 的最外侧（cores[N-1]）。供阵列收回时调用。</summary>
        public void AcceptRetrievedCoreAtOutermost()
        {
            float maxHp = config != null ? config.coreMaxHealth : 20f;
            float eps = config != null ? config.fullnessEpsilon : 0.001f;
            cores.Add(new ReverseCore(maxHp, eps));
            OnCoresChanged?.Invoke();
        }

        // ---------- IReverseRecoverTarget ----------

        /// <summary>
        /// 链式补血：先填锚（即 Player.health），锚满了再依次 cores[0] → cores[N-1]。
        /// 由 Player.RecoverResource(ReverseSystem) 路由进来。
        /// </summary>
        public void RecoverFromInnermost(float amount)
        {
            if (amount <= 0f) return;
            float remaining = amount;
            if (anchor != null)
            {
                remaining -= anchor.Fill(remaining);
                if (remaining <= 0f) { OnCoresChanged?.Invoke(); return; }
            }
            for (int i = 0; i < cores.Count && remaining > 0f; i++)
            {
                if (cores[i] == null) continue;
                remaining -= cores[i].Fill(remaining);
            }
            OnCoresChanged?.Invoke();
        }

        // ---------- 视野 ----------

        public float ComputeViewRadius()
        {
            float baseR = config != null ? config.anchorViewRadius : 5f;
            float bonus = config != null ? config.coreViewBonus : 5f;
            return baseR + FullCoreCount * bonus;
        }

        private void DetectViewRadiusChange()
        {
            float r = ComputeViewRadius();
            if (Mathf.Abs(r - lastViewRadius) > 0.0001f)
            {
                lastViewRadius = r;
                OnViewRadiusChanged?.Invoke(r);
            }
        }

        // ---------- 死亡 / 复活 ----------

        private void HandleAnchorDied()
        {
            if (isDying) return;
            isDying = true;
            OnDeath?.Invoke();
            TryRespawn();
            isDying = false;
        }

        /// <summary>
        /// 与带 <see cref="CharacterController"/> 的角色兼容的瞬移：先禁用 CC 再写 <see cref="Transform.position"/>。
        /// </summary>
        private void TeleportToWorldPosition(Vector3 worldPosition)
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = worldPosition;
                cc.enabled = true;
            }
            else
            {
                transform.position = worldPosition;
            }

            Physics.SyncTransforms();
        }

        private void TryRespawn()
        {
            if (registry == null || registry.DeployedCount == 0)
            {
                OnGameOver?.Invoke();
                return;
            }
            Vector3 pos = transform.position;
            ReverseArray target = registry.FindRespawnTarget(pos);
            if (target == null)
            {
                OnGameOver?.Invoke();
                return;
            }

            // 放回阵列位置（不消耗阵列，不改变核心数量），核心列表槽位补满血、锚补满。
            // CharacterController 会覆盖直接改 transform.position；需短暂禁用再写位置。
            TeleportToWorldPosition(target.transform.position);
            RefillExistingCoresAndAnchorToFull();
            invincibleSecondsRemaining = config != null ? config.respawnInvincibleSeconds : 1.5f;
            OnRespawned?.Invoke(target.transform.position);
        }

        public void RefillExistingCoresAndAnchorToFull()
        {
            if (anchor != null) anchor.RefillToFull();
            if (config == null || config.refillCoresOnRespawn)
            {
                for (int i = 0; i < cores.Count; i++)
                {
                    cores[i]?.RefillToFull();
                }
            }
            OnCoresChanged?.Invoke();
        }
    }
}
