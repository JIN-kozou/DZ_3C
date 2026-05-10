using DZ_3C.AI.Config;
using DZ_3C.AI.HTN;
using DZ_3C.AI.Perception;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 怪物血量、<see cref="IAIHurtReceiver"/> 入口、死亡时关停 AI 与移动。
    /// 受击/死亡动画通过 <see cref="IMonsterHitPresentation"/> 扩展。
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterHurtReceiver : MonoBehaviour, IAIHurtReceiver
    {
        [Header("Refs")]
        [SerializeField] private MonsterCharacter character;
        [Tooltip("实现 IMonsterHitPresentation 的组件（如 MonsterAnimatorHitPresentation）；可为空。")]
        [SerializeField] private MonoBehaviour hitPresentationBehaviour;

        [Header("Health")]
        [Tooltip("当 StatConfig 为空或未运行时，作为最大血量兜底。")]
        [SerializeField, Min(1f)] private float fallbackMaxHealth = 100f;

        [Header("Death")]
        [SerializeField] private bool destroyGameObjectOnDeath = true;
        [SerializeField, Min(0f)] private float destroyDelaySeconds = 2f;
        [SerializeField] private bool disableCharacterControllerOnDeath = true;

        private IMonsterHitPresentation HitPresentation => hitPresentationBehaviour as IMonsterHitPresentation;

        private float _maxHealth;
        private float _currentHealth;
        private bool _dead;

        public bool IsDead => _dead;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        private void Awake()
        {
            if (character == null)
            {
                character = GetComponent<MonsterCharacter>();
            }
        }

        private void Start()
        {
            RebuildMaxHealthFromStat();
            _currentHealth = _maxHealth;
        }

        /// <summary>从当前 <see cref="MonsterCharacter.StatConfig"/> 刷新最大血量（不改变当前血比例时可在外部先调再手动赋值）。</summary>
        public void RebuildMaxHealthFromStat()
        {
            MonsterStatConfigSO stat = character != null ? character.StatConfig : null;
            _maxHealth = stat != null ? Mathf.Max(1f, stat.maxHealth) : Mathf.Max(1f, fallbackMaxHealth);
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }

        public void ReceiveAIDamage(float damage, string buffId, object attacker)
        {
            if (_dead || damage <= 0f)
            {
                return;
            }

            float before = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            bool killed = _currentHealth <= 0.001f;
            var ctx = new MonsterDamageContext(damage, buffId, attacker, _currentHealth, _maxHealth, killed);

            HitPresentation?.OnDamaged(in ctx);

            var attackerGo = ResolveAttackerGameObject(attacker);
            if (attackerGo != null)
            {
                var hitPerceptor = GetComponent<HitPerceptor>();
                hitPerceptor?.ReportDamage(attackerGo);
            }

            if (killed)
            {
                Die(in ctx);
            }
        }

        private void Die(in MonsterDamageContext ctx)
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            _currentHealth = 0f;

            HitPresentation?.OnDeath(in ctx);

            var targetable = GetComponent<AITargetable>();
            if (targetable == null)
            {
                targetable = GetComponentInChildren<AITargetable>();
            }

            if (targetable != null)
            {
                targetable.SetAlive(false);
            }

            var driver = GetComponent<MonsterAICharacterDriver>();
            if (driver != null)
            {
                driver.enabled = false;
            }

            var relay = GetComponent<MonsterAttackRelay>();
            if (relay != null)
            {
                relay.enabled = false;
            }

            var runtime = GetComponent<AIBehaviorRuntime>();
            if (runtime != null)
            {
                runtime.enabled = false;
            }

            if (disableCharacterControllerOnDeath && character != null && character.controller != null)
            {
                character.controller.enabled = false;
            }

            if (destroyGameObjectOnDeath)
            {
                Destroy(gameObject, destroyDelaySeconds);
            }
        }

        private static GameObject ResolveAttackerGameObject(object attacker)
        {
            if (attacker is GameObject go)
            {
                return go;
            }

            if (attacker is Component c)
            {
                return c.gameObject;
            }

            return null;
        }
    }
}
