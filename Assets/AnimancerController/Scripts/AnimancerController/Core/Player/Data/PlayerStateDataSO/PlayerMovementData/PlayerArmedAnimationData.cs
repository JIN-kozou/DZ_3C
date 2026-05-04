using Animancer;
using UnityEngine;

/// <summary>
/// 持枪上半身 Layer + 掏枪/持枪 idle（及可选收枪）过渡配置。未配置 mask 或 draw/idle 时由 <see cref="PlayerArmedPresentation"/> 退化为单层 moveLoop、不启 IK。
/// </summary>
[System.Serializable]
public class PlayerArmedAnimationData
{
    [Tooltip("Humanoid 上半身 AvatarMask；为空则不走 Layer1，与旧版单层持枪一致。")]
    public AvatarMask upperBodyMask;

    [Tooltip("可选。指定后持枪分层时 Layer0 的 moveLoop 仅驱动 Mask 内骨骼（常为下半身），与 Layer1 上半身/持枪/ADS 不抢同一批骨骼；不填则 Layer0 仍为全身。")]
    public AvatarMask locomotionBodyMask;

    [Tooltip("掏枪动画（Layer1）；无效则降级为仅 Layer0 moveLoop。")]
    public ClipTransition draw;

    [Tooltip("持枪 idle 循环（Layer1）；无效则降级为仅 Layer0 moveLoop。")]
    public ClipTransition armedIdle;

    [Tooltip("可选收枪动画；为空时收枪仅淡出 Layer1 与 IK。")]
    public ClipTransition holster;

    [Tooltip("上半身层与 Layer0 混合权重（0–1）。")]
    [Range(0f, 1f)]
    public float upperBodyLayerWeight = 1f;

    [Tooltip("进入持枪时从 Layer1 上一状态（或空层）淡入到 draw 的时长（秒）；>0 时先令 Layer1 权重为 0 再播放，以便层权重一并淡入。")]
    [Min(0f)]
    public float drawFadeInSeconds = 0.22f;

    [Tooltip("draw 结束后切到 armedIdle 的淡入时长（秒）。")]
    [Min(0f)]
    public float drawToIdleFadeSeconds = 0.15f;

    [Tooltip("掏枪结束后右手 IK 从 0 平滑到 1 的时长（秒）。左手在掏枪片段上随动画进度 0→1。")]
    [Min(0f)]
    public float ikFadeInSeconds = 0.2f;

    [Tooltip("收枪时右手 IK 平滑到 0 的时长（秒）。无 holster 片段时左手与右手一并按该速度衰减；有 holster 时左手随收枪片段进度 1→0。")]
    [Min(0f)]
    public float ikFadeOutSeconds = 0.15f;

    [Tooltip("主动收枪流程结束后，Layer0 切入空手 idle 的淡入秒数之一；与 holsterToBareHandsLocomotionFadeSeconds 取较大值后生效；二者均为 0 则不覆写 idle 的 Play 淡入。")]
    [Min(0f)]
    public float holsterExitToIdleLocomotionFadeSeconds = 0.28f;

    [Tooltip("收枪动画结束回空手 locomotion（idle）时 Layer0 的淡入秒数；与 holsterExitToIdleLocomotionFadeSeconds 取较大值，专门保证「持枪上半身 → 空手」不明显硬切。")]
    [Min(0f)]
    public float holsterToBareHandsLocomotionFadeSeconds = 0.35f;

    [Tooltip("无 holster 片段时 Layer1 权重淡出时长（秒）。")]
    [Min(0f)]
    public float layerFadeOutSeconds = 0.2f;

    [Tooltip("播放 holster 时从 Layer1 上一状态切入收枪片段的淡入时长（秒）。")]
    [Min(0f)]
    public float holsterFadeInSeconds = 0.12f;

    [Tooltip("收枪片段结束前将 Layer1 权重从当前值平滑降到 0 的时长（秒），与上半身淡出衔接；0 表示不在片段末尾单独做权重淡出（仍走 layerFadeOutSeconds）。")]
    [Min(0f)]
    public float holsterFadeOutSeconds = 0.18f;

    [Header("Weapon model (IArmedWeaponModelVisibility)")]
    [Tooltip("≤0：掏枪一开始就显示武器；>0：draw 播放到该帧（按片段 frameRate）时显示。")]
    public int drawWeaponModelVisibleAtFrame = 30;

    [Tooltip("≤0：收枪一开始就隐藏；>0：holster 播放到该帧时隐藏。")]
    public int holsterWeaponModelHiddenAtFrame = 26;

    [Header("ADS (上半身 Layer1，需与 draw/idle 同层)")]
    [Tooltip("进入 ADS 动画；播放速度由 PlayerNumericConfig.adsEnterAnimationSpeedScale 乘到片段上。")]
    public ClipTransition adsEnter;

    [Tooltip("ADS 下开火动画；建议循环 Clip 用于全自动按住开火，非循环则配合点射（每帧 WasPressed）。")]
    public ClipTransition adsFire;

    [Tooltip("退出 ADS 动画。")]
    public ClipTransition adsExit;

    [Tooltip("ADS 中不开火时的循环持镜 idle（与腰射 armedIdle 分离）；完整 ADS 动画包必填。")]
    public ClipTransition adsIdle;

    [Tooltip("可选。无 adsIdle 时的备用静止姿；若 adsIdle 已配置则通常不会用到。")]
    public ClipTransition adsHold;

    [Min(0f)]
    public float adsEnterFadeSeconds = 0.12f;

    [Min(0f)]
    public float adsExitFadeSeconds = 0.12f;

    [Tooltip("ADS 开火切入/切出淡入淡出（秒）；全自动循环开火时过短会与 CurrentState 判定打架导致每帧重播抽动，建议 ≥0.12。")]
    [Min(0f)]
    public float adsFireFadeSeconds = 0.14f;

    [Tooltip("从开火或其它片段切回 ADS idle 时的淡入淡出（秒）。")]
    [Min(0f)]
    public float adsIdleCrossFadeSeconds = 0.08f;

    public bool UseLayeredPresentation =>
        upperBodyMask != null &&
        draw != null &&
        draw.IsValid &&
        armedIdle != null &&
        armedIdle.IsValid;

    public bool HasHolsterTransition => holster != null && holster.IsValid;

    public bool HasAdsAnimationPack =>
        adsEnter != null &&
        adsEnter.IsValid &&
        adsFire != null &&
        adsFire.IsValid &&
        adsExit != null &&
        adsExit.IsValid &&
        adsIdle != null &&
        adsIdle.IsValid;
}
