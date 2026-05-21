using System;
using DZ_3C.Reverse;
using UnityEngine;

/// <summary>
/// Smoothly transitions <see cref="ShaderPosition.radius"/> using <see cref="ReverseConfig"/> timing/curve
/// (or fallback values). Shared by player <see cref="DZ_3C.Reverse.ReverseVisionDriver"/> and machine receivers.
/// </summary>
[DisallowMultipleComponent]
public class ShaderPositionRadiusTransition : MonoBehaviour
{
    [SerializeField] private ShaderPosition shaderPosition;

    [SerializeField] private ReverseConfig config;

    [Header("Fallback Transition (兜底过渡参数)")]
    [Tooltip("当未绑定 ReverseConfig 时，半径过渡的默认时长（秒）。0 表示瞬变。")]
    [Min(0f)]
    [SerializeField] private float fallbackTransitionDuration = 0.25f;

    [Tooltip("当未绑定 ReverseConfig 时，半径过渡曲线。X=归一化时间(0~1)，Y=归一化插值(0~1)。")]
    [SerializeField] private AnimationCurve fallbackTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float transitionStartRadius;
    private float transitionTargetRadius;
    private float transitionElapsed;
    private bool isTransitioning;

    public float CurrentRadius { get; private set; }

    public event Action<float> RadiusChanged;

    private void Awake()
    {
        EnsureShaderPosition();
        if (config == null)
        {
            config = Resources.Load<ReverseConfig>("Config/Reverse/ReverseConfig");
        }

        if (shaderPosition != null && CurrentRadius <= 0f)
        {
            CurrentRadius = shaderPosition.radius;
        }
    }

    public void SetConfig(ReverseConfig reverseConfig)
    {
        config = reverseConfig;
    }

    public void SetTargetRadius(float target, bool immediate = false)
    {
        EnsureShaderPosition();
        if (shaderPosition == null)
        {
            return;
        }

        if (immediate || GetTransitionDuration() <= 0f)
        {
            ApplyRadiusImmediate(target);
            return;
        }

        transitionStartRadius = CurrentRadius > 0f ? CurrentRadius : shaderPosition.radius;
        transitionTargetRadius = target;
        transitionElapsed = 0f;
        isTransitioning = true;
    }

    public void ApplyRadiusImmediate(float radius)
    {
        EnsureShaderPosition();
        if (shaderPosition == null)
        {
            return;
        }

        CurrentRadius = radius;
        transitionTargetRadius = radius;
        isTransitioning = false;
        shaderPosition.radius = radius;
        RadiusChanged?.Invoke(radius);
    }

    private void Update()
    {
        if (!isTransitioning || shaderPosition == null)
        {
            return;
        }

        float duration = GetTransitionDuration();
        if (duration <= 0f)
        {
            ApplyRadiusImmediate(transitionTargetRadius);
            return;
        }

        transitionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(transitionElapsed / duration);
        float easedT = EvaluateTransitionCurve(t);

        CurrentRadius = Mathf.LerpUnclamped(transitionStartRadius, transitionTargetRadius, easedT);
        shaderPosition.radius = CurrentRadius;
        RadiusChanged?.Invoke(CurrentRadius);

        if (t >= 1f)
        {
            ApplyRadiusImmediate(transitionTargetRadius);
        }
    }

    private void EnsureShaderPosition()
    {
        if (shaderPosition != null)
        {
            return;
        }

        shaderPosition = GetComponent<ShaderPosition>();
    }

    private float GetTransitionDuration()
    {
        if (config != null)
        {
            return Mathf.Max(0f, config.viewRadiusTransitionDuration);
        }

        return Mathf.Max(0f, fallbackTransitionDuration);
    }

    private float EvaluateTransitionCurve(float normalizedTime)
    {
        AnimationCurve curve = config != null ? config.viewRadiusTransitionCurve : null;
        if (curve == null || curve.length == 0)
        {
            curve = fallbackTransitionCurve;
        }

        if (curve == null || curve.length == 0)
        {
            return normalizedTime;
        }

        return curve.Evaluate(normalizedTime);
    }
}
