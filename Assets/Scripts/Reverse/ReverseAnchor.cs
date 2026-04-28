using System;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 锚 = Player.ReusableData.health 的轻代理组件。
    /// 不存任何额外字段，所有 get/set 直接转发到 Player.ReusableData.health.Value，
    /// 这样 Buff 系统的 Regeneration(Health) 也能直接给锚回血。
    /// 同时订阅 health.ValueChanged 把"归零=死亡"事件透出来给 ReverseCoreStack。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class ReverseAnchor : MonoBehaviour
    {
        [Tooltip("绑定的 Player 组件。空则在 Awake 自动 GetComponent。")]
        [SerializeField] private Player player;

        [Tooltip("逆重系统配置。用于读 anchorMaxHealth 校验和 deathThreshold。")]
        [SerializeField] private ReverseConfig config;

        /// <summary>锚血降到死亡阈值（含等于）时触发一次。</summary>
        public event Action OnAnchorDied;

        /// <summary>锚血变化的实时回调（每次 health.Value 实际变化时触发）。</summary>
        public event Action<float> OnAnchorHealthChanged;

        private bool subscribed;

        public Player Player => player;
        public ReverseConfig Config => config;

        public float CurrentHealth
        {
            get => player != null && player.ReusableData != null ? player.ReusableData.health.Value : 0f;
            set
            {
                if (player == null || player.ReusableData == null) return;
                player.ReusableData.health.Value = Mathf.Clamp(value, 0f, MaxHealth);
            }
        }

        public float MaxHealth
        {
            get
            {
                if (player != null) return player.MaxHealth;
                if (config != null) return config.anchorMaxHealth;
                return 100f;
            }
        }

        public float MissingHealth => Mathf.Max(0f, MaxHealth - CurrentHealth);

        public bool IsFull => MissingHealth <= (config != null ? config.fullnessEpsilon : 0.001f);

        public bool IsDead =>
            CurrentHealth <= (config != null ? config.deathThreshold : 0f) + (config != null ? config.fullnessEpsilon : 0.001f);

        private void Awake()
        {
            if (player == null) player = GetComponent<Player>();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            // 兜底：Player.Awake 后 ReusableData 才存在，确保订阅成功。
            TrySubscribe();
            if (config != null && config.validateAnchorHealthSync && player != null
                && Mathf.Abs(player.MaxHealth - config.anchorMaxHealth) > 0.01f)
            {
                Debug.LogWarning(
                    $"[ReverseAnchor] Player.MaxHealth({player.MaxHealth}) != ReverseConfig.anchorMaxHealth({config.anchorMaxHealth})。" +
                    "建议在 Inspector 里同步两边数值。");
            }
        }

        private void OnDisable()
        {
            if (player != null && player.ReusableData != null && subscribed)
            {
                player.ReusableData.health.ValueChanged -= HandleHealthChanged;
                subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (subscribed) return;
            if (player == null || player.ReusableData == null) return;
            player.ReusableData.health.ValueChanged += HandleHealthChanged;
            subscribed = true;
        }

        private void HandleHealthChanged(float newHealth)
        {
            OnAnchorHealthChanged?.Invoke(newHealth);
            float threshold = (config != null ? config.deathThreshold : 0f);
            if (newHealth <= threshold)
            {
                OnAnchorDied?.Invoke();
            }
        }

        /// <summary>把锚补到满。复活时使用。</summary>
        public void RefillToFull()
        {
            CurrentHealth = MaxHealth;
        }

        /// <summary>从锚扣 amount 血，返回实际扣了多少。</summary>
        public float Drain(float amount)
        {
            if (amount <= 0f) return 0f;
            float take = Mathf.Min(amount, CurrentHealth);
            CurrentHealth -= take;
            return take;
        }

        /// <summary>给锚补 amount 血，返回实际补了多少（受 MissingHealth 上限）。</summary>
        public float Fill(float amount)
        {
            if (amount <= 0f) return 0f;
            float give = Mathf.Min(amount, MissingHealth);
            CurrentHealth += give;
            return give;
        }
    }
}
