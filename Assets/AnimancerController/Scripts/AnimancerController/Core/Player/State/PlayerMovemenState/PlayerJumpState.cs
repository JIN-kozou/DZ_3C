using UnityEngine;

public class PlayerJumpState : PlayerMovementState
{
    PlayerJumpFallAndLandData jumpFallAndLandData;

    
    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        jumpFallAndLandData = playerSO.playerMovementData.PlayerJumpFallAndLandData;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        reusableData.currentInertialVelocity = GetInertialVelocity();
        reusableData.currentInertialVelocity.y = 0;
        float inertiaSpeed = reusableData.currentInertialVelocity.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        float inertiaTriggerThreshold = numericConfig != null ? numericConfig.jumpInertiaTriggerSpeedThreshold : 0f;
        if (inertiaSpeed < inertiaTriggerThreshold)
        {
            reusableData.currentInertialVelocity = Vector3.zero;
        }
        Debug.Log("惯性速度：" + reusableData.currentInertialVelocity / Mathf.Max(Time.deltaTime, 0.0001f));

        float jumpHeight = numericConfig != null ? numericConfig.defaultJumpHeight : 0.8f;
        player.ChangeVerticalSpeed(ToolFunction.GetJumpInitVelocity(jumpHeight, player.gravity));

        //禁用动画y位移
        player.ignoreRootMotionY = false;    
        //统一使用原地跳跃动画
        animancer.Play(jumpFallAndLandData.placeJumpStart).Events(player).OnEnd = OnEnterFall;
        reusableData.isInPlaceJump = true;
    }


    protected override void AddEventListening()
    {
        base.AddEventListening();
        //检测着陆
        player.isOnGround.ValueChanged += OnFallToLand;
    }
    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        player.isOnGround.ValueChanged -= OnFallToLand;
        reusableData.inputInterruptionCB = null;
    }
    public override void OnUpdate()
    {
        base.OnUpdate();
        reusableLogic.InAirMoveCheck(GetTargetDir());
        InAirMove();
        UpdateRotation(false,0,true,2);
    }

 

    public override void OnExit()
    {
        base.OnExit();
        player.ignoreRootMotionY = false;
    }
  
}