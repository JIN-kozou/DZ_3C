using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Marks a trigger collider as a gravity illumination source (bullet / deployed array).
    /// Pair with <see cref="GravityInfluenceTriggerSync"/> on the same GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public class GravityInfluenceSource : MonoBehaviour
    {
        [SerializeField] private Collider influenceCollider;

        private readonly List<GravityIlluminatedBody> subscribers = new List<GravityIlluminatedBody>(8);

        public Collider InfluenceCollider => influenceCollider != null ? influenceCollider : GetComponent<Collider>();

        private void Awake()
        {
            if (influenceCollider == null)
            {
                influenceCollider = GetComponent<Collider>();
            }
        }

        internal void RegisterSubscriber(GravityIlluminatedBody body)
        {
            if (body == null)
            {
                return;
            }

            if (!subscribers.Contains(body))
            {
                subscribers.Add(body);
            }
        }

        internal void UnregisterSubscriber(GravityIlluminatedBody body)
        {
            if (body == null)
            {
                return;
            }

            subscribers.Remove(body);
        }

        private void OnDisable()
        {
            for (int i = subscribers.Count - 1; i >= 0; i--)
            {
                GravityIlluminatedBody b = subscribers[i];
                if (b != null)
                {
                    b.NotifyInfluenceSourceDisabled(this);
                }
            }

            subscribers.Clear();
        }
    }
}
