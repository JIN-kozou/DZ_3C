using System.Collections.Generic;
using DZ_3C.UI.WorldInteraction;
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
    public class ReverseBatteryZone : MonoBehaviour, IWorldInteractionHoldProgress
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
        [SerializeField] private ReverseArrayAudio reverseArrayAudio;
        [SerializeField] private WorldInteractionPromptAnchor promptAnchor;

        private readonly HashSet<int> appliedPlayerIds = new HashSet<int>();
        private readonly HashSet<int> playersInZone = new HashSet<int>();
        private Player activePlayer;
        private CharacterAudio activeCharacterAudio;
        private float holdElapsed;
        private bool hasActivatedInCurrentStay;
        private bool chargeAudioPlaying;

        public bool IsPlayerInside => activePlayer != null;
        public bool IsCharging => IsPlayerInside && !hasActivatedInCurrentStay && holdElapsed > 0f;
        public bool BlocksMovement => IsCharging;
        public float ChargeProgressNormalized => NormalizedProgress;

        public float NormalizedProgress
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

            if (reverseArrayAudio == null)
            {
                reverseArrayAudio = GetComponent<ReverseArrayAudio>();
            }

            if (reverseArrayAudio == null)
            {
                reverseArrayAudio = GetComponentInChildren<ReverseArrayAudio>(true);
            }

            EnsurePromptAnchor();
        }

        private void OnDisable()
        {
            StopChargeAudio();
            if (promptAnchor != null)
            {
                promptAnchor.SetPlayerInRange(false);
                promptAnchor.SetAvailable(false);
            }
        }

        private void Update()
        {
            if (activePlayer == null || hasActivatedInCurrentStay) return;
            if (!IsHoldInputPressed())
            {
                holdElapsed = 0f;
                StopChargeAudio();
                return;
            }

            if (holdElapsed <= 0f)
            {
                StartChargeAudio(activePlayer);
            }

            holdElapsed += Time.deltaTime;
            if (holdElapsed < GetHoldDuration()) return;

            holdElapsed = 0f;
            hasActivatedInCurrentStay = true;
            StopChargeAudio();
            TryApplyBuff(activePlayer);
            activeCharacterAudio?.OnCoreCharge();
            ReverseBatteryRespawnStore.SaveBatteryRespawnPoint(transform);
            RefreshPromptAvailability();
        }

        private void OnTriggerEnter(Collider other)
        {
            Player player = ResolvePlayer(other);
            if (player == null) return;
            int id = player.GetInstanceID();
            playersInZone.Add(id);
            PlayersInsideAnyZone.Add(id);
            activePlayer = player;
            activeCharacterAudio = player.GetComponent<CharacterAudio>() ?? player.GetComponentInChildren<CharacterAudio>(true);
            holdElapsed = 0f;
            hasActivatedInCurrentStay = false;
            WorldInteractionPromptManager.EnsureOnPlayer(player);
            if (promptAnchor != null)
            {
                promptAnchor.SetPlayerInRange(true);
            }

            RefreshPromptAvailability();
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
            if (activeCharacterAudio == null)
            {
                activeCharacterAudio = player.GetComponent<CharacterAudio>() ?? player.GetComponentInChildren<CharacterAudio>(true);
            }
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
                StopChargeAudio();
                activePlayer = null;
                activeCharacterAudio = null;
                holdElapsed = 0f;
                hasActivatedInCurrentStay = false;
                if (promptAnchor != null)
                {
                    promptAnchor.SetPlayerInRange(false);
                    RefreshPromptAvailability();
                }
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

        private void EnsurePromptAnchor()
        {
            if (promptAnchor == null)
            {
                promptAnchor = GetComponent<WorldInteractionPromptAnchor>();
            }

            if (promptAnchor == null)
            {
                promptAnchor = gameObject.AddComponent<WorldInteractionPromptAnchor>();
            }

            string key = GetHoldKey() == KeyCode.E ? "E" : GetHoldKey().ToString();
            promptAnchor.Configure(promptText, WorldInteractionMode.Hold, key);
        }

        private void RefreshPromptAvailability()
        {
            if (promptAnchor == null)
            {
                return;
            }

            bool available = !hasActivatedInCurrentStay;
            promptAnchor.SetAvailable(available);
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

        private void StartChargeAudio(Player player)
        {
            if (chargeAudioPlaying)
            {
                return;
            }

            if (activeCharacterAudio == null && player != null)
            {
                activeCharacterAudio = player.GetComponent<CharacterAudio>() ?? player.GetComponentInChildren<CharacterAudio>(true);
            }

            reverseArrayAudio?.StartCoreCharge();
            chargeAudioPlaying = true;
        }

        private void StopChargeAudio()
        {
            if (!chargeAudioPlaying)
            {
                return;
            }

            reverseArrayAudio?.StopCoreCharge();
            chargeAudioPlaying = false;
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
