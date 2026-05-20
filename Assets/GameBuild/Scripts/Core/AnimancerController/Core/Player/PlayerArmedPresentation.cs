using System;
using System.Collections;
using Animancer;
using UnityEngine;

/// <summary>
/// 持枪 Animancer 三层：Layer0 下半身 locomotion；Layer1 腰射 <see cref="PlayerArmedAnimationData.armedIdle"/> 与 ADS idle 互切，其淡化与 Layer2 的 adsEnter / adsExit 同起止；
/// Layer2 覆盖式播放掏枪 / 收枪 / ADS enter·exit·fire。掏枪时 Layer1 即开始播腰射 idle，与 Layer2 掏枪叠加，减少单层抢同一 Mask 的抽搐。
/// 收枪时 Layer2 权重在 Layer1 淡出之后，与 Layer0 切入空手 idle 的淡入同时进行。
/// </summary>
[DisallowMultipleComponent]
public class PlayerArmedPresentation : MonoBehaviour
{
    [SerializeField, Tooltip("可选。实现 IArmedWeaponModelVisibility 的组件；为空时在角色子层级中查找第一个实现者。")]
    private MonoBehaviour weaponModelVisibilityOptional;

    [SerializeField, Tooltip("Layer2 覆盖层权重向目标值逼近的平滑时间（秒）。收枪协程内仍逐帧手写 Layer2 权重，此插值会暂停。")]
    [Min(0.001f)]
    private float overlayUpperWeightSmoothTime = 0.12f;

    private Player _player;
    private AnimancerComponent _animancer;
    private PlayerArmedHandIkRig _handIk;
    private GunAudio _gunAudio;
    private IArmedWeaponModelVisibility _weaponModelVisibility;
    private bool _weaponShownForCurrentDraw;
    private bool _weaponHiddenForCurrentHolster;
    private bool _weaponModelHiddenByCrouch;
    private AnimancerState _adsLoopingFireState;

    private PlayerArmedAnimationData _data;
    private bool _layeredArmedActive;
    private bool _simpleMoveLoopOnly;
    private Coroutine _exitRoutine;
    /// <summary>
    /// 在 <c>_exitRoutine = StartCoroutine(...)</c> 赋值前即为 true：协程首段跑到第一个 yield 的同一帧内 <see cref="_exitRoutine"/> 仍为 null，
    /// 否则 <see cref="TickOverlayUpperWeightSmoothing"/> 会误判未在收枪而把 Layer2 权重继续向旧 goal 平滑。
    /// </summary>
    private bool _armedExitCoroutinePending;
    private AnimancerState _drawOrHolsterState;

    /// <summary>右手 IK 平滑权重（掏枪结束后由 0 拉到 1；收枪协程里拉回 0）。左手见 <see cref="ComputeLeftHandIkWeight"/>。</summary>
    private float _rightIkWeight;
    private float _rightIkWeightTarget;

    private bool _readyForUpperBodyGameplay;
    private bool _adsInPose;
    private bool _adsEntering;
    private bool _adsExiting;
    private AnimancerState _adsEventState;

    /// <summary>Layer2 上一段非循环动画结束时缓存的片段与时间，供下次 Play 前作为淡入起点。</summary>
    private AnimationClip _layer2HoldClip;
    private float _layer2HoldTime;
    private bool _layer2HoldPoseStored;

    private float _overlayUpperWeightGoal;
    private float _overlayUpperWeightSmoothed;
    private float _overlayUpperWeightVel;

    /// <summary>1＝持枪本地位移 X 全额；收枪片段播放中由 1 线性减到 0；片段结束后保持 0 直至复位。</summary>
    private float _holsterArmedOffsetXMultiplier = 1f;

    /// <summary>无 holster 片段时，收枪协程内 Layer1 权重淡出进度 0→1，用于移速与淡出同步。</summary>
    private float _holsterLocomotionLayerFadeT01;

    /// <summary>无 holster 时仅在 <see cref="CoArmedExit"/> 的 Layer1 淡出循环内置 true。</summary>
    private bool _holsterExitLocomotionLayerFadeDriving;

    /// <summary>上半身基类层（索引 1）：仅 armedIdle / adsIdle。</summary>
    private AnimancerLayer UpperBodyBaseLayer =>
        _animancer != null && _animancer.Layers.Count > 1 ? _animancer.Layers[1] : null;

    /// <summary>上半身覆盖层（索引 2）：draw / holster / adsEnter / adsExit / adsFire。</summary>
    private AnimancerLayer UpperBodyOverlayLayer =>
        _animancer != null && _animancer.Layers.Count > 2 ? _animancer.Layers[2] : null;

    public bool IsExiting => _exitRoutine != null || _armedExitCoroutinePending;

    /// <summary>
    /// 供 <see cref="CameraPitchYOffset"/> 等：收枪片段播放中持枪本地位移 X 系数（1→0）。
    /// </summary>
    public float HolsterArmedOffsetXMultiplier => _holsterArmedOffsetXMultiplier;

    /// <summary>
    /// 分层持枪且配置了 ADS 包时：掏枪动画未结束或收枪流程中不允许 ADS 输入（上半身与武器运行时共用此门控）。
    /// 非分层 / 无 ADS 包时为 true，不改变其它模式行为。
    /// </summary>
    public bool IsAdsInputAllowed
    {
        get
        {
            if (!_layeredArmedActive || _simpleMoveLoopOnly || _data == null || !_data.HasAdsAnimationPack)
            {
                return true;
            }

            return _readyForUpperBodyGameplay && !IsExiting;
        }
    }

    /// <summary>
    /// 分层持枪且配置了完整 ADS 动画包时，镜头 FOV / 跟随距离应与 enter / exit 里程碑对齐，而非仅跟按键。
    /// </summary>
    public bool UsesAnimatedAdsCameraGates =>
        _layeredArmedActive &&
        _data != null &&
        _data.HasAdsAnimationPack;

    /// <summary>
    /// 开镜镜头：enter（含提前衔接）结束后为 true；关镜时于 exit 片段播放前即变为 false，镜头先回腰射。
    /// 仅在 <see cref="UsesAnimatedAdsCameraGates"/> 为真时由镜头脚本读取。
    /// </summary>
    public bool IsAdsCameraAimActive =>
        _adsInPose &&
        !_adsExiting;

    /// <summary>
    /// 腰射十字准心括号：1 = 满间距且不透明；0 = 完全开镜稳态时收拢且透明。
    /// 与开镜 enter / exit 片段进度及 <see cref="IsAdsCameraAimActive"/> 对齐；<paramref name="adsHeld"/> 应与 <see cref="PlayerWeaponRuntime.IsAds"/> 一致（HUD 可对乘子做 SmoothDamp，使收拢与渐隐同步）。
    /// </summary>
    public float EvaluateHipfireBracketDisplayMultiplier(bool adsHeld)
    {
        if (!adsHeld)
        {
            return 1f;
        }

        if (!UsesAnimatedAdsCameraGates || _data == null)
        {
            return 0f;
        }

        if (_adsEntering &&
            _adsEventState != null &&
            _data.adsEnter != null &&
            _data.adsEnter.IsValid &&
            _adsEventState.Clip == _data.adsEnter.Clip &&
            _adsEventState.IsPlaying)
        {
            return 1f - GetUpperLayerClipProgress01(_adsEventState);
        }

        if (_adsExiting &&
            _adsEventState != null &&
            _data.adsExit != null &&
            _data.adsExit.IsValid &&
            _adsEventState.Clip == _data.adsExit.Clip &&
            _adsEventState.IsPlaying)
        {
            return GetUpperLayerClipProgress01(_adsEventState);
        }

        if (IsAdsCameraAimActive)
        {
            return 0f;
        }

        return 0f;
    }

