using System.Collections;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 默认受击表现：支持 Animator <b>Trigger</b> 或 <b>Bool</b>（如 AIcontroller 的 <c>hitted</c>），
    /// 死亡可用 Trigger 或 <see cref="CrossFadeInFixedTime"/> 切入指定状态名（如 <c>Died</c>）。
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterAnimatorHitPresentation : MonoBehaviour, IMonsterHitPresentation
    {
        [SerializeField] private Animator animatorOverride;

        [Header("Hit — Bool (e.g. hitted)")]
        [SerializeField] private bool useBoolForHit;
        [SerializeField] private string hitBoolParameterName = "hitted";
        [Min(0.01f)] [SerializeField] private float hitBoolResetDelay = 0.08f;

        [Header("Hit — Trigger (optional if useBoolForHit)")]
        [SerializeField] private string hitTriggerName = "Hit";

        [Header("Death")]
        [SerializeField] private string deathTriggerName = "Die";
        [Tooltip("非空且未配置死亡 Trigger 时，在 layer 0 上 CrossFade 到该状态（如 Died）。")]
        [SerializeField] private string deathStateName = "Died";
        [SerializeField] private bool clearHitTriggerOnDeath = true;

        private Animator _animator;
        private int _hitTriggerHash;
        private int _deathTriggerHash;
        private int _hitBoolHash;
        private bool _hasHitTrigger;
        private bool _hasDeathTrigger;
        private bool _hasHitBool;
        private Coroutine _hitBoolRoutine;

        private void Awake()
        {
            _animator = animatorOverride != null ? animatorOverride : GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            CacheHashes();
        }

        private void OnValidate()
        {
            CacheHashes();
        }

        private void CacheHashes()
        {
            _hasHitTrigger = !string.IsNullOrEmpty(hitTriggerName);
            _hitTriggerHash = _hasHitTrigger ? Animator.StringToHash(hitTriggerName) : 0;
            _hasDeathTrigger = !string.IsNullOrEmpty(deathTriggerName);
            _deathTriggerHash = _hasDeathTrigger ? Animator.StringToHash(deathTriggerName) : 0;
            _hasHitBool = !string.IsNullOrEmpty(hitBoolParameterName);
            _hitBoolHash = _hasHitBool ? Animator.StringToHash(hitBoolParameterName) : 0;
        }

        public void OnDamaged(in MonsterDamageContext context)
        {
            if (_animator == null)
            {
                return;
            }

            if (useBoolForHit && _hasHitBool)
            {
                if (_hitBoolRoutine != null)
                {
                    StopCoroutine(_hitBoolRoutine);
                }

                _animator.SetBool(_hitBoolHash, true);
                _hitBoolRoutine = StartCoroutine(ResetHitBoolAfterDelay());
            }
            else if (_hasHitTrigger)
            {
                _animator.SetTrigger(_hitTriggerHash);
            }
        }

        private IEnumerator ResetHitBoolAfterDelay()
        {
            yield return new WaitForSeconds(hitBoolResetDelay);
            if (_animator != null && _hasHitBool)
            {
                _animator.SetBool(_hitBoolHash, false);
            }

            _hitBoolRoutine = null;
        }

        public void OnDeath(in MonsterDamageContext context)
        {
            if (_animator == null)
            {
                return;
            }

            if (_hitBoolRoutine != null)
            {
                StopCoroutine(_hitBoolRoutine);
                _hitBoolRoutine = null;
            }

            if (useBoolForHit && _hasHitBool)
            {
                _animator.SetBool(_hitBoolHash, false);
            }

            if (clearHitTriggerOnDeath && _hasHitTrigger)
            {
                _animator.ResetTrigger(_hitTriggerHash);
            }

            if (_hasDeathTrigger)
            {
                _animator.SetTrigger(_deathTriggerHash);
                return;
            }

            if (!string.IsNullOrEmpty(deathStateName))
            {
                _animator.CrossFadeInFixedTime(deathStateName, 0.12f, 0, 0f);
            }
        }
    }
}
