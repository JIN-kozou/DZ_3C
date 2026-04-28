using System;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 单个逆重核心（池子）。
    /// 注意：核心是"持久池"——血量降到 0 不会被从玩家的 cores 列表移除，
    /// 仅当玩家把它部署成阵列时才离开列表。
    /// 因此 IsEmpty 表示 HP=0 但仍占着列表槽位。
    /// </summary>
    [Serializable]
    public class ReverseCore
    {
        [Tooltip("当前血量（0 ~ maxHealth）。")]
        [SerializeField] private float health;

        [Tooltip("血量上限。由 ReverseConfig.coreMaxHealth 注入。")]
        [SerializeField] private float maxHealth = 20f;

        [Tooltip("浮点比较容差，由 ReverseConfig.fullnessEpsilon 注入。")]
        [SerializeField] private float epsilon = 0.001f;

        public float Health
        {
            get => health;
            set => health = Mathf.Clamp(value, 0f, maxHealth);
        }

        public float MaxHealth
        {
            get => maxHealth;
            set => maxHealth = Mathf.Max(0.0001f, value);
        }

        public float Epsilon
        {
            get => epsilon;
            set => epsilon = Mathf.Max(0f, value);
        }

        public float MissingHealth => Mathf.Max(0f, maxHealth - health);

        /// <summary>是否满血。用于"只有满血核心才能部署"判定。</summary>
        public bool IsFull => MissingHealth <= epsilon;

        /// <summary>血量是否为 0（空池）。空池仍留在 cores 列表里，但不能部署、不再吃伤害（除非全空才打到锚）。</summary>
        public bool IsEmpty => health <= epsilon;

        public ReverseCore() { }

        public ReverseCore(float maxHealth, float epsilon)
        {
            this.maxHealth = Mathf.Max(0.0001f, maxHealth);
            this.epsilon = Mathf.Max(0f, epsilon);
            this.health = this.maxHealth;
        }

        /// <summary>把池子重置为满血。复活时使用。</summary>
        public void RefillToFull()
        {
            health = maxHealth;
        }

        /// <summary>把池子清空。一般只在测试/调试用。</summary>
        public void DrainToZero()
        {
            health = 0f;
        }

        /// <summary>
        /// 尝试从本核心吸走 amount 血量。返回实际吸走多少（受 health 上限）。
        /// 用于 ApplyDamage 找到最外侧非空池子之后扣血。
        /// </summary>
        public float Drain(float amount)
        {
            if (amount <= 0f) return 0f;
            float take = Mathf.Min(amount, health);
            health -= take;
            if (health < 0f) health = 0f;
            return take;
        }

        /// <summary>
        /// 尝试给本核心补 amount 血量。返回实际补了多少（受 missing 上限）。
        /// 用于 RecoverFromInnermost 链式回血。
        /// </summary>
        public float Fill(float amount)
        {
            if (amount <= 0f) return 0f;
            float give = Mathf.Min(amount, MissingHealth);
            health += give;
            if (health > maxHealth) health = maxHealth;
            return give;
        }
    }
}
