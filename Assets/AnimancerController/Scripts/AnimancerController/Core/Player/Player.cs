
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
        StateMachine?.OnUpdate();
        TryHolsterWeaponInputIfArmed();
        TickArmedUpperBodyAdsIfNeeded();
        WeaponRuntime?.Tick(Time.deltaTime);
        TryApplyPendingCrouchAfterStandHolster();
        TryResumeArmedAfterCrouchStand();
    }

    /// <summary>
    /// 收枪键（如键盘 3）在任意持枪 locomotion 状态下都应生效；此前仅在 <see cref="PlayerArmedState"/> 内轮询，ADS 在跑循环时退出后按 3 无效。
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

        if (ArmedPresentation.IsExiting)
        {
            return;
        }

        ArmedPresentation.BeginArmedExit(() =>
        {
            ReusableData.armedModeActive = false;
            ReusableData.resumeArmedAfterBreak = false;
            ReusableData.weaponSuppressedUntilStandFromCrouch = false;
            ReusableData.pendingCrouchAfterStandHolster = false;
            StateMachine.ChangeState(StateMachine.idleState);
        });
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

        ArmedPresentation.TickUpperBodyAds(
            InputService.ADSHeld,
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

        if (!InputService.CrouchHeld)
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

        if (!ReusableData.weaponSuppressedUntilStandFromCrouch)
        {
            return;
        }

        if (ReusableData.standValueParameter.CurrentValue < 0.99f)
        {
            return;
        }

        ReusableData.weaponSuppressedUntilStandFromCrouch = false;
        if (ReusableData.resumeArmedAfterBreak && ReusableData.armedModeActive)
        {
            ReusableData.resumeArmedAfterBreak = false;
            StateMachine.ChangeState(StateMachine.armedState);
        }
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
    /// 按站立/下蹲混合值（standValueParameter：1=站立，0=下蹲）插值更新 CharacterController 尺寸。
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

        float standBlend = Mathf.Clamp01(ReusableData.standValueParameter.CurrentValue);
        float radius = Mathf.Lerp(cfg.crouchControllerRadius, cfg.standControllerRadius, standBlend);
        float height = Mathf.Lerp(cfg.crouchControllerHeight, cfg.standControllerHeight, standBlend);
        Vector3 center = Vector3.Lerp(cfg.crouchControllerCenter, cfg.standControllerCenter, standBlend);

        height = Mathf.Max(height, radius * 2f + 0.001f);
        float maxRadius = height * 0.5f - 0.0005f;
        radius = Mathf.Clamp(radius, 0.01f, maxRadius);

        controller.radius = radius;
        controller.height = height;
        controller.center = center;
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
