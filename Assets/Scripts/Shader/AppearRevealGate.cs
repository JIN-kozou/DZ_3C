using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs after <see cref="ShaderPosition"/> and sets material <c>_Reveal</c> when <see cref="appearSatisfied"/> is true.
/// Use with materials using <c>Assets/Shader/AppearShader.shadergraph</c>: add Blackboard <c>_Reveal</c> (and optional <c>_RevealSoft</c>),
/// and a Custom Function node that includes <c>Assets/Shader/AppearRevealLib.hlsl</c> (<c>AppearReveal_UVRevealMask_float</c>).
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class AppearRevealGate : MonoBehaviour
{
    public enum RevealApplyScope
    {
        /// <summary>Only <see cref="targetRenderer"/> (or the first Renderer on this object / in children if unset).</summary>
        SingleRenderer,
        /// <summary>Every <see cref="Renderer"/> under this GameObject (typical for Magic Spline: many child FX instances, each needs its own MPB).</summary>
        ThisGameObjectAndChildren
    }

    [Tooltip("Gameplay: set true only after Appear conditions are met; while false, _Reveal is forced to 0.")]
    public bool appearSatisfied = true;

    [Range(0f, 1f)]
    [Tooltip("Along mesh UV.x when appearSatisfied is true.")]
    public float revealAlongU = 1f;

    [SerializeField]
    [Tooltip("SingleRenderer: only this renderer. ThisGameObjectAndChildren: ignored for scope; each child renderer gets the same reveal value for this spline instance.")]
    Renderer targetRenderer;

    [SerializeField]
    [Tooltip("Magic Spline：挂在样条根物体上时选 ThisGameObjectAndChildren，让每个子 Renderer 各自一份 MPB 的 _Reveal。单网格角色保持 SingleRenderer。")]
    RevealApplyScope applyScope = RevealApplyScope.SingleRenderer;

    [SerializeField]
    [Tooltip("When reveal reaches 1 (and appearSatisfied), each event runs once until reveal drops below threshold again (unless locked).")]
    UnityEvent[] onRevealFullOnce;

    [SerializeField]
    [Tooltip("Treat as full when reveal >= 1 - epsilon.")]
    float fullRevealEpsilon = 0.001f;

    [SerializeField]
    [Tooltip("If true, onRevealFullOnce only ever fires once for this component lifetime.")]
    bool lockRevealFullEventsAfterFirst;

    [SerializeField]
    [Tooltip("When revealAlongU returns to ~0, each event runs once until reveal rises above threshold again (unless locked). Uses revealAlongU, not shader-gated value.")]
    UnityEvent[] onRevealEmptyOnce;

    [SerializeField]
    [Tooltip("Treat as empty when revealAlongU <= epsilon.")]
    float emptyRevealEpsilon = 0.001f;

    [SerializeField]
    [Tooltip("If true, onRevealEmptyOnce only ever fires once for this component lifetime.")]
    bool lockRevealEmptyEventsAfterFirst;

    MaterialPropertyBlock _block;
    Renderer[] _cachedRenderers;
    bool _revealFullEventsFired;
    bool _revealEmptyEventsFired;

    void Awake()
    {
        if (_block == null)
            _block = new MaterialPropertyBlock();

        if (applyScope == RevealApplyScope.SingleRenderer)
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    void OnEnable()
    {
        if (_block == null)
            _block = new MaterialPropertyBlock();
        InvalidateRendererCache();
    }

    void OnTransformChildrenChanged()
    {
        InvalidateRendererCache();
    }

    void InvalidateRendererCache()
    {
        _cachedRenderers = null;
    }

    void EnsureChildRendererCache()
    {
        if (_cachedRenderers != null)
            return;
        _cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void LateUpdate()
    {
        float reveal = appearSatisfied ? revealAlongU : 0f;

        if (applyScope == RevealApplyScope.SingleRenderer)
        {
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_block);
                _block.SetFloat("_Reveal", reveal);
                targetRenderer.SetPropertyBlock(_block);
            }
        }
        else
        {
            EnsureChildRendererCache();
            if (_cachedRenderers != null && _cachedRenderers.Length > 0)
            {
                for (int i = 0; i < _cachedRenderers.Length; i++)
                {
                    Renderer r = _cachedRenderers[i];
                    if (r == null || !r.enabled)
                        continue;

                    r.GetPropertyBlock(_block);
                    _block.SetFloat("_Reveal", reveal);
                    r.SetPropertyBlock(_block);
                }
            }
        }

        EvaluateRevealFullEvents(reveal);
        EvaluateRevealEmptyEvents();
    }

    void EvaluateRevealFullEvents(float effectiveReveal)
    {
        if (onRevealFullOnce == null || onRevealFullOnce.Length == 0)
            return;

        float threshold = 1f - Mathf.Max(0.00001f, fullRevealEpsilon);
        bool full = appearSatisfied && effectiveReveal >= threshold;
        if (!full)
        {
            if (!lockRevealFullEventsAfterFirst)
                _revealFullEventsFired = false;
            return;
        }

        if (_revealFullEventsFired)
            return;

        for (int i = 0; i < onRevealFullOnce.Length; i++)
            onRevealFullOnce[i]?.Invoke();

        _revealFullEventsFired = true;
    }

    void EvaluateRevealEmptyEvents()
    {
        if (onRevealEmptyOnce == null || onRevealEmptyOnce.Length == 0)
            return;

        float eps = Mathf.Max(0.00001f, emptyRevealEpsilon);
        bool empty = revealAlongU <= eps;
        if (!empty)
        {
            if (!lockRevealEmptyEventsAfterFirst)
                _revealEmptyEventsFired = false;
            return;
        }

        if (_revealEmptyEventsFired)
            return;

        for (int i = 0; i < onRevealEmptyOnce.Length; i++)
            onRevealEmptyOnce[i]?.Invoke();

        _revealEmptyEventsFired = true;
    }
}
