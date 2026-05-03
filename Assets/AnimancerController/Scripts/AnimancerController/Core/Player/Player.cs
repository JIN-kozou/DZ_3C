
using Animancer;
using DZ_3C.Reverse;
using UnityEngine;
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

    [Header("Player Resources")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField] private float initHealth = 100f;
    [SerializeField] private float initStamina = 100f;
    private float baseMoveSpeedMultiplier = 1f;

    [Header("Reverse System (optional)")]
    [SerializeField] private MonoBehaviour reverseRecoverTargetBehaviour;
    private IReverseRecoverTarget reverseRecoverTarget;

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
        base.Update();
        BuffSystem?.Tick(Time.deltaTime);
        UpdateBuffDrivenValues();
        StateMachine?.OnUpdate();
    }

    private void LateUpdate()
    {
        UpdateCharacterControllerStance();
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
