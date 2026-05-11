using Animancer;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    /// <summary>
    /// 用 Animancer 播放受击 / 待机 / 死亡片段（Animator 的 Controller 须为空，由 Animancer 接管）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AnimancerComponent))]
    public class MonsterAnimancerHitPresentation : MonoBehaviour, IMonsterHitPresentation
    {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip hitClip;
        [SerializeField] private AnimationClip deathClip;
        [Min(0f)] [SerializeField] private float hitFadeSeconds = 0.12f;
        [Min(0f)] [SerializeField] private float idleFadeSeconds = 0.12f;
        [Min(0f)] [SerializeField] private float deathFadeSeconds = 0.15f;

        private MonsterHurtReceiver _hurt;

        private void Awake()
        {
            if (animancer == null)
            {
                animancer = GetComponent<AnimancerComponent>();
            }

            if (animancer != null && animancer.Animator == null)
            {
                animancer.Animator = GetComponent<Animator>();
            }

            _hurt = GetComponent<MonsterHurtReceiver>();
        }

        private void Start()
        {
            if (animancer == null || idleClip == null)
            {
                return;
            }

            animancer.Play(idleClip, 0f);
        }

        public void OnDamaged(in MonsterDamageContext context)
        {
            if (animancer == null || hitClip == null)
            {
                return;
            }

            var st = animancer.Play(hitClip, hitFadeSeconds);
            if (idleClip != null)
            {
                st.Events(this).OnEnd = OnHitAnimationEnd;
            }
        }

        private void OnHitAnimationEnd()
        {
            if (animancer == null || idleClip == null)
            {
                return;
            }

            if (_hurt != null && _hurt.IsDead)
            {
                return;
            }

            animancer.Play(idleClip, idleFadeSeconds);
        }

        public void OnDeath(in MonsterDamageContext context)
        {
            if (animancer == null)
            {
                return;
            }

            if (deathClip != null)
            {
                animancer.Play(deathClip, deathFadeSeconds);
            }
            else if (idleClip != null)
            {
                animancer.Play(idleClip, idleFadeSeconds);
            }
        }
    }
}
