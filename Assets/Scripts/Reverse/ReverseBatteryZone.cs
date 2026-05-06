using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 逆重充能站（地面区域）。
    /// 玩家进入触发器：通过 PlayerBuffSystem 申请一个 Regeneration buff
    ///                （RecoverTargetType=ReverseSystem，由 Player.RecoverResource 路由到
    ///                 ReverseCoreStack.RecoverFromInnermost 链式补血——锚优先，再 cores[0]→cores[N-1]）。
    /// 玩家离开触发器：主动调用 PlayerBuffSystem.RemoveBuff(buffId) 终止回血——
    ///                 这是"走出区域立刻断电"语义，与 PlayerBuffTriggerTransmitter 不同。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ReverseBatteryZone : MonoBehaviour
    {
        private static readonly HashSet<int> PlayersInsideAnyZone = new HashSet<int>();
        [Header("Buff Config")]
        [Tooltip("ReverseBatteryConfig.asset：BuffType=Regeneration、RecoverTargetType=ReverseSystem。")]
        [SerializeField] private PlayerBuffConfigSO batteryBuffConfig;

        [Tooltip("作为 Buff 来源的 sourceType。一般填 Other 即可。")]
        [SerializeField] private PlayerBuffSourceType sourceType = PlayerBuffSourceType.Other;

        [Tooltip("是否在玩家离开时主动 RemoveBuff，实现\"走出区域立刻终止回血\"。" +
                 "关掉则任由 buff 自然过期。")]
        [SerializeField] private bool removeBuffOnExit = true;

        [Header("Hold Interaction")]
        [SerializeField] private ReverseConfig reverseConfig;
        [Tooltip("当未配置 ReverseConfig 时使用该按键。")]
        [SerializeField] private KeyCode holdKey = KeyCode.E;
        [Tooltip("当未配置 ReverseConfig 时使用该长按阈值（秒）。")]
        [Min(0.1f)]
        [SerializeField] private float holdDuration = 1.2f;
        [SerializeField] private string promptText = "长按E开始充能，并且存储该重生点";
        [SerializeField] private bool useOnGuiPromptFallback = true;

        private readonly HashSet<int> appliedPlayerIds = new HashSet<int>();
        private readonly HashSet<int> playersInZone = new HashSet<int>();
        private Player activePlayer;
        private float holdElapsed;
        private bool hasActivatedInCurrentStay;

        public bool IsPlayerInside => activePlayer != null;
        public bool IsCharging => IsPlayerInside && !hasActivatedInCurrentStay && holdElapsed > 0f;
        public float ChargeProgressNormalized
        {
            get
            {
                float duration = GetHoldDuration();
                if (duration <= 0f) return 0f;
                return Mathf.Clamp01(holdElapsed / duration);
            }
        }

        public bool IsActivatedInCurrentStay => hasActivatedInCurrentStay;
        public string PromptText => promptText;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (reverseConfig == null)
            {
                reverseConfig = Resources.Load<ReverseConfig>("Config/Reverse/ReverseConfig");
            }
        }

        private void Update()
        {
            if (activePlayer == null || hasActivatedInCurrentStay) return;
            if (!IsHoldInputPressed())
            {
                holdElapsed = 0f;
                return;
            }

            holdElapsed += Time.deltaTime;
            if (holdElapsed < GetHoldDuration()) return;

            holdElapsed = 0f;
            hasActivatedInCurrentStay = true;
            TryApplyBuff(activePlayer);
            ReverseBatteryRespawnStore.SaveBatteryRespawnPoint(transform);
        }

        private void OnGUI()
        {
            if (!useOnGuiPromptFallback) return;
            if (!IsPlayerInside || IsActivatedInCurrentStay) return;

            const float width = 460f;
            const float height = 36f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 120f;
            GUI.Label(new Rect(x, y, width, height), promptText);

            const float barHeight = 18f;
            float barY = y + height + 6f;
            Rect bgRect = new Rect(x, barY, width, barHeight);
            GUI.Box(bgRect, GUIContent.none);

            float fill = ChargeProgressNormalized;
            if (fill > 0f)
            {
                Rect fillRect = new Rect(x + 2f, barY + 2f, (width - 4f) * fill, barHeight - 4f);
                GUI.Box(fillRect, GUIContent.none);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Player player = ResolvePlayer(other);
            if (player == null) return;
            int id = player.GetInstanceID();
            playersInZone.Add(id);
            PlayersInsideAnyZone.Add(id);
            activePlayer = player;
            holdElapsed = 0f;
            hasActivatedInCurrentStay = false;
        }

        private void OnTriggerStay(Collider other)
        {
            Player player = ResolvePlayer(other);
            if (player == null) return;
            int id = player.GetInstanceID();
            if (!playersInZone.Contains(id))
            {
                playersInZone.Add(id);
            }
            PlayersInsideAnyZone.Add(id);
            activePlayer = player;
        }

        private void OnTriggerExit(Collider other)
        {
            Player player = ResolvePlayer(other);
            if (player == null) return;
            int id = player.GetInstanceID();
            playersInZone.Remove(id);
            PlayersInsideAnyZone.Remove(id);
            appliedPlayerIds.Remove(id);
            if (removeBuffOnExit && batteryBuffConfig != null)
            {
                player.BuffSystem?.RemoveBuff(batteryBuffConfig.BuffId);
            }

            if (activePlayer == player)
            {
                activePlayer = null;
                holdElapsed = 0f;
                hasActivatedInCurrentStay = false;
            }
        }

        private void TryApplyBuff(Player player)
        {
            if (player == null || batteryBuffConfig == null) return;
            int id = player.GetInstanceID();
            if (appliedPlayerIds.Contains(id)) return;
            player.ApplyBuff(batteryBuffConfig, new PlayerBuffSourceContext(sourceType, gameObject));
            appliedPlayerIds.Add(id);
        }

        private static Player ResolvePlayer(Collider other)
        {
            if (other == null) return null;
            Player p = other.GetComponent<Player>();
            if (p == null) p = other.GetComponentInParent<Player>();
            return p;
        }

        public static bool IsPlayerInsideAnyBatteryZone(Player player)
        {
            return player != null && PlayersInsideAnyZone.Contains(player.GetInstanceID());
        }

        private KeyCode GetHoldKey()
        {
            return reverseConfig != null ? reverseConfig.batteryZoneHoldKey : holdKey;
        }

        private float GetHoldDuration()
        {
            return reverseConfig != null ? reverseConfig.batteryZoneHoldDuration : holdDuration;
        }

        private bool IsHoldInputPressed()
        {
            var inputService = InputService.Instance;
            if (inputService != null && GetHoldKey() == KeyCode.E)
            {
                return inputService.Interactive;
            }

            if (Keyboard.current == null) return false;
            Key key = KeyFromKeyCode(GetHoldKey());
            return key != Key.None && Keyboard.current[key].isPressed;
        }

        private static Key KeyFromKeyCode(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.Q: return Key.Q;
                default: return Key.None;
            }
        }
    }
}
