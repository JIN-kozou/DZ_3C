using UnityEngine;

/// <summary>
/// Dynamically raises <see cref="Material.renderQueue"/> while this object is inside any
/// <see cref="ShaderPosition"/> reveal sphere (and optionally while <see cref="AppearRevealGate"/> allows reveal),
/// so Appear/transparent materials sort closer to opaque scene geometry and pop less from transparent reordering.
/// </summary>
[DefaultExecutionOrder(110)]
[DisallowMultipleComponent]
public class AppearRevealRenderPriority : MonoBehaviour
{
    public enum QueueProfile
    {
        /// <summary>Revealed = 3001, ghost = 2990. Stays in transparent pass; draws on top of other transparents.</summary>
        TransparentOnTop,
        /// <summary>Revealed = 2450 (alpha-test band), ghost = 2990. Drawn before default transparent (3000); best default for “scene-like” order.</summary>
        BeforeTransparent,
        /// <summary>Revealed = 2000 (geometry), ghost = 2990. Strongest priority; may look wrong if shader stays alpha-blend.</summary>
        Geometry
    }

    public enum RevealTestPoint
    {
        TransformPosition,
        RendererBoundsCenter
    }

    [SerializeField]
    [Tooltip("How revealed vs ghost queues are chosen.")]
    QueueProfile queueProfile = QueueProfile.BeforeTransparent;

    [SerializeField]
    [Tooltip("World point tested against ShaderPosition spheres.")]
    RevealTestPoint testPoint = RevealTestPoint.RendererBoundsCenter;

    [SerializeField]
    [Tooltip("Match ShaderPosition.IsWorldPositionInsideAnyRevealSphere (XZ distance).")]
    bool useXZPlaneDistance = true;

    [SerializeField]
    [Tooltip("Also require AppearRevealGate on this object when present.")]
    bool respectAppearRevealGate = true;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("When AppearRevealGate exists, revealAlongU must be >= this to count as revealed.")]
    float minRevealAlongU = 0.999f;

    [SerializeField]
    [Tooltip("Empty = all child Renderers. Otherwise only these renderers get queue changes.")]
    Renderer[] renderers;

    [SerializeField]
    [Tooltip("Override revealed queue (0 = use QueueProfile preset).")]
    int revealedRenderQueueOverride;

    [SerializeField]
    [Tooltip("Override ghost queue (0 = use QueueProfile preset).")]
    int ghostRenderQueueOverride;

    AppearRevealGate _gate;
    Renderer[] _cachedRenderers;
    bool _lastRevealed;
    bool _hasLastRevealed;

    void Awake()
    {
        _gate = GetComponent<AppearRevealGate>();
        EnsureRendererCache();
    }

    void OnTransformChildrenChanged()
    {
        _cachedRenderers = null;
    }

    void LateUpdate()
    {
        bool revealed = EvaluateRevealed();
        if (_hasLastRevealed && revealed == _lastRevealed)
            return;

        _lastRevealed = revealed;
        _hasLastRevealed = true;
        ApplyQueues(revealed);
    }

    void OnDisable()
    {
        if (_hasLastRevealed)
            ApplyQueues(false);
        _hasLastRevealed = false;
    }

    void EnsureRendererCache()
    {
        if (renderers != null && renderers.Length > 0)
            return;

        _cachedRenderers = GetComponentsInChildren<Renderer>(true);
        renderers = _cachedRenderers;
    }

    bool EvaluateRevealed()
    {
        if (!TryGetTestPosition(out Vector3 worldPos))
            return false;

        if (!ShaderPosition.IsWorldPositionInsideAnyRevealSphere(worldPos, useXZPlaneDistance))
            return false;

        if (!respectAppearRevealGate || _gate == null)
            return true;

        if (!_gate.appearSatisfied)
            return false;

        return _gate.revealAlongU >= minRevealAlongU - 0.0001f;
    }

    bool TryGetTestPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (testPoint == RevealTestPoint.TransformPosition)
        {
            worldPos = transform.position;
            return true;
        }

        EnsureRendererCache();
        if (renderers == null || renderers.Length == 0)
            return false;

        Bounds? bounds = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            if (bounds == null)
                bounds = r.bounds;
            else
            {
                Bounds b = bounds.Value;
                b.Encapsulate(r.bounds);
                bounds = b;
            }
        }

        if (bounds == null)
            return false;

        worldPos = bounds.Value.center;
        return true;
    }

    void ApplyQueues(bool revealed)
    {
        EnsureRendererCache();
        if (renderers == null)
            return;

        GetQueues(out int revealedQueue, out int ghostQueue);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            Material[] mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null)
                    continue;

                mats[m].renderQueue = revealed ? revealedQueue : ghostQueue;
            }
        }
    }

    void GetQueues(out int revealedQueue, out int ghostQueue)
    {
        if (revealedRenderQueueOverride > 0)
            revealedQueue = revealedRenderQueueOverride;
        else
        {
            switch (queueProfile)
            {
                case QueueProfile.TransparentOnTop:
                    revealedQueue = 3001;
                    break;
                case QueueProfile.Geometry:
                    revealedQueue = 2000;
                    break;
                default:
                    revealedQueue = 2450;
                    break;
            }
        }

        if (ghostRenderQueueOverride > 0)
            ghostQueue = ghostRenderQueueOverride;
        else
            ghostQueue = 2990;
    }
}
