using UnityEngine;

namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 只读快照：怪物一次受击/死亡时交给 <see cref="IMonsterHitPresentation"/>。
    /// </summary>
    public readonly struct MonsterDamageContext
    {
        public readonly float DamageAmount;
        public readonly string BuffId;
        public readonly object Attacker;
        public readonly float CurrentHealth;
        public readonly float MaxHealth;
        public readonly bool IsDead;

        public MonsterDamageContext(float damageAmount, string buffId, object attacker, float currentHealth, float maxHealth, bool isDead)
        {
            DamageAmount = damageAmount;
            BuffId = buffId ?? string.Empty;
            Attacker = attacker;
            CurrentHealth = Mathf.Max(0f, currentHealth);
            MaxHealth = Mathf.Max(0.01f, maxHealth);
            IsDead = isDead;
        }

        public float NormalizedHealth => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
    }
}
