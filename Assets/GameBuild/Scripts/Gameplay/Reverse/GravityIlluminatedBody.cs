using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Opt-in weightless prop: gains Unity gravity only while overlapping a <see cref="GravityInfluenceSource"/> trigger.
    /// Kick bookkeeping decays separately from player-applied velocity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class GravityIlluminatedBody : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;

        [Tooltip("Layers accepted as illumination triggers (should include GravityInfluence).")]
        [SerializeField] private LayerMask influenceTriggerMask = ~0;

        [Tooltip("If true, Awake sets the mask to only GravityInfluence from project layer name.")]
        [SerializeField] private bool autoInfluenceLayerMask = true;

        [Tooltip("Initial state per design: weightless until illuminated.")]
        [SerializeField] private bool startWeightless = true;

        [Header("Return kick (失重回弹)")]
        [Tooltip("After losing illumination, wait a random time in [min, max] before applying kick.")]
        [SerializeField, Min(0f)]
        private float kickDelaySecondsMin = 0.5f;

        [SerializeField, Min(0f)]
        private float kickDelaySecondsMax = 1f;

        [SerializeField] private float kickInitialSpeed = 0.8f;

        [SerializeField] private float kickDecayLambda = 6f;

        [SerializeField] private float kickCutoffSqr = 1e-6f;

        private readonly HashSet<Collider> influencers = new HashSet<Collider>();

        private Coroutine delayedKickRoutine;

        /// <summary>Scalar speed along anti-gravity axis still attributed to the artificial kick.</summary>
        private float kickAlongUp;

        private static Vector3 PhysicsGravityDown =>
            Physics.gravity.sqrMagnitude > 1e-8f ? Physics.gravity.normalized : Vector3.down;

        private static Vector3 AntiGravityUp => -PhysicsGravityDown;

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            rb.drag = 0f;

            if (autoInfluenceLayerMask)
            {
                int inf = LayerMask.NameToLayer(GravityIlluminationLayers.Influence);
                if (inf >= 0)
                {
                    influenceTriggerMask = 1 << inf;
                }
            }
        }

        private void Start()
        {
            if (startWeightless)
            {
                rb.useGravity = false;
            }

            rb.drag = 0f;
        }

        private void FixedUpdate()
        {
            rb.drag = 0f;

            if (kickAlongUp * kickAlongUp <= kickCutoffSqr)
            {
                if (kickAlongUp != 0f)
                {
                    kickAlongUp = 0f;
                }

                return;
            }

            float decay = Mathf.Exp(-kickDecayLambda * Time.fixedDeltaTime);
            float newKick = kickAlongUp * decay;
            float deltaKick = kickAlongUp - newKick;
            rb.velocity -= AntiGravityUp * deltaKick;
            kickAlongUp = newKick;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidInfluenceCollider(other))
            {
                return;
            }

            if (!influencers.Add(other))
            {
                return;
            }

            GravityInfluenceSource src = other.GetComponent<GravityInfluenceSource>();
            src?.RegisterSubscriber(this);
            ApplyIlluminationChanged();
        }

        private void OnTriggerExit(Collider other)
        {
            GravityInfluenceSource src = other.GetComponent<GravityInfluenceSource>();
            src?.UnregisterSubscriber(this);

            if (!influencers.Remove(other))
            {
                return;
            }

            ApplyIlluminationChanged();
        }

        internal void NotifyInfluenceSourceDisabled(GravityInfluenceSource source)
        {
            Collider c = source != null ? source.InfluenceCollider : null;
            if (c == null)
            {
                return;
            }

            if (influencers.Remove(c))
            {
                ApplyIlluminationChanged();
            }
        }

        private bool IsValidInfluenceCollider(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if ((influenceTriggerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return false;
            }

            return other.GetComponent<GravityInfluenceSource>() != null;
        }

        private void ApplyIlluminationChanged()
        {
            bool lit = influencers.Count > 0;
            if (lit)
            {
                CancelDelayedKick();
                rb.velocity -= AntiGravityUp * kickAlongUp;
                kickAlongUp = 0f;
                rb.useGravity = true;
                return;
            }

            rb.useGravity = false;
            rb.velocity -= AntiGravityUp * kickAlongUp;
            kickAlongUp = 0f;
            CancelDelayedKick();
            delayedKickRoutine = StartCoroutine(DelayedKickRoutine());
        }

        private void CancelDelayedKick()
        {
            if (delayedKickRoutine == null)
            {
                return;
            }

            StopCoroutine(delayedKickRoutine);
            delayedKickRoutine = null;
        }

        private IEnumerator DelayedKickRoutine()
        {
            float lo = Mathf.Min(kickDelaySecondsMin, kickDelaySecondsMax);
            float hi = Mathf.Max(kickDelaySecondsMin, kickDelaySecondsMax);
            float wait = lo >= hi ? lo : Random.Range(lo, hi);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            delayedKickRoutine = null;
            if (influencers.Count > 0)
            {
                yield break;
            }

            kickAlongUp = kickInitialSpeed;
            rb.velocity += AntiGravityUp * kickInitialSpeed;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (rb != null)
            {
                rb.drag = 0f;
            }

            if (kickDelaySecondsMax < kickDelaySecondsMin)
            {
                kickDelaySecondsMax = kickDelaySecondsMin;
            }
        }
#endif
    }
}
