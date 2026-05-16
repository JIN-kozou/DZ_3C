using UnityEngine;
using UnityEngine.InputSystem;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 运行时快捷键扣血，便于测试锚血归零后的死亡 / 复活 / GameOver。
    /// 挂在与 <see cref="ReverseCoreStack"/> 同一物体（玩家）上；伤害走 <see cref="ReverseCoreStack.ApplyDamage"/>，
    /// 会先扣外层核心再扣锚血，与正式受伤一致。
    /// <para><b>重要：</b>复活后 <see cref="ReverseCoreStack"/> 有无敌期，此期间 <see cref="ReverseCoreStack.ApplyDamage"/> 会直接 return，
    /// 表现为按死亡测试键不扣血、不触发死亡。可勾选「扣血前清除无敌」或按 <see cref="clearInvincibilityKey"/>。</para>
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
        [Tooltip("重开测试：回到初始点，清空阵列与 checkpoint，并重置逆重状态。")]
        [SerializeField] private KeyCode resetRunKey = KeyCode.F7;

        [Header("扣血量")]
        [SerializeField] private float stepDamage = 20f;
        [Tooltip("Shift + 步进键时倍率。")]
        [SerializeField] private float shiftMultiplier = 5f;

        [Tooltip("一键清空：对栈造成超大伤害（核心→锚），用于快速触发死亡。")]
        [SerializeField] private float instantKillDamage = 100000f;

        [Header("无敌与日志")]
        [Tooltip("在步进 / 死亡测试键扣血前自动清除复活无敌，否则无敌期内 ApplyDamage 无效。")]
        [SerializeField] private bool clearInvincibilityBeforeTestDamage = true;

        [Tooltip("向 Console 打印 Death / Respawn / GameOver 以及部署阵列数量（需本组件启用）。")]
        [SerializeField] private bool logReverseEvents = true;

        private Vector3 initialWorldPosition;
        private Quaternion initialWorldRotation;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            initialWorldPosition = transform.position;
            initialWorldRotation = transform.rotation;
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

            if (IsKeyDownThisFrame(clearInvincibilityKey))
            {
                coreStack.ClearRespawnInvincibility();
                Debug.Log("[ReverseRespawnTestDriver] 已清除复活无敌（F8）", this);
            }

            bool shiftHeld = Keyboard.current != null &&
                             (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float amount = shiftHeld
                ? stepDamage * shiftMultiplier
                : stepDamage;

            if (IsKeyDownThisFrame(stepDamageKey))
            {
                PrepareDamage();
                coreStack.ApplyDamage(amount);
                LogIfDamageSkipped();
            }

            if (IsKeyDownThisFrame(instantKillKey))
            {
                PrepareDamage();
                coreStack.ApplyDamage(instantKillDamage);
                LogIfDamageSkipped();
            }

            if (IsKeyDownThisFrame(resetRunKey))
            {
                ResetRunToInitialState();
            }
        }

        private static bool IsKeyDownThisFrame(KeyCode keyCode)
        {
            if (Keyboard.current == null)
            {
                return Input.GetKeyDown(keyCode);
            }

            if (!TryGetInputSystemKey(keyCode, out Key key))
            {
                return false;
            }

            return Keyboard.current[key].wasPressedThisFrame;
        }

        private static bool TryGetInputSystemKey(KeyCode keyCode, out Key key)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                key = Key.A + (keyCode - KeyCode.A);
                return true;
            }

            if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
            {
                key = Key.F1 + (keyCode - KeyCode.F1);
                return true;
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                key = Key.Digit0 + (keyCode - KeyCode.Alpha0);
                return true;
            }

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            {
                key = Key.Numpad0 + (keyCode - KeyCode.Keypad0);
                return true;
            }

            switch (keyCode)
            {
                case KeyCode.Minus: key = Key.Minus; return true;
                case KeyCode.Equals: key = Key.Equals; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                case KeyCode.Tab: key = Key.Tab; return true;
                case KeyCode.LeftShift: key = Key.LeftShift; return true;
                case KeyCode.RightShift: key = Key.RightShift; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.LeftAlt: key = Key.LeftAlt; return true;
                case KeyCode.RightAlt: key = Key.RightAlt; return true;
                default:
                    return System.Enum.TryParse(keyCode.ToString(), out key);
            }
        }

        private void ResetRunToInitialState()
        {
            if (coreStack == null) return;

            var player = coreStack.Player;
            player?.BuffSystem?.ClearAll(includeUndispellable: true);

            if (coreStack.Registry != null)
            {
                coreStack.Registry.ClearAll();
            }

            ReverseBatteryRespawnStore.Clear();
            coreStack.ClearRespawnInvincibility();

            // 回到开局位置时和 CharacterController 兼容：先禁用再设置位置。
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.SetPositionAndRotation(initialWorldPosition, initialWorldRotation);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();

            coreStack.InitializeCoresFull();
            coreStack.RefillExistingCoresAndAnchorToFull();

            Debug.Log("[ReverseRespawnTestDriver] 已重开：回到初始点、清空阵列、清空 checkpoint、重置核心与锚。", this);
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

        [ContextMenu("Test/Reset Run To Initial State")]
        private void ContextResetRun()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            if (coreStack == null)
            {
                Debug.LogWarning("[ReverseRespawnTestDriver] No ReverseCoreStack.", this);
                return;
            }

            ResetRunToInitialState();
        }
    }
}
