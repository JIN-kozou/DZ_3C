using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// 持枪双手 IK：在角色上挂 <see cref="RigBuilder"/> + 左右 <see cref="TwoBoneIKConstraint"/>，Target 指向武器 grip 等 Transform；运行时仅由 <see cref="PlayerArmedPresentation"/> 调节 weight 与 RigBuilder.enabled。
/// </summary>
[DisallowMultipleComponent]
public class PlayerArmedHandIkRig : MonoBehaviour
{
    [SerializeField, Tooltip("可选。禁用时整段 Rig 不写入 PlayableGraph，省一点开销。")]
    private RigBuilder rigBuilder;

    [SerializeField, Tooltip("左手 Two Bone IK（Target 建议绑武器 grip 子物体）。")]
    private TwoBoneIKConstraint leftHandIk;

    [SerializeField, Tooltip("右手 Two Bone IK。")]
    private TwoBoneIKConstraint rightHandIk;

    public void SetRigBuilderEnabled(bool enabled)
    {
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
