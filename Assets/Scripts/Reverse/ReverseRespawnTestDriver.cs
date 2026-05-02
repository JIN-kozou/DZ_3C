using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 运行时快捷键扣血，便于测试锚血归零后的死亡 / 复活 / GameOver。
    /// 挂在与 <see cref="ReverseCoreStack"/> 同一物体（玩家）上；伤害走 <see cref="ReverseCoreStack.ApplyDamage"/>，
    /// 会先扣外层核心再扣锚血，与正式受伤一致。
    /// <para><b>重要：</b>复活后 <see cref="ReverseCoreStack"/> 有无敌期，此期间 <see cref="ReverseCoreStack.ApplyDamage"/> 会直接 return，
    /// 表现为按 F9 不扣血、不触发死亡。可勾选「扣血前清除无敌」或按 <see cref="clearInvincibilityKey"/>。</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseRespawnTestDriver : MonoBehaviour
    {
        [SerializeField] private ReverseCoreStack coreStack;

        [Tooltip("关闭后 Update 内不再响应快捷键（仍可用 Context Menu）。")]
        [SerializeField] private bool listenKeys = true;

        [Header("快捷键")]
        [SerializeField] private KeyCode stepDamageKey = KeyCode.Minus;
        [SerializeField] private KeyCode instantKillKey = KeyCode.F9;
        [Tooltip("清除复活无敌，便于紧接着再测死亡。")]
        [SerializeField] private KeyCode clearInvincibilityKey = KeyCode.F8;

        [Header("扣血量")]
        [SerializeField] private float stepDamage = 20f;
        [Tooltip("Shift + 步进键时倍率。")]
        [SerializeField] private float shiftMultiplier = 5f;

        [Tooltip("一键清空：对栈造成超大伤害（核心→锚），用于快速触发死亡。")]
        [SerializeField] private float instantKillDamage = 100000f;

        [Header("无敌与日志")]
        [Tooltip("在步进 / F9 扣血前自动清除复活无敌，否则无敌期内 ApplyDamage 无效。")]
        [SerializeField] private bool clearInvincibilityBeforeTestDamage = true;

        [Tooltip("向 Console 打印 Death / Respawn / GameOver 以及部署阵列数量（需本组件启用）。")]
        [SerializeField] private bool logReverseEvents = true;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
        }

        private void OnEnable()
        {
            if (coreStack == null || !logReverseEvents) return;
            coreStack.OnDeath += LogDeath;
            coreStack.OnRespawned += LogRespawn;
            coreStack.OnGameOver += LogGameOver;
        }

        private void OnDisable()
        {
            if (coreStack == null) return;
            coreStack.OnDeath -= LogDeath;
            coreStack.OnRespawned -= LogRespawn;
            coreStack.OnGameOver -= LogGameOver;
        }

        private void LogDeath()
        {
            Debug.Log("[ReverseRespawnTestDriver] OnDeath（锚血触顶死亡阈值）", this);
        }

        private void LogRespawn(Vector3 pos)
        {
            int n = coreStack != null && coreStack.Registry != null ? coreStack.Registry.DeployedCount : -1;
            Debug.Log($"[ReverseRespawnTestDriver] OnRespawned @ {pos} | registry.DeployedCount={n}", this);
        }

        private void LogGameOver()
        {
            var reg = coreStack != null ? coreStack.Registry : null;
            Debug.LogWarning(
                "[ReverseRespawnTestDriver] OnGameOver：无可用复活阵列（registry 为空，或 DeployedCount==0，或 FindRespawnTarget 为 null）。"
                + (reg == null ? " Registry=null." : $" DeployedCount={reg.DeployedCount}."),
                this);
        }

        private void Update()
        {
            if (!listenKeys || coreStack == null) return;

            if (Input.GetKeyDown(clearInvincibilityKey))
            {
                coreStack.ClearRespawnInvincibility();
                Debug.Log("[ReverseRespawnTestDriver] 已清除复活无敌（F8）", this);
            }

            float amount = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                ? stepDamage * shiftMultiplier
                : stepDamage;

            if (Input.GetKeyDown(stepDamageKey))
            {
                PrepareDamage();
                coreStack.ApplyDamage(amount);
                LogIfDamageSkipped();
            }

            if (Input.GetKeyDown(instantKillKey))
            {
                PrepareDamage();
                coreStack.ApplyDamage(instantKillDamage);
                LogIfDamageSkipped();
            }
        }

        private void PrepareDamage()
        {
            if (!clearInvincibilityBeforeTestDamage) return;
            if (!coreStack.IsInvincible) return;
            coreStack.ClearRespawnInvincibility();
            Debug.Log("[ReverseRespawnTestDriver] 扣血前已自动清除复活无敌（可在 Inspector 关掉 clearInvincibilityBeforeTestDamage）", this);
        }

        private void LogIfDamageSkipped()
        {
            if (coreStack.IsInvincible)
            {
                Debug.LogWarning(
                    "[ReverseRespawnTestDriver] 仍处在无敌状态，本次 ApplyDamage 被跳过。按 F8 或勾选「扣血前清除无敌」。",
                    this);
            }
        }

        [ContextMenu("Test/Apply Step Damage (once)")]
        private void ContextApplyStep()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            if (coreStack == null)
            {
                Debug.LogWarning("[ReverseRespawnTestDriver] No ReverseCoreStack.", this);
                return;
            }

            PrepareDamage();
            coreStack.ApplyDamage(stepDamage);
            LogIfDamageSkipped();
        }

        [ContextMenu("Test/Instant Kill (ApplyDamage)")]
        private void ContextInstantKill()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            if (coreStack == null)
            {
                Debug.LogWarning("[ReverseRespawnTestDriver] No ReverseCoreStack.", this);
                return;
            }

            PrepareDamage();
            coreStack.ApplyDamage(instantKillDamage);
            LogIfDamageSkipped();
        }
    }
}
