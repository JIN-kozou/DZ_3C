using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Perception
{
    public abstract class BasePerceptor : MonoBehaviour
    {
        [SerializeField] protected AIConfigSO config;
        [SerializeField] protected AIBlackboard blackboard;
        [SerializeField] protected Transform owner;

        private float elapsed;

        protected virtual void Awake()
        {
            if (owner == null) owner = transform;
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        protected virtual void Update()
        {
            float interval = Mathf.Max(0.01f, 1f / Mathf.Max(1f, TickHz));
            elapsed += Time.deltaTime;
            if (elapsed < interval) return;
            elapsed = 0f;
            Sense(Time.time);
        }

        protected abstract float TickHz { get; }
        protected abstract void Sense(float now);
    }
}
