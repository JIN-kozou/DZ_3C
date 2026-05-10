using System.Collections;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 默认受击表现：优先用 <see cref="CrossFadeInFixedTime"/> 切入 Hit 状态（最稳），
    /// 可选 Bool/Trigger；死亡可用 Trigger 或 CrossFade 到 Died。
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterAnimatorHitPresentation : MonoBehaviour, IMonsterHitPresentation
    {
        [SerializeField] private Animator animatorOverride;

        [Header("Hit — Direct state (recommended)")]
        [Tooltip("为 true 时每次受击直接 CrossFade 到 Hit，不依赖状态机里 Bool 过渡。")]
        [SerializeField] private bool playHitByCrossFade = true;
        [SerializeField] private string hitStateName = "Hit";
        [Min(0.01f)] [SerializeField] private float hitCrossFadeDuration = 0.12f;
        [Tooltip("避免被摄像机视锥剔除时不更新 Animator。")]
        [SerializeField] private bool forceAlwaysAnimate = true;

        [Header("Hit — Bool (e.g. hitted)")]
        [SerializeField] private bool useBoolForHit;
        [SerializeField] private string hitBoolParameterName = "hitted";
        [Min(0.01f)] [SerializeField] private float hitBoolResetDelay = 0.2f;

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
        private bool _loggedMissingController;

        private void Awake()
        {
            _animator = animatorOverride != null ? animatorOverride : GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null && forceAlwaysAnimate)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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
            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                if (!_loggedMissingController)
                {
                    _loggedMissingController = true;
                    Debug.LogWarning(
                        "[MonsterAnimatorHitPresentation] Animator has no RuntimeAnimatorController; hit animation will not play.",
                        this);
                }

                return;
            }

            if (playHitByCrossFade && !string.IsNullOrEmpty(hitStateName))
            {
                _animator.CrossFadeInFixedTime(hitStateName, hitCrossFadeDuration, 0, 0f);
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
