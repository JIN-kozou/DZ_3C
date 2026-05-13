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

    [Tooltip("Layers counted as \"pressing\" (e.g. Player / Default).")]
    [SerializeField]
    LayerMask pressLayers = ~0;

    [SerializeField]
    Animator animator;

    [Tooltip("Animator bool set true when occupied, false when empty.")]
    [SerializeField]
    string pressedBoolParameter = "Pressed";

    [Tooltip("Trigger volume; defaults to a BoxCollider on this GameObject (isTrigger should be on).")]
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
            pressVolume = GetComponent<BoxCollider>();
        if (pressVolume == null)
            pressVolume = GetComponent<Collider>();

        if (!string.IsNullOrEmpty(pressedBoolParameter))
            _pressedBoolHash = Animator.StringToHash(pressedBoolParameter);
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

    void SyncOccupiedFromSet()
    {
        bool occupied = _pressing.Count > 0;
        if (occupied == _occupied)
            return;
        _occupied = occupied;
        ApplyAnimator(occupied);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValidPressCollider(other))
            return;
        _pressing.Add(other);
        SyncOccupiedFromSet();
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsValidPressCollider(other))
            return;
        _pressing.Add(other);
        SyncOccupiedFromSet();
    }

    void OnTriggerExit(Collider other)
    {
        _pressing.Remove(other);
        SyncOccupiedFromSet();
    }

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
}