    /// <summary>
    /// 分层持枪时掏枪已播完且上半身进入 idle（可腰射开火）；非分层或未启用分层时视为始终就绪。
    /// </summary>
    public bool IsUpperBodyReadyForWeapon =>
        !_layeredArmedActive || _simpleMoveLoopOnly || _readyForUpperBodyGameplay;

    /// <summary>
    /// 掏枪动画结束后且未处于收枪协程中；收枪键、松移动触发的收枪等用。
    /// </summary>
    public bool IsHolsterInputAllowed => IsUpperBodyReadyForWeapon && !IsExiting;

    /// <summary>
    /// 掏枪结束后且未在收枪流程中；腰射/全自动开火 Tick 用（收枪片段与后续淡出期间均禁止）。
    /// </summary>
    public bool IsWeaponFireAllowed => IsUpperBodyReadyForWeapon && !IsExiting;

    /// <summary>
    /// 收枪协程未占用时可开始新一轮持枪分层（掏枪）；防止收枪未播完就再次掏枪。
    /// </summary>
    public bool CanBeginArmedPresentation => !IsExiting;

    /// <summary>
    /// 上一帧 <see cref="EvaluateLocomotionSpeedMultiplier"/> 是否按掏枪/收枪/ADS enter·exit 片段进度驱动；
    /// 为 true 时 <see cref="PlayerMovementState.UpdateSpeed"/> 应将 <c>speedValueParameter.CurrentValue</c> 与 Target 对齐以免 SmoothDamp 滞后。
    /// </summary>
    public bool LocomotionSpeedDrivenByWeaponAnimation { get; private set; }

    /// <summary>
    /// 地面 walk/run 目标速度乘数：空手为 1；持枪/收枪/开镜与对应动画时间轴对齐（见配置 <see cref="PlayerNumericConfig.armedLocomotionSpeedMultiplier"/> 等）。
    /// </summary>
    public float EvaluateLocomotionSpeedMultiplier(PlayerNumericConfig cfg, PlayerWeaponRuntime weaponRuntime)
    {
        LocomotionSpeedDrivenByWeaponAnimation = false;

        if (_player?.ReusableData == null)
        {
            return 1f;
        }

        bool armedMode = _player.ReusableData.armedModeActive;
        float armedMult = cfg != null ? Mathf.Max(0.01f, cfg.armedLocomotionSpeedMultiplier) : 1f;
        float adsMult = cfg != null ? Mathf.Max(0.01f, cfg.adsLocomotionSpeedMultiplier) : 1f;

        if (!armedMode && !IsExiting)
        {
            return 1f;
        }

        if (IsExiting)
        {
            if (_data == null)
            {
                return 1f;
            }

            if (_data.HasHolsterTransition &&
                _drawOrHolsterState != null &&
                _drawOrHolsterState.IsPlaying &&
                _data.holster != null &&
                _data.holster.IsValid &&
                _drawOrHolsterState.Clip == _data.holster.Clip)
            {
                float p = GetUpperLayerClipProgress01(_drawOrHolsterState);
                LocomotionSpeedDrivenByWeaponAnimation = true;
                return Mathf.Lerp(armedMult, 1f, p);
            }

            if (!_data.HasHolsterTransition && _holsterExitLocomotionLayerFadeDriving)
            {
                LocomotionSpeedDrivenByWeaponAnimation = true;
                return Mathf.Lerp(armedMult, 1f, _holsterLocomotionLayerFadeT01);
            }

            return 1f;
        }

        if (_simpleMoveLoopOnly || !_layeredArmedActive)
        {
            float m = armedMult;
            if (weaponRuntime != null && weaponRuntime.IsAds)
            {
                m *= adsMult;
            }

            return m;
        }

        if (_data == null)
        {
            return 1f;
        }

        if (!_readyForUpperBodyGameplay &&
            _data.draw != null &&
            _data.draw.IsValid &&
            _drawOrHolsterState != null &&
            _drawOrHolsterState.IsPlaying &&
            _drawOrHolsterState.Clip == _data.draw.Clip)
        {
            float p = GetUpperLayerClipProgress01(_drawOrHolsterState);
            LocomotionSpeedDrivenByWeaponAnimation = true;
            return Mathf.Lerp(1f, armedMult, p);
        }

        if (_data.HasAdsAnimationPack)
        {
            if (_adsEntering &&
                _adsEventState != null &&
                _data.adsEnter != null &&
                _data.adsEnter.IsValid &&
                _adsEventState.Clip == _data.adsEnter.Clip &&
                _adsEventState.IsPlaying)
            {
                float p = GetUpperLayerClipProgress01(_adsEventState);
                LocomotionSpeedDrivenByWeaponAnimation = true;
                return Mathf.Lerp(armedMult, armedMult * adsMult, p);
            }

            if (_adsExiting &&
                _adsEventState != null &&
                _data.adsExit != null &&
                _data.adsExit.IsValid &&
                _adsEventState.Clip == _data.adsExit.Clip &&
                _adsEventState.IsPlaying)
            {
                float p = GetUpperLayerClipProgress01(_adsEventState);
                LocomotionSpeedDrivenByWeaponAnimation = true;
                return Mathf.Lerp(armedMult * adsMult, armedMult, p);
            }

            if (_adsInPose)
            {
                return armedMult * adsMult;
            }

            return armedMult;
        }

        if (weaponRuntime != null && weaponRuntime.IsAds)
        {
            return armedMult * adsMult;
        }

        return armedMult;
    }

    public void Init(Player player)
    {
        _player = player;
        _animancer = player != null ? player.animancer : null;
        _handIk = GetComponent<PlayerArmedHandIkRig>();
        _gunAudio = player != null ? player.GetComponent<GunAudio>() : GetComponent<GunAudio>();
        ResolveWeaponModelVisibility(player);
    }

