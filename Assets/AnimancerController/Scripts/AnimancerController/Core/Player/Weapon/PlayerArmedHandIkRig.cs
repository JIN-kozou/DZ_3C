using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// 持枪双手 IK：在角色上挂 <see cref="RigBuilder"/> + 左右 <see cref="TwoBoneIKConstraint"/>，Target 指向武器 grip 等 Transform；运行时由 <see cref="PlayerArmedPresentation"/> 调节 IK weight，以及整包 RigBuilder 或单层 <see cref="Rig"/> 的开关。
/// 可选：Layer2 换片时由 <see cref="PlayerArmedPresentation"/> 调用 <see cref="NotifyLayer2OverlayPlayed"/>，立刻降低脊柱 Rig 权重并在 <see cref="Update"/> 中用 SmoothDamp 拉回，避免晚于 <see cref="PlayerArmedPresentation"/> 的 LateUpdate 检测错过与 Rig 求解的时序。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class PlayerArmedHandIkRig : MonoBehaviour
{
    [SerializeField, Tooltip("用于解析 RigBuilder；指定「仅单层」模式时也会用来保持 builder 开启。")]
    private RigBuilder rigBuilder;

    [SerializeField, Tooltip(
        "指定后：持枪 IK 开关只改该 Rig 的 weight（0/1），不再关掉整个 RigBuilder，这样 RigBuilder 里其它 Rig 层（例如第一层）可一直参与求解。" +
        "请拖入「持枪双手 IK 所在」的那一个 Rig（常见为 Rig Builder 列表里的第二项对应的 Rig 组件）。")]
    private Rig armedIkRigLayerOnly;

    [SerializeField, Tooltip(
        "可选。影响脊柱等上身的 Rig 层（如 Rig Builder 中的 Rig1）。由 Presentation 在每次 Layer2 Play 后调用 Notify；勿与「仅持枪 Rig 层」拖成同一 Rig。")]
    private Rig spineRigLayerForLayer2Transition;

    [SerializeField, Tooltip("Notify 后脊柱 Rig.weight 用 SmoothDamp 回到 1 的近似时长（秒）。建议约 0.08～0.18。")]
    [Min(0.001f)]
    private float spineRigLayer2TransitionSmoothTime = 0.12f;

    [SerializeField, Tooltip("Notify 当帧立即设成的脊柱 Rig.weight 起点。若全 0 仍抽，可试 0.2～0.4 保留部分约束。")]
    [Range(0f, 1f)]
    private float spineRigMinWeightOnLayer2Play = 0f;

    [SerializeField, Tooltip("左手 Two Bone IK（Target 建议绑武器 grip 子物体）。")]
    private TwoBoneIKConstraint leftHandIk;

    [SerializeField, Tooltip("右手 Two Bone IK。")]
    private TwoBoneIKConstraint rightHandIk;

    [SerializeField, Tooltip("使用「仅持枪 Rig 层」时，Rig.weight 在开关之间的平滑时间（秒）。0 表示立即切换。")]
    [Min(0f)]
    private float armedRigLayerBlendSeconds = 0.1f;

    private float _armedRigLayerSmoothedWeight;
    private float _armedRigLayerTargetWeight;
    private bool _armedRigLayerWeightsInitialized;

    private float _spineRigSmoothedWeight = 1f;
    private float _spineRigSmoothVelocity;
    private bool _spineRigRecoverActive;
    private bool _loggedSpineSameAsArmedRig;

    private void OnEnable()
    {
        if (armedIkRigLayerOnly != null)
        {
            armedIkRigLayerOnly.weight = 0f;
            _armedRigLayerSmoothedWeight = 0f;
            _armedRigLayerTargetWeight = 0f;
            _armedRigLayerWeightsInitialized = true;
        }

        _spineRigRecoverActive = false;
        _spineRigSmoothVelocity = 0f;
        if (spineRigLayerForLayer2Transition != null)
        {
            _spineRigSmoothedWeight = spineRigLayerForLayer2Transition.weight;
        }
        else
        {
            _spineRigSmoothedWeight = 1f;
        }
    }

    private void Update()
    {
        TickSpineRigLayer2Recover();
    }

    private void LateUpdate()
    {
        if (armedIkRigLayerOnly == null || armedRigLayerBlendSeconds <= 0.0001f)
        {
            return;
        }

        float step = Time.deltaTime / armedRigLayerBlendSeconds;
        _armedRigLayerSmoothedWeight = Mathf.MoveTowards(_armedRigLayerSmoothedWeight, _armedRigLayerTargetWeight, step);
        armedIkRigLayerOnly.weight = _armedRigLayerSmoothedWeight;
    }

    /// <summary>
    /// 由 <see cref="PlayerArmedPresentation"/> 在每次 Animancer Layer2 <c>Play</c> 之后立即调用，使脊柱 Rig 与换片在同一帧内先于后续 Rig 求解被压低。
    /// </summary>
    public void NotifyLayer2OverlayPlayed()
    {
        if (!CanRunSpineLayer2Stabilizer())
        {
            return;
        }

        float w = Mathf.Clamp01(spineRigMinWeightOnLayer2Play);
        _spineRigSmoothedWeight = w;
        _spineRigSmoothVelocity = 0f;
        spineRigLayerForLayer2Transition.weight = w;
        _spineRigRecoverActive = true;
    }

    private bool CanRunSpineLayer2Stabilizer()
    {
        if (spineRigLayerForLayer2Transition == null)
        {
            return false;
        }

        if (armedIkRigLayerOnly != null &&
            ReferenceEquals(spineRigLayerForLayer2Transition, armedIkRigLayerOnly))
        {
            if (!_loggedSpineSameAsArmedRig)
            {
                _loggedSpineSameAsArmedRig = true;
                Debug.LogWarning(
                    $"{nameof(PlayerArmedHandIkRig)}：{nameof(spineRigLayerForLayer2Transition)} 与 {nameof(armedIkRigLayerOnly)} 为同一 Rig，已跳过 Layer2 过渡稳定逻辑。",
                    this);
            }

            return false;
        }

        RigBuilder parentBuilder = spineRigLayerForLayer2Transition.GetComponentInParent<RigBuilder>();
        return parentBuilder != null && parentBuilder.enabled;
    }

    private void TickSpineRigLayer2Recover()
    {
        if (!_spineRigRecoverActive || spineRigLayerForLayer2Transition == null)
        {
            return;
        }

        RigBuilder parentBuilder = spineRigLayerForLayer2Transition.GetComponentInParent<RigBuilder>();
        if (parentBuilder == null || !parentBuilder.enabled)
        {
            return;
        }

        float t = Mathf.Max(0.001f, spineRigLayer2TransitionSmoothTime);
        _spineRigSmoothedWeight = Mathf.SmoothDamp(
            _spineRigSmoothedWeight,
            1f,
            ref _spineRigSmoothVelocity,
            t,
            Mathf.Infinity,
            Time.deltaTime);
        spineRigLayerForLayer2Transition.weight = _spineRigSmoothedWeight;
        if (_spineRigSmoothedWeight >= 0.999f)
        {
            spineRigLayerForLayer2Transition.weight = 1f;
            _spineRigSmoothedWeight = 1f;
            _spineRigRecoverActive = false;
            _spineRigSmoothVelocity = 0f;
        }
    }

    /// <summary>
    /// 若配置了 <see cref="armedIkRigLayerOnly"/>：保持 <see cref="RigBuilder"/> 开启，只把该层 <see cref="Rig.weight"/> 置 0 或 1。
    /// 否则：与旧版一致，整包 <see cref="RigBuilder.enabled"/>。
    /// </summary>
    public void SetRigBuilderEnabled(bool enabled)
    {
        if (armedIkRigLayerOnly != null)
        {
            RigBuilder builder = rigBuilder;
            if (builder == null)
            {
                builder = armedIkRigLayerOnly.GetComponentInParent<RigBuilder>();
            }

            if (builder != null)
            {
                builder.enabled = true;
            }

            if (!_armedRigLayerWeightsInitialized)
            {
                _armedRigLayerSmoothedWeight = armedIkRigLayerOnly.weight;
                _armedRigLayerWeightsInitialized = true;
            }

            _armedRigLayerTargetWeight = enabled ? 1f : 0f;
            if (armedRigLayerBlendSeconds <= 0.0001f)
            {
                _armedRigLayerSmoothedWeight = _armedRigLayerTargetWeight;
                armedIkRigLayerOnly.weight = _armedRigLayerTargetWeight;
            }

            return;
        }

        if (rigBuilder != null)
        {
            rigBuilder.enabled = enabled;
        }
    }

    public void SetLeftHandIkWeight(float weight)
    {
        if (leftHandIk == null)
        {
            return;
        }

        leftHandIk.weight = Mathf.Clamp01(weight);
    }

    public void SetRightHandIkWeight(float weight)
    {
        if (rightHandIk == null)
        {
            return;
        }

        rightHandIk.weight = Mathf.Clamp01(weight);
    }

    /// <summary>左右手使用同一权重（收枪末尾、复位等）。</summary>
    public void SetHandIkWeight(float weight)
    {
        float w = Mathf.Clamp01(weight);
        SetLeftHandIkWeight(w);
        SetRightHandIkWeight(w);
    }
}
