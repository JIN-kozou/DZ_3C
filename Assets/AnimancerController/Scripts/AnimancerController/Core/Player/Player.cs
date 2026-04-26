
using Animancer;
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

    protected override void Awake()
    {
        base.Awake();
        InputService = InputService.Instance;
        TimerService = TimerService.Instance;
        ApplyNumericConfig(playerSO?.playerMovementData?.PlayerNumericConfig);

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
        //�����߼�
        ReusableLogic = new PlayerReusableLogic(this);
        //����״̬��
        StateMachine = new PlayerStateMachine(this);
        //����Ĭ�Ͽ�ʼ״̬
        StateMachine.ChangeState(StateMachine.idleState);
    }
    protected override void Update()
    {
        base.Update();
        StateMachine?.OnUpdate();
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

}
