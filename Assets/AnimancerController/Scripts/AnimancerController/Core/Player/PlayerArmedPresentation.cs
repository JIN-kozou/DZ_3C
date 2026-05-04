using System;
using System.Collections;
using Animancer;
using UnityEngine;

/// <summary>
/// 持枪 Animancer 双层：Layer0 不强制改片，与进入持枪前相同（idle / moveStart / moveLoop 等由上一状态已播内容延续）；
/// 可选 <see cref="PlayerArmedAnimationData.locomotionBodyMask"/> 仅约束 Layer0 骨骼范围；Layer1 用 <see cref="PlayerArmedAnimationData.upperBodyMask"/> 播掏枪 / 腰射 idle / 收枪 / ADS。
/// 离开持枪分层时会恢复 Layer0 为默认全身 Mask，避免影响其它状态的移动动画。
/// </summary>
[DisallowMultipleComponent]
public class PlayerArmedPresentation : MonoBehaviour
{
    [SerializeField, Tooltip("可选。实现 IArmedWeaponModelVisibility 的组件；为空时在角色子层级中查找第一个实现者。")]
    private MonoBehaviour weaponModelVisibilityOptional;

    private Player _player;
    private AnimancerComponent _animancer;
    private PlayerArmedHandIkRig _handIk;
    private IArmedWeaponModelVisibility _weaponModelVisibility;
    private bool _weaponShownForCurrentDraw;
    private bool _weaponHiddenForCurrentHolster;
    private bool _weaponModelHiddenByCrouch;
    private AnimancerState _adsLoopingFireState;

    private PlayerArmedAnimationData _data;
    private bool _layeredArmedActive;
    private bool _simpleMoveLoopOnly;
    private Coroutine _exitRoutine;
    private AnimancerState _drawOrHolsterState;

    /// <summary>右手 IK 平滑权重（掏枪结束后由 0 拉到 1；收枪协程里拉回 0）。左手见 <see cref="ComputeLeftHandIkWeight"/>。</summary>
    private float _rightIkWeight;
    private float _rightIkWeightTarget;

    private bool _readyForUpperBodyGameplay;
    private bool _adsInPose;
    private bool _adsEntering;
    private bool _adsExiting;
    private AnimancerState _adsEventState;

    public bool IsExiting => _exitRoutine != null;

    /// <summary>
    /// 分层持枪时掏枪已播完且上半身进入 idle（可腰射开火）；非分层或未启用分层时视为始终就绪。
    /// </summary>
    public bool IsUpperBodyReadyForWeapon =>
        !_layeredArmedActive || _simpleMoveLoopOnly || _readyForUpperBodyGameplay;

    public void Init(Player player)
    {
        _player = player;
        _animancer = player != null ? player.animancer : null;
        _handIk = GetComponent<PlayerArmedHandIkRig>();
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
        }

