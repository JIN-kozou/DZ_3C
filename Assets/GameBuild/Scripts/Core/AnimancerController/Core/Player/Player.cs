
using Animancer;
using DZ_3C.Reverse;
using UnityEngine;
using UnityEngine.Animations.Rigging;
/*************************************************
����: HuHu
����: 3112891874@qq.com
����: ��ҿ��ƺ������
*************************************************/
[RequireComponent(typeof(AnimancerComponent))]
public class Player : CharacterBase
{
    public PlayerSO playerSO;
    //�ƶ�ҵ��
    public  AnimancerComponent animancer { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerReusableData ReusableData { get; private set; }
    public PlayerReusableLogic ReusableLogic { get; private set; }
    public Transform camTransform { get; private set; }

    //����ģ��
    public InputService InputService { get; private set; }
    public TimerService TimerService { get; private set; }
    public PlayerBuffSystem BuffSystem { get; private set; }
    public PlayerWeaponRuntime WeaponRuntime { get; private set; }
    public PlayerArmedPresentation ArmedPresentation { get; private set; }

    /// <summary>收枪协程未结束时不可再进入持枪分层（掏枪）；各状态切 <see cref="PlayerStateMachine.armedState"/> 前应检查。</summary>
    public bool CanBeginArmedPresentationNow() =>
        ArmedPresentation == null || ArmedPresentation.CanBeginArmedPresentation;

    /// <summary>
    /// 与地面 Idle / MoveStart / MoveLoop / Land 中 ToggleWeapon 输入时相同：校验站立可持枪与可掏枪后，置位复用数据并切到 <see cref="PlayerStateMachine.armedState"/>。
    /// 站起自动掏枪在 <see cref="Player.TryResumeArmedAfterCrouchStand"/> 中设置 <see cref="PlayerReusableData.resumeArmedPresentationWithoutDraw"/> 后调用本方法，与手动掏枪一致。
    /// </summary>
    /// <returns>是否已通过校验并发起 <see cref="PlayerStateMachine.ChangeState"/>。</returns>
    public bool TryEnterArmedStateSameAsToggleWeaponInput()
    {
        if (ReusableData == null || StateMachine == null)
        {
            return false;
        }

        if (!ReusableData.AllowsArmedWeaponActions())
        {
            return false;
        }

        if (!CanBeginArmedPresentationNow())
        {
            return false;
        }

        ReusableData.pendingAutoDrawWeaponAfterCrouchHolsterStand = false;
        ReusableData.armedModeActive = true;
        ReusableData.resumeArmedAfterBreak = false;
        ReusableData.weaponSuppressedUntilStandFromCrouch = false;
        ReusableData.pendingCrouchAfterStandHolster = false;
        StateMachine.ChangeState(StateMachine.armedState);
        return true;
    }

    /// <summary>
    /// 持枪模式下由下蹲触发：先 <see cref="PlayerArmedPresentation.BeginArmedExit"/> 收枪，回调内再空手并下蹲。
    /// 返回 true 表示已消费本次下蹲输入（已开收枪协程，或持枪但当前不可收枪故不下蹲）；false 表示非持枪模式，由调用方照常设蹲姿。
    /// </summary>
    public bool TryBeginHolsterThenCrouchFromArmedLocomotion()
    {
        if (ReusableData == null || StateMachine == null || InputService == null)
        {
            return false;
        }

        if (!ReusableData.armedModeActive)
        {
            return false;
        }

        if (ArmedPresentation == null)
        {
            return true;
        }

        if (!ArmedPresentation.IsHolsterInputAllowed || !CanBeginArmedPresentationNow())
        {
            return true;
        }

        ArmedPresentation.BeginArmedExit(() => ApplyArmedHolsterExitToUnarmedLocomotion(enterCrouchAndQueueAutoDrawOnStand: true));
        return true;
    }

    /// <param name="enterCrouchAndQueueAutoDrawOnStand">true：收枪后下蹲并排队站起自动掏枪；false：收枪键同款，站立空手 Idle。</param>
    private void ApplyArmedHolsterExitToUnarmedLocomotion(bool enterCrouchAndQueueAutoDrawOnStand)
    {
        if (ReusableData == null || StateMachine == null || InputService == null)
        {
            return;
        }

        if (enterCrouchAndQueueAutoDrawOnStand)
        {
            ReusableData.pendingAutoDrawWeaponAfterCrouchHolsterStand = true;
            ReusableData.pendingStandWhenCrouchCeilingClears = false;
            ReusableData.standValueParameter.TargetValue = 0f;

            if (isOnGround.Value && InputService.MoveDiscrete != UnityEngine.Vector2.zero)
            {
                if (InputService.Shift)
                {
                    StateMachine.ChangeState(StateMachine.moveLoopState);
                }
                else
                {
                    StateMachine.ChangeState(StateMachine.moveStartState);
                }
            }
            else
            {
                StateMachine.ChangeState(StateMachine.idleState);
            }

            ReusableData.armedModeActive = false;
            ReusableData.resumeArmedAfterBreak = false;
            ReusableData.weaponSuppressedUntilStandFromCrouch = false;
            ReusableData.pendingCrouchAfterStandHolster = false;
        }
        else
        {
            ReusableData.pendingAutoDrawWeaponAfterCrouchHolsterStand = false;
            // 先切 Idle（armed 仍为 true）再关 armed，避免 Idle.OnEnter 里 NotifyArmedStateForceQuit 打断收枪协程末尾的 Layer2 淡出。
            StateMachine.ChangeState(StateMachine.idleState);
            ReusableData.armedModeActive = false;
            ReusableData.resumeArmedAfterBreak = false;
            ReusableData.weaponSuppressedUntilStandFromCrouch = false;
            ReusableData.pendingCrouchAfterStandHolster = false;
        }
    }

    [Header("Player Resources")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField] private float initHealth = 100f;
    [SerializeField] private float initStamina = 100f;
    private float baseMoveSpeedMultiplier = 1f;

    [Header("Reverse System (optional)")]
    [SerializeField] private MonoBehaviour reverseRecoverTargetBehaviour;
    private IReverseRecoverTarget reverseRecoverTarget;

    private bool _wasCrouchedForRigBuilders;
    private RigBuilder[] _rigBuildersSnapshot;
    private bool[] _rigBuildersEnabledSnapshot;

    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;

    protected override void Awake()
    {
        base.Awake();
        InputService = InputService.Instance;
        TimerService = TimerService.Instance;
        ApplyNumericConfig(playerSO?.playerMovementData?.PlayerNumericConfig);
        if (playerSO?.playerMovementData?.PlayerNumericConfig != null)
        {
            baseMoveSpeedMultiplier = playerSO.playerMovementData.PlayerNumericConfig.moveSpeedMultiplier;
        }

        camTransform = Camera.main.transform;
        animancer = GetComponent<AnimancerComponent>();
        if (animancer == null)
        {
            Debug.LogError("δָ��Animancer������޷����Ŷ�������");
            return;
        }
        //��������
        ReusableData = new PlayerReusableData(animancer, playerSO);
        if (playerSO?.playerMovementData?.PlayerNumericConfig != null)
        {
            ReusableData.jumpExternalForce = playerSO.playerMovementData.PlayerNumericConfig.platformerJumpHeight;
        }
        ReusableData.health.Value = Mathf.Clamp(initHealth, 0f, maxHealth);
        ReusableData.stamina.Value = Mathf.Clamp(initStamina, 0f, maxStamina);
        //�����߼�
        ReusableLogic = new PlayerReusableLogic(this);
        BuffSystem = new PlayerBuffSystem(this);
        //����״̬��
        WeaponRuntime = GetComponent<PlayerWeaponRuntime>();
        if (WeaponRuntime == null)
        {
            WeaponRuntime = gameObject.AddComponent<PlayerWeaponRuntime>();
        }

        if (GetComponent<PlayerArmedHandIkRig>() == null)
        {
            gameObject.AddComponent<PlayerArmedHandIkRig>();
        }

        ArmedPresentation = GetComponent<PlayerArmedPresentation>();
        if (ArmedPresentation == null)
        {
            ArmedPresentation = gameObject.AddComponent<PlayerArmedPresentation>();
        }

        ArmedPresentation.Init(this);

        StateMachine = new PlayerStateMachine(this);
        if (GetComponent<PlayerMotionDebugView>() == null)
        {
            gameObject.AddComponent<PlayerMotionDebugView>();
        }
        //����Ĭ�Ͽ�ʼ״̬
        StateMachine.ChangeState(StateMachine.idleState);
        UpdateCharacterControllerStance();
    }
    protected override void Update()
    {
        TryRestoreRigBuildersAfterCrouchExitBeforeStateMachine();
        base.Update();
        BuffSystem?.Tick(Time.deltaTime);
        UpdateBuffDrivenValues();
        TickKeyboardMoveSmoothing();
        StateMachine?.OnUpdate();
        TryHolsterWeaponInputIfArmed();
        // 先结算武器 Tick（SuccessfulShotsLastTick），再播 ADS 开火动画，避免未射出子弹仍播开火。
        WeaponRuntime?.Tick(Time.deltaTime);
        TickArmedUpperBodyAdsIfNeeded();
        TryApplyPendingCrouchAfterStandHolster();
        TryResumeArmedAfterCrouchStand();
    }

    /// <summary>
    /// 在状态机前刷新 <see cref="InputService.Move"/>（键鼠 SmoothDamp 摇杆模拟）；门控与攀爬等仍用 <see cref="InputService.MoveDiscrete"/>。
    /// </summary>
    private void TickKeyboardMoveSmoothing()
    {
        if (InputService == null)
        {
            return;
        }

        PlayerNumericConfig numericConfig = playerSO?.playerMovementData?.PlayerNumericConfig;
        if (numericConfig != null)
        {
            InputService.TickMoveSmoothing(
                Time.deltaTime,
                numericConfig.keyboardMoveSmoothTime,
                numericConfig.keyboardMoveInputSmoothing);
        }
        else
        {
            InputService.TickMoveSmoothing(Time.deltaTime, 0.12f, true);
        }
    }

    /// <summary>
    /// 收枪键（如键盘 3）：掏枪动画未结束前、收枪协程进行中均忽略。
    /// </summary>
    private void TryHolsterWeaponInputIfArmed()
    {
        if (ReusableData == null ||
            InputService == null ||
            StateMachine == null ||
            ArmedPresentation == null)
        {
            return;
        }

        if (!ReusableData.armedModeActive || !ReusableData.AllowsArmedWeaponActions())
        {
            return;
        }

        if (!InputService.HolsterWeaponWasPressedThisFrame)
        {
            return;
        }

        var cur = StateMachine.currentState;
        if (cur == null)
        {
            return;
        }

        if (cur.GetType().Name.Contains("Climb"))
        {
            return;
        }

        if (!ArmedPresentation.IsHolsterInputAllowed)
        {
            return;
        }

        ArmedPresentation.BeginArmedExit(() => ApplyArmedHolsterExitToUnarmedLocomotion(enterCrouchAndQueueAutoDrawOnStand: false));
    }

    /// <summary>
    /// 持枪模式下 ADS 输入在跳跃/落地等非 <see cref="PlayerArmedState"/> 中仍生效；攀爬时关闭。
    /// </summary>
    private void TickArmedUpperBodyAdsIfNeeded()
    {
        if (ReusableData == null ||
            ArmedPresentation == null ||
            !ReusableData.armedModeActive ||
            !ReusableData.AllowsArmedWeaponActions() ||
            InputService == null ||
            playerSO?.playerMovementData == null)
        {
            return;
        }

        var cur = StateMachine?.currentState;
        if (cur == null)
        {
            return;
        }

        string stateName = cur.GetType().Name;
        if (stateName.Contains("Climb"))
        {
            return;
        }

        var armedAnim = playerSO.playerMovementData.PlayerArmedAnimationData;
        if (armedAnim == null || !armedAnim.HasAdsAnimationPack)
        {
            return;
        }

        float adsSpeedScale = playerSO.playerMovementData.PlayerNumericConfig != null
            ? playerSO.playerMovementData.PlayerNumericConfig.adsEnterAnimationSpeedScale
            : 1f;

        bool adsHeld = InputService.ADSHeld;
        if (!ArmedPresentation.IsAdsInputAllowed)
        {
            adsHeld = false;
        }

        ArmedPresentation.TickUpperBodyAds(
            adsHeld,
            InputService.FireHeld,
            InputService.FireWasPressedThisFrame,
            adsSpeedScale);
    }

    private void TryApplyPendingCrouchAfterStandHolster()
    {
        if (ReusableData == null || InputService == null || !ReusableData.pendingCrouchAfterStandHolster)
        {
            return;
        }

        if (ReusableData.armedModeActive)
        {
            ReusableData.pendingCrouchAfterStandHolster = false;
            return;
        }

        if (!ReusableData.AllowsArmedWeaponActions())
        {
            return;
        }

        var num = playerSO?.playerMovementData?.PlayerNumericConfig;
        if (num != null && num.useHoldForCrouch && !InputService.CrouchHeld)
        {
            ReusableData.pendingCrouchAfterStandHolster = false;
            return;
        }

        ReusableData.pendingStandWhenCrouchCeilingClears = false;
        ReusableData.standValueParameter.TargetValue = 0;
        ReusableData.pendingCrouchAfterStandHolster = false;
    }

    private void TryResumeArmedAfterCrouchStand()
    {
        if (ReusableData == null || StateMachine == null)
        {
            return;
        }

        if (!ReusableData.pendingAutoDrawWeaponAfterCrouchHolsterStand)
        {
            return;
        }

        // Current 在设 Target=蹲 后仍会短暂保持「站立」；必须同时看 Target，否则会刚进蹲就误判站起并自动掏枪。
        if (ReusableData.standValueParameter.CurrentValue < 0.99f ||
            ReusableData.standValueParameter.TargetValue < 0.99f)
        {
            return;
        }

        if (ReusableData.armedModeActive)
        {
            ReusableData.pendingAutoDrawWeaponAfterCrouchHolsterStand = false;
            return;
        }

        if (!ReusableData.AllowsArmedWeaponActions() || !CanBeginArmedPresentationNow())
        {
            return;
        }

        ReusableData.resumeArmedPresentationWithoutDraw = false;
        TryEnterArmedStateSameAsToggleWeaponInput();
    }

    private void LateUpdate()
    {
        UpdateCharacterControllerStance();
        ApplyRigBuildersDisabledWhileCrouched();
    }

    /// <summary>
    /// 下蹲（stand 混合值 &lt; 0.99）时关闭角色身上所有 <see cref="RigBuilder"/>；站起恢复在进入下蹲当帧记录的 enabled 状态。
    /// 恢复在 <see cref="Update"/> 最前执行，避免与当帧 <see cref="PlayerArmedPresentation.BeginArmedEnter"/> 等写 Rig 的逻辑打架。
    /// </summary>
    private void TryRestoreRigBuildersAfterCrouchExitBeforeStateMachine()
    {
        if (ReusableData == null)
        {
            return;
        }

        bool crouched = ReusableData.standValueParameter.CurrentValue < 0.99f;
        if (!crouched && _wasCrouchedForRigBuilders)
        {
            if (_rigBuildersSnapshot != null && _rigBuildersEnabledSnapshot != null)
            {
                int n = Mathf.Min(_rigBuildersSnapshot.Length, _rigBuildersEnabledSnapshot.Length);
                for (int i = 0; i < n; i++)
                {
                    RigBuilder rb = _rigBuildersSnapshot[i];
                    if (rb != null)
                    {
                        rb.enabled = _rigBuildersEnabledSnapshot[i];
                    }
                }
            }

            _rigBuildersSnapshot = null;
            _rigBuildersEnabledSnapshot = null;
        }

        _wasCrouchedForRigBuilders = crouched;
    }

    private void ApplyRigBuildersDisabledWhileCrouched()
    {
        if (ReusableData == null)
        {
            return;
        }

        if (ReusableData.standValueParameter.CurrentValue >= 0.99f)
        {
            return;
        }

        if (_rigBuildersSnapshot == null)
        {
            _rigBuildersSnapshot = GetComponentsInChildren<RigBuilder>(true);
            _rigBuildersEnabledSnapshot = new bool[_rigBuildersSnapshot.Length];
            for (int i = 0; i < _rigBuildersSnapshot.Length; i++)
            {
                RigBuilder rb = _rigBuildersSnapshot[i];
                _rigBuildersEnabledSnapshot[i] = rb != null && rb.enabled;
            }
        }

        for (int i = 0; i < _rigBuildersSnapshot.Length; i++)
        {
            RigBuilder rb = _rigBuildersSnapshot[i];
            if (rb != null)
            {
                rb.enabled = false;
            }
        }
    }

    /// <summary>
    /// 按站立/下蹲混合值（standValueParameter：1=站立，0=下蹲）插值更新 CharacterController 尺寸；
    /// <see cref="CharacterBase.isOnGround"/> 为 false 时（起跳后整段空中至再次接地前）使用 <see cref="PlayerNumericConfig"/> 中的空中专用尺寸。
    /// </summary>
    private void UpdateCharacterControllerStance()
    {
        if (controller == null || ReusableData == null)
        {
            return;
        }
        if (!controller.enabled)
        {
            return;
        }

        var cfg = playerSO?.playerMovementData?.PlayerNumericConfig;
        if (cfg == null)
        {
            return;
        }

        float radius;
        float height;
        Vector3 center;
        if (!isOnGround.Value)
        {
            radius = cfg.fallControllerRadius;
            height = cfg.fallControllerHeight;
            center = cfg.fallControllerCenter;
        }
        else
        {
            float standBlend = Mathf.Clamp01(ReusableData.standValueParameter.CurrentValue);
            radius = Mathf.Lerp(cfg.crouchControllerRadius, cfg.standControllerRadius, standBlend);
            height = Mathf.Lerp(cfg.crouchControllerHeight, cfg.standControllerHeight, standBlend);
            center = Vector3.Lerp(cfg.crouchControllerCenter, cfg.standControllerCenter, standBlend);
        }

        height = Mathf.Max(height, radius * 2f + 0.001f);
        float maxRadius = height * 0.5f - 0.0005f;
        radius = Mathf.Clamp(radius, 0.01f, maxRadius);

        controller.radius = radius;
        controller.height = height;
        controller.center = center;
    }

    private const float LedgeDebugRayDuration = 0.08f;

    protected override void PostGroundCheck()
    {
        PlayerNumericConfig cfg = playerSO?.playerMovementData?.PlayerNumericConfig;
        if (cfg == null || !cfg.ledgeWalkOffEnabled)
        {
            return;
        }

        if (disEnableGravity || controller == null || !controller.enabled || !isOnGround.Value)
        {
            return;
        }

        if (!IsLedgeWalkOffStateAllowed())
        {
            return;
        }

        Vector2 moveInput = InputService != null ? InputService.MoveDiscrete : Vector2.zero;
        if (cfg.ledgeRequireMoveInput)
        {
            if (moveInput.sqrMagnitude < 1e-6f)
            {
                return;
            }
        }
        else if (InputService != null)
        {
            moveInput = InputService.Move;
            if (moveInput.sqrMagnitude < 1e-6f)
            {
                return;
            }
        }
        else
        {
            return;
        }

        Vector3 planarDir = GetPlanarMoveDirection(moveInput);
        if (planarDir.sqrMagnitude < 1e-6f)
        {
            return;
        }

        float footY = GetCapsuleFootWorldY();
        Vector3 planarOrigin = new Vector3(transform.position.x, footY, transform.position.z);
        Vector3 probeOrigin = planarOrigin + planarDir.normalized * cfg.ledgeProbeForwardDistance + Vector3.up * 0.05f;

        bool shouldWalkOff;
        if (TryProbeGroundBelow(probeOrigin, cfg.ledgeProbeDownDistance, out RaycastHit hit))
        {
            float drop = footY - hit.point.y;
            if (drop <= cfg.ledgeSameHeightTolerance)
            {
                DrawLedgeDebugRay(probeOrigin, hit.point, false);
                return;
            }

            shouldWalkOff = true;
            DrawLedgeDebugRay(probeOrigin, hit.point, true);
        }
        else
        {
            shouldWalkOff = true;
            DrawLedgeDebugRay(probeOrigin, probeOrigin + Vector3.down * cfg.ledgeProbeDownDistance, true);
        }

        if (shouldWalkOff)
        {
            ForceWalkOffLedge();
        }
    }

    private bool IsLedgeWalkOffStateAllowed()
    {
        if (StateMachine == null)
        {
            return false;
        }

        IState state = StateMachine.currentState;
        return state is PlayerIdleState
            || state is PlayerMoveStartState
            || state is PlayerMoveLoopState
            || state is PlayerLandState
            || state is PlayerArmedState;
    }

    private Vector3 GetPlanarMoveDirection(Vector2 move)
    {
        if (camTransform == null)
        {
            return new Vector3(move.x, 0f, move.y);
        }

        return Quaternion.Euler(0f, camTransform.eulerAngles.y, 0f) * new Vector3(move.x, 0f, move.y);
    }

    private void DrawLedgeDebugRay(Vector3 origin, Vector3 end, bool walkOff)
    {
#if UNITY_EDITOR
        Color color = walkOff ? Color.red : Color.green;
        Debug.DrawLine(origin, end, color, LedgeDebugRayDuration);
#endif
    }

    protected override void OnAnimatorMove()
    {
        base.OnAnimatorMove();
        StateMachine?.OnAnimationUpdate();
    }
    public void AnimationEnd()
    {
        StateMachine?.OnAnimationEnd();
    }

    private void UpdateBuffDrivenValues()
    {
        if (BuffSystem == null || ReusableData == null)
        {
            return;
        }

        ReusableData.buffSnapshot = BuffSystem.RuntimeSnapshot;
        moveSpeedMult = baseMoveSpeedMultiplier * Mathf.Max(0.01f, ReusableData.buffSnapshot.moveSpeedMultiplier);
    }

    public void ApplyBuff(PlayerBuffConfigSO buffConfig, PlayerBuffSourceContext sourceContext)
    {
        BuffSystem?.ApplyBuff(buffConfig, sourceContext);
    }

    public void ApplyBuffByExternal(PlayerBuffConfigSO buffConfig, GameObject sourceObject, PlayerBuffSourceType sourceType = PlayerBuffSourceType.Other)
    {
        ApplyBuff(buffConfig, new PlayerBuffSourceContext(sourceType, sourceObject));
    }

    public void RecoverResource(RecoverTargetType recoverTargetType, float recoverValue)
    {
        if (recoverValue <= 0f)
        {
            return;
        }

        switch (recoverTargetType)
        {
            case RecoverTargetType.Health:
                ReusableData.health.Value = Mathf.Clamp(ReusableData.health.Value + recoverValue, 0f, maxHealth);
                break;
            case RecoverTargetType.Stamina:
                ReusableData.stamina.Value = Mathf.Clamp(ReusableData.stamina.Value + recoverValue, 0f, maxStamina);
                break;
            case RecoverTargetType.ReverseSystem:
                EnsureReverseRecoverTarget();
                reverseRecoverTarget?.RecoverFromInnermost(recoverValue);
                break;
        }
    }

    private void EnsureReverseRecoverTarget()
    {
        if (reverseRecoverTarget != null)
        {
            return;
        }
        if (reverseRecoverTargetBehaviour is IReverseRecoverTarget bound)
        {
            reverseRecoverTarget = bound;
            return;
        }
        reverseRecoverTarget = GetComponent<IReverseRecoverTarget>();
    }
}