    private void ResolveWeaponModelVisibility(Player player)
    {
        _weaponModelVisibility = null;
        if (weaponModelVisibilityOptional != null && weaponModelVisibilityOptional is IArmedWeaponModelVisibility v)
        {
            _weaponModelVisibility = v;
            return;
        }

        if (player == null)
        {
            return;
        }

        foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IArmedWeaponModelVisibility w)
            {
                _weaponModelVisibility = w;
                return;
            }
        }
    }

    private void ApplyWeaponModelVisible(bool visible)
    {
        _weaponModelVisibility?.SetWeaponModelVisible(visible);
    }

    private static float FrameIndexToClipTimeSeconds(AnimationClip clip, int frameIndex)
    {
        if (clip == null || frameIndex <= 0)
        {
            return 0f;
        }

        float fps = clip.frameRate > 0.01f ? clip.frameRate : 60f;
        return frameIndex / fps;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _exitRoutine = null;
        _armedExitCoroutinePending = false;
        ClearDrawOrHolsterEndEvent();
        ForceResetVisuals();
    }

    /// <summary>
    /// 停收枪协程、清 draw/holster 回调与 ADS 子状态。
    /// <paramref name="preserveLayeredUpperBody"/> 为 true 时（持枪跳空中等）不清 Layer1 权重、不关 IK/Rig、不恢复 Layer0 Mask。
    /// </summary>
    private void StopExitAndClearUpperBodyHardware(bool preserveLayeredUpperBody = false)
    {
        if (_exitRoutine != null)
        {
            StopCoroutine(_exitRoutine);
            _exitRoutine = null;
            _armedExitCoroutinePending = false;
        }

        ClearDrawOrHolsterEndEvent();
        if (!preserveLayeredUpperBody)
        {
            ResetHolsterArmedOffsetXMultiplier();
            ResetAdsPresentationState();
            InvalidateLayer2HoldPose();
            _rightIkWeight = 0f;
            _rightIkWeightTarget = 0f;

            if (_animancer != null && _animancer.Layers.Count > 1)
            {
                _animancer.Layers[1].Weight = 0f;
            }

            if (_animancer != null && _animancer.Layers.Count > 2)
            {
                ResetOverlayUpperWeightSmoothing(0f);
            }

            _handIk?.SetHandIkWeight(0f);
            _handIk?.SetRigBuilderEnabled(false);
            RestoreLayer0DefaultMask();
            ApplyWeaponModelVisible(false);
            _weaponShownForCurrentDraw = false;
            _weaponHiddenForCurrentHolster = false;
            _weaponModelHiddenByCrouch = false;
        }
        else
        {
            ResetHolsterArmedOffsetXMultiplier();
            ResetAdsPresentationState();
        }
    }

    private static void ApplyLocomotionMaskForLayer0(AnimancerLayerList layers, AvatarMask locomotionBodyMask)
    {
        layers.SetMask(0, locomotionBodyMask);
    }

    /// <summary>恢复 Layer0 默认 AvatarMask（全身），供非持枪分层状态继续使用同一 Animancer。</summary>
    private void RestoreLayer0DefaultMask()
    {
        if (_animancer == null || _animancer.Layers.Count < 1)
        {
            return;
        }

        _animancer.Layers.SetMask(0, null);
    }

    /// <summary>
    /// 进入持枪：分层时只配置 Mask 与 Layer1，不覆盖 Layer0 当前 locomotion 片段；非分层降级时 Layer0 播 <paramref name="moveLoop"/>。
    /// <paramref name="resumeLayeredPresentationWithoutDraw"/>：仍持枪但从跳跃等回到持枪态时，不重新掏枪，仅同步 Mask。
    /// </summary>
    public void BeginArmedEnter(TransitionAsset moveLoop, PlayerArmedAnimationData data, bool resumeLayeredPresentationWithoutDraw = false)
    {
        if (_animancer == null)
        {
            return;
        }

        // 收枪协程未结束前不允许再次掏枪/进入持枪分层（此前会先 StopExit 打断收枪，与「播完前不能掏枪」冲突）。
        if (IsExiting)
        {
            return;
        }

        bool layeredConfigured = data != null && data.UseLayeredPresentation;
        if (!layeredConfigured && moveLoop == null)
        {
            return;
        }

        bool preserveUpper = resumeLayeredPresentationWithoutDraw && layeredConfigured;
        StopExitAndClearUpperBodyHardware(preserveUpper);
        if (!preserveUpper)
        {
            InvalidateLayer2HoldPose();
        }

        if (preserveUpper)
        {
            _data = data;
            _simpleMoveLoopOnly = false;
            _layeredArmedActive = true;
            _readyForUpperBodyGameplay = true;

            _animancer.Layers.SetMinCount(3);
            if (data.locomotionBodyMask != null)
            {
                ApplyLocomotionMaskForLayer0(_animancer.Layers, data.locomotionBodyMask);
            }
            else
            {
                RestoreLayer0DefaultMask();
            }

            _animancer.Layers.SetMask(1, data.upperBodyMask);
            _animancer.Layers.SetMask(2, data.upperBodyMask);
            var baseL = _animancer.Layers[1];
            var overlayL = _animancer.Layers[2];
            ResetOverlayUpperWeightSmoothing(0f);
            if (baseL.Weight < 0.001f)
            {
                baseL.Weight = data.upperBodyLayerWeight;
            }

            _handIk?.SetRigBuilderEnabled(true);
            _rightIkWeightTarget = 1f;
            ApplyWeaponModelVisible(true);
            _weaponShownForCurrentDraw = true;
            _weaponHiddenForCurrentHolster = false;
            return;
        }

        _gunAudio?.OnDraw();

        _layeredArmedActive = false;
        _simpleMoveLoopOnly = false;
        _readyForUpperBodyGameplay = false;

        _data = data;

        if (data == null || !data.UseLayeredPresentation)
        {
            _simpleMoveLoopOnly = true;
            _layeredArmedActive = false;
            _animancer.Layers.SetMinCount(1);
            RestoreLayer0DefaultMask();
            _animancer.Play(moveLoop);
            return;
        }

        _animancer.Layers.SetMinCount(3);
        if (data.locomotionBodyMask != null)
        {
            ApplyLocomotionMaskForLayer0(_animancer.Layers, data.locomotionBodyMask);
        }
        else
        {
            RestoreLayer0DefaultMask();
        }

        _animancer.Layers.SetMask(1, data.upperBodyMask);
        _animancer.Layers.SetMask(2, data.upperBodyMask);
        var baseLayer = _animancer.Layers[1];
        var overlayLayer = _animancer.Layers[2];
        baseLayer.Weight = data.upperBodyLayerWeight;
        ResetOverlayUpperWeightSmoothing(0f);

        float idleFadeUnderDraw = Mathf.Max(0.01f, data.drawToIdleFadeSeconds);
        baseLayer.Play(data.armedIdle, idleFadeUnderDraw);

        _handIk?.SetRigBuilderEnabled(true);
        _handIk?.SetHandIkWeight(0f);

        _weaponShownForCurrentDraw = false;
        _weaponHiddenForCurrentHolster = false;
        ApplyWeaponModelVisible(false);
        if (data.drawWeaponModelVisibleAtFrame <= 0)
        {
            ApplyWeaponModelVisible(true);
            _weaponShownForCurrentDraw = true;
        }

        _layeredArmedActive = true;
        float drawIn = Mathf.Max(0f, data.drawFadeInSeconds);
        if (drawIn <= 0f)
        {
            ResetOverlayUpperWeightSmoothing(data.upperBodyLayerWeight);
        }
        else
        {
            SetOverlayUpperWeightGoal(data.upperBodyLayerWeight);
        }

        TryApplyLayer2HoldPoseBeforePlay(overlayLayer);
        _drawOrHolsterState = drawIn > 0f
            ? overlayLayer.Play(data.draw, drawIn)
            : overlayLayer.Play(data.draw);
        RestorePresentationOverlayLayerWeight(overlayLayer);
        NotifyLayer2OverlayRigStabilizer();

        if (_drawOrHolsterState != null)
        {
            _drawOrHolsterState.Events(this).OnEnd = OnDrawFinished;
        }
        else
        {
            OnDrawFinished();
        }
    }

    private void OnDrawFinished()
    {
        if (!_layeredArmedActive || _data == null)
        {
            return;
        }

        if (_readyForUpperBodyGameplay)
        {
            return;
        }

        CaptureLayer2HoldPoseFromState(_drawOrHolsterState);
        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();
        var baseL = UpperBodyBaseLayer;
        SetOverlayUpperWeightGoal(0f);

        if (baseL != null)
        {
            baseL.Weight = _data.upperBodyLayerWeight;
            baseL.Play(_data.armedIdle, _data.drawToIdleFadeSeconds);
        }

        _rightIkWeightTarget = 1f;
        _readyForUpperBodyGameplay = true;
        ResetHolsterArmedOffsetXMultiplier();
    }

    /// <summary>持枪且上半身就绪时由 <see cref="PlayerArmedState"/> 每帧调用；<paramref name="adsEnterSpeedScale"/> 来自角色数值配置。</summary>
    public void TickUpperBodyAds(bool adsHeld, bool fireHeld, bool firePressedThisFrame, float adsEnterSpeedScale)
    {
        if (!_layeredArmedActive ||
            _simpleMoveLoopOnly ||
            _data == null ||
            !_readyForUpperBodyGameplay ||
            !_data.HasAdsAnimationPack ||
            IsExiting)
        {
            return;
        }

        var baseL = UpperBodyBaseLayer;
        var overlayL = UpperBodyOverlayLayer;
        if (baseL == null || overlayL == null)
        {
            return;
        }

        if (!adsHeld)
        {
            if (_adsInPose || _adsEntering)
            {
                TryBeginAdsExit(overlayL);
            }

            return;
        }

        if (_adsExiting)
        {
            return;
        }

        if (!_adsInPose && !_adsEntering)
        {
            BeginAdsEnter(overlayL, adsEnterSpeedScale);
            return;
        }

        if (_adsEntering)
        {
            // 实际收尾在 LateUpdate 中根据片段进度检测（Animancer 先于本脚本更新），避免仅依赖 OnEnd 时漏接 adsIdle。
            return;
        }

        if (fireHeld)
        {
            int shotsThisFrame = _player != null && _player.WeaponRuntime != null
                ? _player.WeaponRuntime.SuccessfulShotsLastTick
                : 0;

            bool fireLooping = _data.adsFire.Clip != null && _data.adsFire.Clip.isLooping;
            if (fireLooping)
            {
                if (_adsLoopingFireState != null &&
                    _adsLoopingFireState.IsPlaying &&
                    _adsLoopingFireState.Clip == _data.adsFire.Clip)
                {
                    return;
                }

                if (shotsThisFrame <= 0)
                {
                    return;
                }

                ClearAdsEventState();
                float fireIn = Mathf.Max(
                    _data.adsFireFadeSeconds,
                    _data.adsIdleCrossFadeSeconds * 0.5f);
                SetOverlayUpperWeightGoal(_data.upperBodyLayerWeight);
                TryApplyLayer2HoldPoseBeforePlay(overlayL);
                _adsLoopingFireState = overlayL.Play(_data.adsFire, fireIn);
                RestorePresentationOverlayLayerWeight(overlayL);
                NotifyLayer2OverlayRigStabilizer();
            }
            else if (firePressedThisFrame)
            {
                if (shotsThisFrame <= 0)
                {
                    return;
                }

                _adsLoopingFireState = null;
                ClearAdsEventState();
                float fireIn = Mathf.Max(
                    _data.adsFireFadeSeconds,
                    _data.adsIdleCrossFadeSeconds * 0.5f);
                SetOverlayUpperWeightGoal(_data.upperBodyLayerWeight);
                TryApplyLayer2HoldPoseBeforePlay(overlayL);
                var fireState = overlayL.Play(_data.adsFire, fireIn);
                RestorePresentationOverlayLayerWeight(overlayL);
                NotifyLayer2OverlayRigStabilizer();
                _adsEventState = fireState;
                fireState.Events(this).OnEnd = OnAdsFireClipFinished;
            }
        }
        else if (overlayL.CurrentState != null && overlayL.CurrentState.Clip == _data.adsFire.Clip)
        {
            _adsLoopingFireState = null;
            ClearAdsEventState();
            float backFade = Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
            SetOverlayUpperWeightGoal(0f);
            PlayAdsIdleOrFallback(baseL, backFade);
        }
        else if (_adsInPose)
        {
            EnsureAdsIdlePlaying(baseL);
        }
    }

    private void BeginAdsEnter(AnimancerLayer overlayLayer, float adsEnterSpeedScale)
    {
        if (!IsAdsInputAllowed)
        {
            return;
        }

        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsEntering = true;
        _adsInPose = false;
        SetOverlayUpperWeightGoal(_data.upperBodyLayerWeight);
        TryApplyLayer2HoldPoseBeforePlay(overlayLayer);
        var st = overlayLayer.Play(_data.adsEnter, _data.adsEnterFadeSeconds);
        RestorePresentationOverlayLayerWeight(overlayLayer);
        NotifyLayer2OverlayRigStabilizer();
        float scale = Mathf.Max(0.05f, adsEnterSpeedScale);
        st.Speed = _data.adsEnter.Speed * scale;
        _adsEventState = st;
        st.Events(this).OnEnd = OnAdsEnterFinished;

        // Layer1 腰射 idle → ADS idle 的过渡与 adsEnter 同起止，避免 enter 播完后再用长淡入接 idle。
        var baseL = UpperBodyBaseLayer;
        if (baseL != null)
        {
            float blendSeconds = GetAdsOverlayAlignedBaseBlendSeconds(st, _data.adsIdleCrossFadeSeconds);
            PlayAdsIdleOrFallback(baseL, blendSeconds);
        }
    }

    private void OnAdsEnterFinished()
    {
        if (!_adsEntering || _data == null || !_layeredArmedActive)
        {
            return;
        }

        CompleteAdsEnterToIdle();
    }

    /// <summary>
    /// 开镜 enter 播完：切 ADS idle。由 OnEnd 与 LateUpdate 进度检测共用；第二次调用因 _adsEntering 已为 false 直接忽略。
    /// </summary>
    private void CompleteAdsEnterToIdle()
    {
        if (!_adsEntering ||
            _data == null ||
            !_layeredArmedActive ||
            _animancer == null ||
            _animancer.Layers.Count < 3)
        {
            return;
        }

        CaptureLayer2HoldPoseFromState(_adsEventState);
        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsEntering = false;
        _adsInPose = true;
        SetOverlayUpperWeightGoal(0f);

        var baseL = UpperBodyBaseLayer;
        if (baseL != null)
        {
            var idleTrans = ResolveAdsIdleTransition();
            var idleClip = idleTrans != null && idleTrans.IsValid ? idleTrans.Clip : null;
            var cur = baseL.CurrentState;
            if (idleClip != null && (cur == null || cur.Clip != idleClip))
            {
                PlayAdsIdleOrFallback(baseL, Mathf.Max(0.01f, _data.adsIdleCrossFadeSeconds));
            }
        }
    }

    /// <summary>
    /// 非循环 adsEnter：剩余时间 ≤ 衔接淡入时提前切入 adsIdle，与上一段尾部重叠交叉淡化（Animancer Play 权重互补）。
    /// </summary>
    private void TryCompleteAdsEnterByProgress()
    {
        if (!_adsEntering || _data == null || !_layeredArmedActive || _animancer == null || _animancer.Layers.Count < 3)
        {
            return;
        }

        var enterClip = _data.adsEnter != null ? _data.adsEnter.Clip : null;
        if (enterClip == null || _adsEventState == null)
        {
            return;
        }

        if (_adsEventState.Clip != enterClip || _adsEventState.IsLooping)
        {
            return;
        }

        float overlapFade = Mathf.Max(_data.adsEnterFadeSeconds, _data.adsIdleCrossFadeSeconds);
        if (!ShouldStartEarlyClipHandoff(_adsEventState, overlapFade))
        {
            return;
        }

        CompleteAdsEnterToIdle();
    }

    private void OnAdsFireClipFinished()
    {
        if (!_adsInPose || _data == null || !_layeredArmedActive)
        {
            return;
        }

        CompleteAdsFireToIdle();
    }

    /// <summary>
    /// 点射等非循环 adsFire 播完接 ADS idle；与进度提前衔接共用，防 OnEnd 多次触发。
    /// </summary>
    private void CompleteAdsFireToIdle()
    {
        if (!_adsInPose || _data == null || !_layeredArmedActive || _animancer == null || _animancer.Layers.Count < 3)
        {
            return;
        }

        CaptureLayer2HoldPoseFromState(_adsEventState ?? _adsLoopingFireState);
        _adsLoopingFireState = null;
        ClearAdsEventState();
        float backFade = Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
        SetOverlayUpperWeightGoal(0f);

        var baseL = UpperBodyBaseLayer;
        if (baseL != null)
        {
            PlayAdsIdleOrFallback(baseL, backFade);
        }
    }

    private void TryBeginAdsExit(AnimancerLayer overlayLayer)
    {
        if (_adsExiting || _data == null)
        {
            return;
        }

        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsEntering = false;
        _adsInPose = false;
        _adsExiting = true;
        SetOverlayUpperWeightGoal(_data.upperBodyLayerWeight);
        TryApplyLayer2HoldPoseBeforePlay(overlayLayer);
        var st = overlayLayer.Play(_data.adsExit, _data.adsExitFadeSeconds);
        RestorePresentationOverlayLayerWeight(overlayLayer);
        NotifyLayer2OverlayRigStabilizer();
        _adsEventState = st;
        st.Events(this).OnEnd = OnAdsExitFinished;

        // Layer1 ADS idle → 腰射 idle 与 adsExit 同起止。
        var baseL = UpperBodyBaseLayer;
        if (baseL != null && _data.armedIdle != null && _data.armedIdle.IsValid)
        {
            float blendSeconds = GetAdsOverlayAlignedBaseBlendSeconds(st, _data.adsIdleCrossFadeSeconds);
            baseL.Play(_data.armedIdle, blendSeconds);
        }
    }

    private void OnAdsExitFinished()
    {
        if (!_adsExiting || _data == null || !_layeredArmedActive)
        {
            return;
        }

        CompleteAdsExitToHipIdle();
    }

    /// <summary>
    /// 关镜 exit 播完：切腰射 armedIdle。Animancer OnEnd 可能多次触发，须用 _adsExiting 防重复 Play。
    /// </summary>
    private void CompleteAdsExitToHipIdle()
    {
        if (!_adsExiting ||
            _data == null ||
            !_layeredArmedActive ||
            _animancer == null ||
            _animancer.Layers.Count < 3)
        {
            return;
        }

        CaptureLayer2HoldPoseFromState(_adsEventState);
        ClearAdsEventState();
        _adsExiting = false;
        SetOverlayUpperWeightGoal(0f);

        var baseL = UpperBodyBaseLayer;
        if (baseL != null && _data.armedIdle != null && _data.armedIdle.IsValid)
        {
            var hipClip = _data.armedIdle.Clip;
            var cur = baseL.CurrentState;
            if (hipClip != null && (cur == null || cur.Clip != hipClip))
            {
                baseL.Play(_data.armedIdle, Mathf.Max(0.01f, _data.adsIdleCrossFadeSeconds));
            }
        }
    }

    private ClipTransition ResolveAdsIdleTransition()
    {
        if (_data.adsIdle != null && _data.adsIdle.IsValid)
        {
            return _data.adsIdle;
        }

        if (_data.adsHold != null && _data.adsHold.IsValid)
        {
            return _data.adsHold;
        }

        return _data.armedIdle;
    }

    private void PlayAdsIdleOrFallback(AnimancerLayer upper, float fadeSeconds)
    {
        if (_data == null)
        {
            return;
        }

        upper.Play(ResolveAdsIdleTransition(), fadeSeconds);
    }

    private void EnsureAdsIdlePlaying(AnimancerLayer upper)
    {
        if (_data == null)
        {
            return;
        }

        var idle = ResolveAdsIdleTransition();
        var cur = upper.CurrentState;
        if (cur == null || cur.Clip == idle.Clip)
        {
            return;
        }

        // 与 CompleteAdsEnterToIdle / 开火回落 使用一致的淡入时长，避免比当前淡入更短的二次 Play 触发 Animancer 重启淡入而抽搐。
        float fade = GetFadeSecondsBlendingToAdsIdle(cur);
        ClearAdsEventState();
        upper.Play(idle, fade);
    }

    private float GetFadeSecondsBlendingToAdsIdle(AnimancerState cur)
    {
        if (_data == null)
        {
            return 0.1f;
        }

        if (cur == null || cur.Clip == null)
        {
            return _data.adsIdleCrossFadeSeconds;
        }

        if (_data.adsEnter != null && _data.adsEnter.IsValid && cur.Clip == _data.adsEnter.Clip)
        {
            return Mathf.Max(_data.adsEnterFadeSeconds, _data.adsIdleCrossFadeSeconds);
        }

        if (_data.adsFire != null && _data.adsFire.IsValid && cur.Clip == _data.adsFire.Clip)
        {
            return Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
        }

        return _data.adsIdleCrossFadeSeconds;
    }

    private void ResetAdsPresentationState()
    {
        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsInPose = false;
        _adsEntering = false;
        _adsExiting = false;
    }

    private void ClearAdsEventState()
    {
        if (_adsEventState == null)
        {
            return;
        }

        _adsEventState.Events(this).OnEnd = null;
        _adsEventState = null;
    }

    private void LateUpdate()
    {
        // AnimancerComponent 默认 ExecutionOrder 为 -5000，先于本组件更新；此处用 RemainingDuration 做「提前衔接」与 OnEnd 兜底。
        TryCompleteDrawToArmedIdleByProgress();
        TryCompleteAdsEnterByProgress();
        TryCompleteAdsFireToIdleByProgress();
        TryCompleteAdsExitByProgress();
        TickOverlayUpperWeightSmoothing();
    }

    private void ResetOverlayUpperWeightSmoothing(float absoluteWeight)
    {
        _overlayUpperWeightGoal = absoluteWeight;
        _overlayUpperWeightSmoothed = absoluteWeight;
        _overlayUpperWeightVel = 0f;
        if (UpperBodyOverlayLayer != null)
        {
            UpperBodyOverlayLayer.Weight = absoluteWeight;
        }
    }

    private void SetOverlayUpperWeightGoal(float absoluteWeight)
    {
        _overlayUpperWeightGoal = absoluteWeight;
    }

    private void TickOverlayUpperWeightSmoothing()
    {
        if (!_layeredArmedActive || _simpleMoveLoopOnly || _data == null || IsExiting)
        {
            return;
        }

        var o = UpperBodyOverlayLayer;
        if (o == null)
        {
            return;
        }

        float t = Mathf.Max(0.001f, overlayUpperWeightSmoothTime);
        _overlayUpperWeightSmoothed = Mathf.SmoothDamp(
            _overlayUpperWeightSmoothed,
            _overlayUpperWeightGoal,
            ref _overlayUpperWeightVel,
            t,
            Mathf.Infinity,
            Time.deltaTime);
        o.Weight = _overlayUpperWeightSmoothed;
    }

    private void InvalidateLayer2HoldPose()
    {
        _layer2HoldPoseStored = false;
        _layer2HoldClip = null;
        _layer2HoldTime = 0f;
    }

    private void CaptureLayer2HoldPoseFromState(AnimancerState state)
    {
        if (!_layeredArmedActive || state == null || state.Clip == null || state.Clip.isLooping)
        {
            return;
        }

        float length = state.Length > 1e-5f
            ? state.Length
            : (state.Clip.length > 1e-5f ? state.Clip.length : 0f);
        if (length <= 1e-5f)
        {
            return;
        }

        _layer2HoldClip = state.Clip;
        _layer2HoldTime = Mathf.Clamp(state.Time, 0f, length - 1e-5f);
        _layer2HoldPoseStored = true;
    }

    /// <summary>
    /// 收枪片段自然结束后冻结最后一帧：复用当前状态，避免再 <c>Play(clip)</c> 叠出第二个同名状态。
    /// </summary>
    private void TryFreezeHolsterOverlayStateInPlace(AnimancerLayer overlay, AnimancerState holsterState)
    {
        if (overlay == null ||
            !_layeredArmedActive ||
            holsterState == null ||
            holsterState.Clip == null ||
            _data == null ||
            _data.holster == null ||
            !_data.holster.IsValid ||
            holsterState.Clip != _data.holster.Clip ||
            overlay.CurrentState != holsterState)
        {
            TryApplyLayer2HoldPoseBeforePlay(overlay);
            return;
        }

        float len = holsterState.Length > 1e-5f
            ? holsterState.Length
            : (holsterState.Clip.length > 1e-5f ? holsterState.Clip.length : 0f);
        if (len > 1e-5f)
        {
            float holdTime = _layer2HoldPoseStored ? _layer2HoldTime : (float)holsterState.Time;
            holsterState.Time = Mathf.Clamp(holdTime, 0f, len - 1e-5f);
        }

        holsterState.IsPlaying = false;
        RestorePresentationOverlayLayerWeight(overlay);
        NotifyLayer2OverlayRigStabilizer();
    }

    private void TryApplyLayer2HoldPoseBeforePlay(AnimancerLayer overlay)
    {
        if (overlay == null || !_layer2HoldPoseStored || _layer2HoldClip == null || !_layeredArmedActive)
        {
            return;
        }

        AnimancerState holdSt = overlay.Play(_layer2HoldClip, 0f);
        if (holdSt == null)
        {
            return;
        }

        float length = holdSt.Length > 1e-5f
            ? holdSt.Length
            : (_layer2HoldClip.length > 1e-5f ? _layer2HoldClip.length : 0f);
        if (length > 1e-5f)
        {
            holdSt.Time = Mathf.Clamp(_layer2HoldTime, 0f, length - 1e-5f);
        }
        else
        {
            holdSt.Time = 0f;
        }

        holdSt.IsPlaying = false;
        RestorePresentationOverlayLayerWeight(overlay);
        NotifyLayer2OverlayRigStabilizer();
    }

    private void RestorePresentationOverlayLayerWeight(AnimancerLayer overlay)
    {
        if (overlay == null)
        {
            return;
        }

        overlay.Weight = _overlayUpperWeightSmoothed;
    }

    private void NotifyLayer2OverlayRigStabilizer()
    {
        _handIk?.NotifyLayer2OverlayPlayed();
    }

    /// <summary>
    /// 非循环 adsExit：在剩余时间 ≤ 衔接淡入时提前切入腰射 armedIdle，与上一段尾部重叠交叉淡化。
    /// </summary>
    private void TryCompleteAdsExitByProgress()
    {
        if (!_adsExiting ||
            _data == null ||
            !_layeredArmedActive ||
            _animancer == null ||
            _animancer.Layers.Count < 3)
        {
            return;
        }

        var exitClip = _data.adsExit != null ? _data.adsExit.Clip : null;
        if (exitClip == null || _adsEventState == null)
        {
            return;
        }

        if (_adsEventState.Clip != exitClip || _adsEventState.IsLooping)
        {
            return;
        }

        float overlapFade = Mathf.Max(_data.adsExitFadeSeconds, _data.adsIdleCrossFadeSeconds);
        if (!ShouldStartEarlyClipHandoff(_adsEventState, overlapFade))
        {
            return;
        }

        CompleteAdsExitToHipIdle();
    }

    /// <summary>
    /// 非循环 adsFire：提前衔接回 ADS idle（与 OnAdsFireClipFinished 相同目标）。
    /// </summary>
    private void TryCompleteAdsFireToIdleByProgress()
    {
        if (!_adsInPose ||
            _adsEntering ||
            _adsExiting ||
            _data == null ||
            !_layeredArmedActive ||
            _animancer == null ||
            _animancer.Layers.Count < 3)
        {
            return;
        }

        var fireClip = _data.adsFire != null ? _data.adsFire.Clip : null;
        if (fireClip == null || _adsEventState == null)
        {
            return;
        }

        if (_adsEventState.Clip != fireClip || _adsEventState.IsLooping)
        {
            return;
        }

        float overlapFade = Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
        if (!ShouldStartEarlyClipHandoff(_adsEventState, overlapFade))
        {
            return;
        }

        CompleteAdsFireToIdle();
    }

    /// <summary>
    /// 掏枪 draw：在剩余时间 ≤ draw→idle 淡入秒数时提前切入 armedIdle。
    /// </summary>
    private void TryCompleteDrawToArmedIdleByProgress()
    {
        if (!_layeredArmedActive ||
            _simpleMoveLoopOnly ||
            _data == null ||
            _readyForUpperBodyGameplay ||
            _animancer == null ||
            _animancer.Layers.Count < 3)
        {
            return;
        }

        if (_data.draw == null || !_data.draw.IsValid || _drawOrHolsterState == null)
        {
            return;
        }

        if (_drawOrHolsterState.Clip != _data.draw.Clip || _drawOrHolsterState.IsLooping)
        {
            return;
        }

        float overlapFade = Mathf.Max(0.0001f, _data.drawToIdleFadeSeconds);
        if (!ShouldStartEarlyClipHandoff(_drawOrHolsterState, overlapFade))
        {
            return;
        }

        OnDrawFinished();
    }

    /// <summary>
    /// 当前非循环片段剩余播放时间（按 EffectiveSpeed）是否已进入与下一段的交叉淡化窗口。
    /// </summary>
    private static bool ShouldStartEarlyClipHandoff(AnimancerState state, float overlapFadeSeconds)
    {
        if (state == null || state.IsLooping || !state.IsPlaying)
        {
            return false;
        }

        overlapFadeSeconds = Mathf.Max(0.0001f, overlapFadeSeconds);
        return state.RemainingDuration <= overlapFadeSeconds + 0.001f;
    }

    /// <summary>
    /// Layer1 与 Layer2 的 adsEnter/adsExit 对齐的淡化秒数：优先用当前状态的 RemainingDuration，异常时用片段长度/速度或回退值。
    /// </summary>
    private static float GetAdsOverlayAlignedBaseBlendSeconds(AnimancerState st, float fallbackSeconds)
    {
        if (st == null)
        {
            return Mathf.Max(0.01f, fallbackSeconds);
        }

        float rd = st.RemainingDuration;
        if (rd > 0.01f && !float.IsInfinity(rd) && !float.IsNaN(rd))
        {
            return rd;
        }

        float spd = Mathf.Abs(st.EffectiveSpeed);
        if (spd < 0.001f)
        {
            spd = 1f;
        }

        if (!st.IsLooping && st.Length > 1e-5f)
        {
            return Mathf.Max(0.01f, st.Length / spd);
        }

        return Mathf.Max(0.01f, fallbackSeconds);
    }

    private void Update()
    {
        if (!_layeredArmedActive || _data == null || _simpleMoveLoopOnly)
        {
            return;
        }

        var ikDrivingState = ResolveHandIkDrivingState();

        float dt = Time.deltaTime;
        float speedIn = _data.ikFadeInSeconds > 0f ? dt / _data.ikFadeInSeconds : 1f;
        float speedOut = _data.ikFadeOutSeconds > 0f ? dt / _data.ikFadeOutSeconds : 1f;
        float delta = _rightIkWeightTarget > _rightIkWeight ? speedIn : speedOut;
        _rightIkWeight = Mathf.MoveTowards(_rightIkWeight, _rightIkWeightTarget, delta);

        float leftW = ComputeLeftHandIkWeight(ikDrivingState);
        _handIk?.SetLeftHandIkWeight(leftW);
        _handIk?.SetRightHandIkWeight(_rightIkWeight);
        TickWeaponModelVisibility(ikDrivingState);
        TickCrouchWeaponModelVisibility();
    }

    /// <summary>掏枪/收枪/ADS 覆盖层在播时优先用其状态驱动 IK 与武器显隐时间轴。</summary>
    private AnimancerState ResolveHandIkDrivingState()
    {
        if (_drawOrHolsterState != null && _drawOrHolsterState.IsPlaying)
        {
            return _drawOrHolsterState;
        }

        if (_adsEventState != null && _adsEventState.IsPlaying)
        {
            return _adsEventState;
        }

        return UpperBodyBaseLayer != null ? UpperBodyBaseLayer.CurrentState : null;
    }

    private void TickWeaponModelVisibility(AnimancerState cur)
    {
        if (_weaponModelVisibility == null || _data == null || !_layeredArmedActive || _simpleMoveLoopOnly)
        {
            return;
        }

        TickDrawWeaponModelVisibility(cur);
        TickHolsterWeaponModelVisibility(cur);
    }

    private void TickDrawWeaponModelVisibility(AnimancerState cur)
    {
        if (_weaponShownForCurrentDraw ||
            _data.draw == null ||
            !_data.draw.IsValid ||
            _readyForUpperBodyGameplay ||
            _data.drawWeaponModelVisibleAtFrame <= 0)
        {
            return;
        }

        var drawState = _drawOrHolsterState;
        if (drawState == null || drawState.Clip == null || drawState.Clip != _data.draw.Clip)
        {
            return;
        }

        float threshold = FrameIndexToClipTimeSeconds(_data.draw.Clip, _data.drawWeaponModelVisibleAtFrame);
        if (drawState.Time >= threshold)
        {
            ApplyWeaponModelVisible(true);
            _weaponShownForCurrentDraw = true;
        }
    }

    private void TickCrouchWeaponModelVisibility()
    {
        if (_weaponModelVisibility == null || _player?.ReusableData == null)
        {
            return;
        }

        if (!_player.ReusableData.armedModeActive)
        {
            _weaponModelHiddenByCrouch = false;
            return;
        }

        if (!_layeredArmedActive || _simpleMoveLoopOnly)
        {
            return;
        }

        if (!_player.ReusableData.AllowsArmedWeaponActions())
        {
            if (!_weaponModelHiddenByCrouch)
            {
                _weaponModelHiddenByCrouch = true;
                ApplyWeaponModelVisible(false);
            }

            return;
        }

        if (_weaponModelHiddenByCrouch)
        {
            _weaponModelHiddenByCrouch = false;
            if (IsExiting)
            {
                return;
            }

            ReapplyWeaponModelVisibilityAfterCrouchStand();
        }
    }

    private void ReapplyWeaponModelVisibilityAfterCrouchStand()
    {
        // 站起后若新一轮掏枪已在播：_weaponShownForCurrentDraw 尚未置 true，不应按「未掏过枪」强制隐藏，否则打断显隐时间轴。
        if (_data != null &&
            _data.draw != null &&
            _data.draw.IsValid &&
            _drawOrHolsterState != null &&
            _drawOrHolsterState.IsPlaying &&
            _drawOrHolsterState.Clip == _data.draw.Clip &&
            !_readyForUpperBodyGameplay)
        {
            return;
        }

        if (!_weaponShownForCurrentDraw || _weaponHiddenForCurrentHolster)
        {
            ApplyWeaponModelVisible(false);
            return;
        }

        ApplyWeaponModelVisible(true);
    }

    private void TickHolsterWeaponModelVisibility(AnimancerState cur)
    {
        if (_weaponHiddenForCurrentHolster ||
            _exitRoutine == null ||
            !_data.HasHolsterTransition ||
            _data.holsterWeaponModelHiddenAtFrame <= 0 ||
            _data.holster == null ||
            !_data.holster.IsValid)
        {
            return;
        }

        var holsterState = _drawOrHolsterState;
        if (holsterState == null || holsterState.Clip == null || holsterState.Clip != _data.holster.Clip)
        {
            return;
        }

        float threshold = FrameIndexToClipTimeSeconds(_data.holster.Clip, _data.holsterWeaponModelHiddenAtFrame);
        if (holsterState.Time >= threshold)
        {
            ApplyWeaponModelVisible(false);
            _weaponHiddenForCurrentHolster = true;
        }
    }

    private float ComputeLeftHandIkWeight(AnimancerState cur)
    {
        if (_data == null)
        {
            return 1f;
        }

        if (_exitRoutine != null &&
            _data.HasHolsterTransition &&
            _data.holster != null &&
            _data.holster.IsValid &&
            cur != null &&
            cur.Clip != null &&
            cur.Clip == _data.holster.Clip)
        {
            return 1f - GetUpperLayerClipProgress01(cur);
        }

        if (!_readyForUpperBodyGameplay &&
            _data.draw != null &&
            _data.draw.IsValid &&
            cur != null &&
            cur.Clip != null &&
            cur.Clip == _data.draw.Clip)
        {
            return GetUpperLayerClipProgress01(cur);
        }

        if (_exitRoutine != null)
        {
            return _rightIkWeight;
        }

        return 1f;
    }

    private static float GetUpperLayerClipProgress01(AnimancerState cur)
    {
        if (cur == null)
        {
            return 0f;
        }

        if (cur.Length > 1e-4f)
        {
            return Mathf.Clamp01((float)(cur.Time / cur.Length));
        }

        return Mathf.Clamp01(cur.NormalizedTime);
    }

    private void ResetHolsterArmedOffsetXMultiplier()
    {
        _holsterArmedOffsetXMultiplier = 1f;
        _holsterLocomotionLayerFadeT01 = 0f;
        _holsterExitLocomotionLayerFadeDriving = false;
    }

    /// <summary>收枪或离开持枪：协程内先播可选 holster；收枪片段结束后再收 IK、淡出 Layer1；Layer2 权重与 Layer0 idle 淡入同步淡出，最后回调与复位。</summary>
    public void BeginArmedExit(Action onComplete)
    {
        if (_exitRoutine != null)
        {
            StopCoroutine(_exitRoutine);
            _exitRoutine = null;
            _armedExitCoroutinePending = false;
        }

        float holsterEndToIdleLocomotionFade = ReadCombinedHolsterToIdleLocomotionFade(_player);
        _gunAudio?.OnHolster();

        if (_simpleMoveLoopOnly || !_layeredArmedActive || _data == null)
        {
            StopExitAndClearUpperBodyHardware();
            _layeredArmedActive = false;
            _simpleMoveLoopOnly = false;
            _readyForUpperBodyGameplay = false;
            _data = null;
            QueueHolsterExitToIdleLocomotionFade(holsterEndToIdleLocomotionFade);
            onComplete?.Invoke();
            return;
        }

        ResetAdsPresentationState();
        _armedExitCoroutinePending = true;
        _exitRoutine = StartCoroutine(CoArmedExit(onComplete, holsterEndToIdleLocomotionFade));
    }

    /// <summary>收枪结束后切入 idle 的 Layer0 淡入：取「收枪收尾」与「回空手」两配置中的较大值。</summary>
    private static float ReadCombinedHolsterToIdleLocomotionFade(Player player)
    {
        var cfg = player != null ? player.playerSO?.playerMovementData?.PlayerArmedAnimationData : null;
        if (cfg == null)
        {
            return -1f;
        }

        return Mathf.Max(cfg.holsterExitToIdleLocomotionFadeSeconds, cfg.holsterToBareHandsLocomotionFadeSeconds);
    }

    private void QueueHolsterExitToIdleLocomotionFade(float fadeSeconds)
    {
        if (_player?.ReusableData == null || fadeSeconds <= 0f)
        {
            return;
        }

        _player.ReusableData.pendingHolsterExitToIdleLocomotionFadeSeconds = fadeSeconds;
    }

    private IEnumerator CoArmedExit(Action onComplete, float holsterEndToIdleLocomotionFade)
    {
        ResetHolsterArmedOffsetXMultiplier();
        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();

        if (_animancer == null)
        {
            _armedExitCoroutinePending = false;
            _exitRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        _animancer.Layers.SetMinCount(3);
        var baseL = UpperBodyBaseLayer;
        var overlayL = UpperBodyOverlayLayer;

        float overlayWeightForLocomotionSyncFade = 0f;
        bool layer2NeedsLocomotionSyncFade = false;

        _weaponHiddenForCurrentHolster = false;
        if (_data.HasHolsterTransition)
        {
            if (_data.holsterWeaponModelHiddenAtFrame <= 0)
            {
                ApplyWeaponModelVisible(false);
                _weaponHiddenForCurrentHolster = true;
            }

            float holsterOverlayStartW = Mathf.Max(overlayL.Weight, 0.0001f);
            if (Mathf.Approximately(overlayL.Weight, 0f))
            {
                holsterOverlayStartW = _data.upperBodyLayerWeight;
            }

            overlayWeightForLocomotionSyncFade = holsterOverlayStartW;
            layer2NeedsLocomotionSyncFade = true;

            // 先启动收枪片段，再同步 Layer2 权重/平滑目标，避免在换片前改层级权重与额外 Play（hold）过渡抢在收枪动画之前。
            _drawOrHolsterState = overlayL.Play(_data.holster, _data.holsterFadeInSeconds);
            _overlayUpperWeightSmoothed = holsterOverlayStartW;
            _overlayUpperWeightGoal = holsterOverlayStartW;
            _overlayUpperWeightVel = 0f;
            RestorePresentationOverlayLayerWeight(overlayL);
            overlayL.Weight = holsterOverlayStartW;
            NotifyLayer2OverlayRigStabilizer();
            if (_drawOrHolsterState != null)
            {
                bool ended = false;
                void OnHolsterEnd()
                {
                    ended = true;
                }

                _drawOrHolsterState.Events(this).OnEnd = OnHolsterEnd;
                while (!ended && _drawOrHolsterState != null && _drawOrHolsterState.IsPlaying)
                {
                    _holsterArmedOffsetXMultiplier = 1f - GetUpperLayerClipProgress01(_drawOrHolsterState);
                    yield return null;
                }

                CaptureLayer2HoldPoseFromState(_drawOrHolsterState);
                var holsterStateToFreeze = _drawOrHolsterState;
                ClearDrawOrHolsterEndEvent();
                // 不再对同一 clip 二次 Play：否则会多一个状态节点（Inspector 里像「播了两次」），且与 ClipTransition 首播并存。
                TryFreezeHolsterOverlayStateInPlace(overlayL, holsterStateToFreeze);
                _holsterArmedOffsetXMultiplier = 0f;
            }
            else
            {
                _holsterArmedOffsetXMultiplier = 0f;
            }

            if (_data.holsterWeaponModelHiddenAtFrame > 0 && !_weaponHiddenForCurrentHolster)
            {
                ApplyWeaponModelVisible(false);
                _weaponHiddenForCurrentHolster = true;
            }
        }
        else
        {
            ApplyWeaponModelVisible(false);
            _weaponHiddenForCurrentHolster = true;
        }

        // 收枪片段结束后再收 IK，避免与「先播退出动画、再动层级」的顺序冲突。
        _rightIkWeightTarget = 0f;

        if (!layer2NeedsLocomotionSyncFade && overlayL != null)
        {
            overlayL.Weight = 0f;
            ResetOverlayUpperWeightSmoothing(0f);
        }

        float layerOut = Mathf.Max(0.0001f, _data.layerFadeOutSeconds);
        float startW = baseL.Weight;
        float t = 0f;
        bool noHolsterLayerFade = !_data.HasHolsterTransition;
        if (noHolsterLayerFade)
        {
            _holsterExitLocomotionLayerFadeDriving = true;
            _holsterLocomotionLayerFadeT01 = 0f;
        }

        while (t < 1f)
        {
            t += Time.deltaTime / layerOut;
            float tc = Mathf.Clamp01(t);
            baseL.Weight = Mathf.Lerp(startW, 0f, tc);
            if (noHolsterLayerFade)
            {
                _holsterLocomotionLayerFadeT01 = tc;
            }

            yield return null;
        }

        if (noHolsterLayerFade)
        {
            _holsterExitLocomotionLayerFadeDriving = false;
        }

        baseL.Weight = 0f;
        while (_rightIkWeight > 0.001f)
        {
            yield return null;
        }

        _handIk?.SetHandIkWeight(0f);
        _handIk?.SetRigBuilderEnabled(false);

        float locomotionFade = Mathf.Max(0f, holsterEndToIdleLocomotionFade);
        QueueHolsterExitToIdleLocomotionFade(holsterEndToIdleLocomotionFade);
        onComplete?.Invoke();

        if (layer2NeedsLocomotionSyncFade &&
            overlayL != null &&
            overlayWeightForLocomotionSyncFade > 0.0001f &&
            locomotionFade > 0.0001f)
        {
            float wStart = overlayWeightForLocomotionSyncFade;
            float u = 0f;
            while (u < 1f)
            {
                u += Time.deltaTime / locomotionFade;
                float k = Mathf.Clamp01(u);
                float w = Mathf.Lerp(wStart, 0f, k);
                overlayL.Weight = w;
                _overlayUpperWeightSmoothed = w;
                _overlayUpperWeightGoal = w;
                yield return null;
            }
        }
        else if (overlayL != null)
        {
            overlayL.Weight = 0f;
        }

        ResetOverlayUpperWeightSmoothing(0f);
        ForceResetVisuals();

        _armedExitCoroutinePending = false;
        _exitRoutine = null;
    }

    private void ClearDrawOrHolsterEndEvent()
    {
        if (_drawOrHolsterState == null)
        {
            return;
        }

        _drawOrHolsterState.Events(this).OnEnd = null;
        _drawOrHolsterState = null;
    }

    /// <summary>清 Layer 事件、Layer1 权重与内部标记；不停止正在运行的收枪协程（由 <see cref="BeginArmedExit"/> 管理）。</summary>
    public void ForceResetVisuals()
    {
        InvalidateLayer2HoldPose();
        ClearDrawOrHolsterEndEvent();
        ResetHolsterArmedOffsetXMultiplier();
        ResetAdsPresentationState();
        _layeredArmedActive = false;
        _simpleMoveLoopOnly = false;
        _readyForUpperBodyGameplay = false;
        _rightIkWeight = 0f;
        _rightIkWeightTarget = 0f;
        _data = null;
        ApplyWeaponModelVisible(false);
        _weaponShownForCurrentDraw = false;
        _weaponHiddenForCurrentHolster = false;
        _weaponModelHiddenByCrouch = false;

        if (_animancer != null && _animancer.Layers.Count > 1)
        {
            _animancer.Layers[1].Weight = 0f;
        }

        if (_animancer != null && _animancer.Layers.Count > 2)
        {
            ResetOverlayUpperWeightSmoothing(0f);
        }

        _handIk?.SetHandIkWeight(0f);
        _handIk?.SetRigBuilderEnabled(false);
        RestoreLayer0DefaultMask();
        ClearResumeArmedPresentationFlag();
    }

    private void ClearResumeArmedPresentationFlag()
    {
        if (_player != null && _player.ReusableData != null)
        {
            _player.ReusableData.resumeArmedPresentationWithoutDraw = false;
        }
    }

    /// <summary>状态机强制离开持枪且未走 <see cref="BeginArmedExit"/> 时调用：停止收枪协程并复位视觉。</summary>
    public void NotifyArmedStateForceQuit()
    {
        StopAllCoroutines();
        _exitRoutine = null;
        _armedExitCoroutinePending = false;
        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();
        _readyForUpperBodyGameplay = false;
        _handIk?.SetRigBuilderEnabled(false);
        _handIk?.SetHandIkWeight(0f);
        ForceResetVisuals();
        if (_player != null && _player.ReusableData != null)
        {
            _player.ReusableData.suppressCameraArmedLocalOffset = true;
            _player.ReusableData.pendingCameraPitchArmedOffsetHardStrip = true;
        }
    }
}