        ClearDrawOrHolsterEndEvent();
        if (!preserveLayeredUpperBody)
        {
            ResetAdsPresentationState();
            _rightIkWeight = 0f;
            _rightIkWeightTarget = 0f;

            if (_animancer != null && _animancer.Layers.Count > 1)
            {
                _animancer.Layers[1].Weight = 0f;
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

        bool layeredConfigured = data != null && data.UseLayeredPresentation;
        if (!layeredConfigured && moveLoop == null)
        {
            return;
        }

        bool preserveUpper = resumeLayeredPresentationWithoutDraw && layeredConfigured;
        StopExitAndClearUpperBodyHardware(preserveUpper);

        if (preserveUpper)
        {
            _data = data;
            _simpleMoveLoopOnly = false;
            _layeredArmedActive = true;
            _readyForUpperBodyGameplay = true;

            _animancer.Layers.SetMinCount(2);
            if (data.locomotionBodyMask != null)
            {
                ApplyLocomotionMaskForLayer0(_animancer.Layers, data.locomotionBodyMask);
            }
            else
            {
                RestoreLayer0DefaultMask();
            }

            _animancer.Layers.SetMask(1, data.upperBodyMask);
            var upper = _animancer.Layers[1];
            if (upper.Weight < 0.001f)
            {
                upper.Weight = data.upperBodyLayerWeight;
            }

            _handIk?.SetRigBuilderEnabled(true);
            _rightIkWeightTarget = 1f;
            ApplyWeaponModelVisible(true);
            _weaponShownForCurrentDraw = true;
            _weaponHiddenForCurrentHolster = false;
            return;
        }

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

        _animancer.Layers.SetMinCount(2);
        if (data.locomotionBodyMask != null)
        {
            ApplyLocomotionMaskForLayer0(_animancer.Layers, data.locomotionBodyMask);
        }
        else
        {
            RestoreLayer0DefaultMask();
        }

        _animancer.Layers.SetMask(1, data.upperBodyMask);
        var upperLayer = _animancer.Layers[1];
        upperLayer.Weight = 0f;

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
        _drawOrHolsterState = drawIn > 0f
            ? upperLayer.Play(data.draw, drawIn)
            : upperLayer.Play(data.draw);
        if (drawIn <= 0f && Mathf.Abs(upperLayer.Weight - data.upperBodyLayerWeight) > 0.001f)
        {
            upperLayer.Weight = data.upperBodyLayerWeight;
        }
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

        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();
        var upper = _animancer.Layers[1];
        upper.Weight = _data.upperBodyLayerWeight;
        upper.Play(_data.armedIdle, _data.drawToIdleFadeSeconds);
        _rightIkWeightTarget = 1f;
        _readyForUpperBodyGameplay = true;
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

        var upper = _animancer.Layers[1];

        if (!adsHeld)
        {
            if (_adsInPose || _adsEntering)
            {
                TryBeginAdsExit(upper);
            }

            return;
        }

        if (_adsExiting)
        {
            return;
        }

        if (!_adsInPose && !_adsEntering)
        {
            BeginAdsEnter(upper, adsEnterSpeedScale);
            return;
        }

        if (_adsEntering)
        {
            return;
        }

        if (fireHeld)
        {
            bool fireLooping = _data.adsFire.Clip != null && _data.adsFire.Clip.isLooping;
            if (fireLooping)
            {
                if (_adsLoopingFireState != null &&
                    _adsLoopingFireState.IsPlaying &&
                    _adsLoopingFireState.Clip == _data.adsFire.Clip)
                {
                    return;
                }

                ClearAdsEventState();
                float fireIn = Mathf.Max(
                    _data.adsFireFadeSeconds,
                    _data.adsIdleCrossFadeSeconds * 0.5f);
                _adsLoopingFireState = upper.Play(_data.adsFire, fireIn);
            }
            else if (firePressedThisFrame)
            {
                _adsLoopingFireState = null;
                ClearAdsEventState();
                float fireIn = Mathf.Max(
                    _data.adsFireFadeSeconds,
                    _data.adsIdleCrossFadeSeconds * 0.5f);
                var fireState = upper.Play(_data.adsFire, fireIn);
                _adsEventState = fireState;
                fireState.Events(this).OnEnd = OnAdsFireClipFinished;
            }
        }
        else if (upper.CurrentState != null && upper.CurrentState.Clip == _data.adsFire.Clip)
        {
            _adsLoopingFireState = null;
            ClearAdsEventState();
            float backFade = Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
            PlayAdsIdleOrFallback(upper, backFade);
        }
        else if (_adsInPose)
        {
            EnsureAdsIdlePlaying(upper);
        }
    }

    private void BeginAdsEnter(AnimancerLayer upper, float adsEnterSpeedScale)
    {
        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsEntering = true;
        _adsInPose = false;
        var st = upper.Play(_data.adsEnter, _data.adsEnterFadeSeconds);
        float scale = Mathf.Max(0.05f, adsEnterSpeedScale);
        st.Speed = _data.adsEnter.Speed * scale;
        _adsEventState = st;
        st.Events(this).OnEnd = OnAdsEnterFinished;
    }

    private void OnAdsEnterFinished()
    {
        if (!_adsEntering || _data == null || !_layeredArmedActive)
        {
            return;
        }

        ClearAdsEventState();
        _adsLoopingFireState = null;
        _adsEntering = false;
        _adsInPose = true;
        PlayAdsIdleOrFallback(_animancer.Layers[1], _data.adsEnterFadeSeconds);
    }

    private void OnAdsFireClipFinished()
    {
        if (!_adsInPose || _data == null || !_layeredArmedActive)
        {
            return;
        }

        _adsLoopingFireState = null;
        ClearAdsEventState();
        float backFade = Mathf.Max(_data.adsFireFadeSeconds, _data.adsIdleCrossFadeSeconds);
        PlayAdsIdleOrFallback(_animancer.Layers[1], backFade);
    }

    private void TryBeginAdsExit(AnimancerLayer upper)
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
        var st = upper.Play(_data.adsExit, _data.adsExitFadeSeconds);
        _adsEventState = st;
        st.Events(this).OnEnd = OnAdsExitFinished;
    }

