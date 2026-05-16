using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical button: non-trigger colliders entering this object's trigger collider count as "pressed"
/// (works with CharacterController + kinematic Rigidbody on this object). Drives Animator bool; while held,
/// ramps <see cref="AppearRevealGate.revealAlongU"/> up; while not held, decays it down at a configurable rate.
/// </summary>
[DefaultExecutionOrder(0)]
[RequireComponent(typeof(Rigidbody))]
public class BatteryButtonRevealDriver : MonoBehaviour
{
    [Tooltip("Per second added to each pipeline revealAlongU while the button is held (occupied).")]
    [SerializeField]
    float revealPerSecond = 0.35f;

    [Tooltip("Per second subtracted from each pipeline revealAlongU while the button is not held. 0 = no decay.")]
    [SerializeField]
    float revealDecayPerSecond = 0.2f;

    [SerializeField]
    AppearRevealGate[] pipelines;

    [Tooltip("Layers counted as \"pressing\" (e.g. Player).")]
    [SerializeField]
    LayerMask pressLayers = ~0;

    [SerializeField]
    Animator animator;

    [Tooltip("Animator bool set true when occupied, false when empty.")]
    [SerializeField]
    string pressedBoolParameter = "Pressed";

    [Tooltip("Trigger volume; defaults to a trigger Collider on this GameObject or children.")]
    [SerializeField]
    Collider pressVolume;

    [Tooltip("Ignore colliders on children of this transform (e.g. mesh colliders on the button model).")]
    [SerializeField]
    bool ignoreCollidersOnSelfAndChildren = true;

    readonly HashSet<Collider> _pressing = new HashSet<Collider>();
    readonly List<Collider> _staleScratch = new List<Collider>(8);

    bool _occupied;
    int _pressedBoolHash;

    void Awake()
    {
        if (pressVolume == null)
            pressVolume = GetComponent<Collider>();

        if (pressVolume == null)
            pressVolume = GetComponentInChildren<Collider>(true);

        if (pressVolume != null && !pressVolume.isTrigger)
        {
            Debug.LogWarning(
                $"[BatteryButtonRevealDriver] '{name}': pressVolume '{pressVolume.name}' should have isTrigger enabled.",
                pressVolume);
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (!string.IsNullOrEmpty(pressedBoolParameter))
            _pressedBoolHash = Animator.StringToHash(pressedBoolParameter);

        if (pressVolume != null && pressVolume.gameObject != gameObject)
            EnsurePressVolumeForwarder();
    }

    void OnDisable()
    {
        _pressing.Clear();
        _occupied = false;
        ApplyAnimator(false);
    }

    void FixedUpdate()
    {
        PruneDestroyedColliders();
        SyncOccupiedFromSet();

        if (pipelines == null)
            return;

        float dt = Time.fixedDeltaTime;
        if (_occupied)
        {
            float delta = revealPerSecond * dt;
            for (int i = 0; i < pipelines.Length; i++)
            {
                AppearRevealGate g = pipelines[i];
                if (g == null || !g.appearSatisfied)
                    continue;
                g.revealAlongU = Mathf.Min(1f, g.revealAlongU + delta);
            }
        }
        else if (revealDecayPerSecond > 0f)
        {
            float decay = revealDecayPerSecond * dt;
            for (int i = 0; i < pipelines.Length; i++)
            {
                AppearRevealGate g = pipelines[i];
                if (g == null)
                    continue;
                g.revealAlongU = Mathf.Max(0f, g.revealAlongU - decay);
            }
        }
    }

    void EnsurePressVolumeForwarder()
    {
        PressVolumeTriggerForwarder forwarder =
            pressVolume.GetComponent<PressVolumeTriggerForwarder>();
        if (forwarder == null)
            forwarder = pressVolume.gameObject.AddComponent<PressVolumeTriggerForwarder>();

        forwarder.Bind(this);
    }

    /// <summary>Unity sends trigger messages to the collider's GameObject; forward from child press volumes.</summary>
    internal void HandleTriggerEnter(Collider other)
    {
        if (!IsValidPressCollider(other))
            return;
        _pressing.Add(other);
        SyncOccupiedFromSet();
    }

    internal void HandleTriggerStay(Collider other)
    {
        if (!IsValidPressCollider(other))
            return;
        _pressing.Add(other);
        SyncOccupiedFromSet();
    }

    internal void HandleTriggerExit(Collider other)
    {
        _pressing.Remove(other);
        SyncOccupiedFromSet();
    }

    void SyncOccupiedFromSet()
    {
        bool occupied = _pressing.Count > 0;
        if (occupied == _occupied)
            return;
        _occupied = occupied;
        ApplyAnimator(occupied);
    }

    void OnTriggerEnter(Collider other) => HandleTriggerEnter(other);

    void OnTriggerStay(Collider other) => HandleTriggerStay(other);

    void OnTriggerExit(Collider other) => HandleTriggerExit(other);

    void PruneDestroyedColliders()
    {
        if (_pressing.Count == 0)
            return;
        _staleScratch.Clear();
        foreach (Collider c in _pressing)
        {
            if (c == null)
                _staleScratch.Add(c);
        }

        for (int i = 0; i < _staleScratch.Count; i++)
            _pressing.Remove(_staleScratch[i]);
    }

    bool IsValidPressCollider(Collider c)
    {
        if (c == null || !c.enabled || c.isTrigger)
            return false;
        if (ignoreCollidersOnSelfAndChildren && c.transform.IsChildOf(transform))
            return false;
        if (((1 << c.gameObject.layer) & pressLayers) == 0)
            return false;
        return true;
    }

    void ApplyAnimator(bool pressed)
    {
        if (animator == null || string.IsNullOrEmpty(pressedBoolParameter))
            return;
        animator.SetBool(_pressedBoolHash, pressed);
    }

    sealed class PressVolumeTriggerForwarder : MonoBehaviour
    {
        BatteryButtonRevealDriver _driver;

        public void Bind(BatteryButtonRevealDriver driver)
        {
            _driver = driver;
        }

        void OnTriggerEnter(Collider other) => _driver?.HandleTriggerEnter(other);

        void OnTriggerStay(Collider other) => _driver?.HandleTriggerStay(other);

        void OnTriggerExit(Collider other) => _driver?.HandleTriggerExit(other);
    }
}