    private void OnAdsExitFinished()
    {
        if (_data == null || !_layeredArmedActive)
        {
            return;
        }

        ClearAdsEventState();
        _adsExiting = false;
        _animancer.Layers[1].Play(_data.armedIdle, _data.adsExitFadeSeconds);
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
        if (upper.CurrentState == null || upper.CurrentState.Clip != idle.Clip)
        {
            ClearAdsEventState();
            upper.Play(idle, _data.adsIdleCrossFadeSeconds);
        }
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

    private void Update()
    {
        if (!_layeredArmedActive || _data == null || _simpleMoveLoopOnly)
        {
            return;
        }

        var upper = _animancer.Layers.Count > 1 ? _animancer.Layers[1] : null;
        var cur = upper != null ? upper.CurrentState : null;

        float dt = Time.deltaTime;
        float speedIn = _data.ikFadeInSeconds > 0f ? dt / _data.ikFadeInSeconds : 1f;
        float speedOut = _data.ikFadeOutSeconds > 0f ? dt / _data.ikFadeOutSeconds : 1f;
        float delta = _rightIkWeightTarget > _rightIkWeight ? speedIn : speedOut;
        _rightIkWeight = Mathf.MoveTowards(_rightIkWeight, _rightIkWeightTarget, delta);

        float leftW = ComputeLeftHandIkWeight(cur);
        _handIk?.SetLeftHandIkWeight(leftW);
        _handIk?.SetRightHandIkWeight(_rightIkWeight);
        TickWeaponModelVisibility(cur);
        TickCrouchWeaponModelVisibility();
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

        if (cur == null || cur.Clip == null || cur.Clip != _data.draw.Clip)
        {
            return;
        }

        float threshold = FrameIndexToClipTimeSeconds(_data.draw.Clip, _data.drawWeaponModelVisibleAtFrame);
        if (cur.Time >= threshold)
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

        if (cur == null || cur.Clip == null || cur.Clip != _data.holster.Clip)
        {
            return;
        }

        float threshold = FrameIndexToClipTimeSeconds(_data.holster.Clip, _data.holsterWeaponModelHiddenAtFrame);
        if (cur.Time >= threshold)
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

    /// <summary>收枪或离开持枪：先播可选 holster，再淡出 Layer1，等待 IK 归零后回调。</summary>
    public void BeginArmedExit(Action onComplete)
    {
        if (_exitRoutine != null)
        {
            StopCoroutine(_exitRoutine);
            _exitRoutine = null;
        }

        float holsterEndToIdleLocomotionFade = ReadCombinedHolsterToIdleLocomotionFade(_player);

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
        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();
        _rightIkWeightTarget = 0f;

        var upper = _animancer.Layers[1];
        _weaponHiddenForCurrentHolster = false;
        if (_data.HasHolsterTransition)
        {
            if (_data.holsterWeaponModelHiddenAtFrame <= 0)
            {
                ApplyWeaponModelVisible(false);
                _weaponHiddenForCurrentHolster = true;
            }

            float holsterUpperStartW = Mathf.Max(upper.Weight, 0.0001f);
            if (Mathf.Approximately(upper.Weight, 0f))
            {
                holsterUpperStartW = _data.upperBodyLayerWeight;
            }

            _drawOrHolsterState = upper.Play(_data.holster, _data.holsterFadeInSeconds);
            if (_drawOrHolsterState != null)
            {
                bool ended = false;
                void OnHolsterEnd()
                {
                    ended = true;
                }

                _drawOrHolsterState.Events(this).OnEnd = OnHolsterEnd;
                float holsterFadeOut = Mathf.Max(0f, _data.holsterFadeOutSeconds);
                while (!ended && _drawOrHolsterState != null && _drawOrHolsterState.IsPlaying)
                {
                    if (holsterFadeOut > 0.0001f && _drawOrHolsterState.Length > 0.0001f)
                    {
                        double timeRemaining = _drawOrHolsterState.Length - _drawOrHolsterState.Time;
                        if (timeRemaining <= holsterFadeOut)
                        {
                            float k = holsterFadeOut > 0.0001f
                                ? Mathf.Clamp01((float)(timeRemaining / holsterFadeOut))
                                : 0f;
                            upper.Weight = Mathf.Lerp(0f, holsterUpperStartW, k);
                        }
                        else
                        {
                            upper.Weight = holsterUpperStartW;
                        }
                    }

                    yield return null;
                }

                ClearDrawOrHolsterEndEvent();
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

        float layerOut = Mathf.Max(0.0001f, _data.layerFadeOutSeconds);
        float startW = upper.Weight;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / layerOut;
            upper.Weight = Mathf.Lerp(startW, 0f, Mathf.Clamp01(t));
            yield return null;
        }

        upper.Weight = 0f;
        while (_rightIkWeight > 0.001f)
        {
            yield return null;
        }

        _handIk?.SetHandIkWeight(0f);
        _handIk?.SetRigBuilderEnabled(false);
        ForceResetVisuals();

        _exitRoutine = null;
        QueueHolsterExitToIdleLocomotionFade(holsterEndToIdleLocomotionFade);
        onComplete?.Invoke();
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
        ClearDrawOrHolsterEndEvent();
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
        ClearDrawOrHolsterEndEvent();
        ResetAdsPresentationState();
        _readyForUpperBodyGameplay = false;
        _handIk?.SetRigBuilderEnabled(false);
        _handIk?.SetHandIkWeight(0f);
        ForceResetVisuals();
    }
}
