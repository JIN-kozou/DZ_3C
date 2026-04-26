
using UnityEngine;

public class PlayerMoveEndState : PlayerMovementState
{
    public PlayerMoveEndData moveEndData;
    float angle;
    float speed;
    public PlayerMoveEndState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        moveEndData = playerSO.playerMovementData.PlayerMoveEndData;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        angle = reusableData.rotationValueParameter.CurrentValue;
        speed = reusableData.speedValueParameter.CurrentValue;
        //判断是左脚还是右脚
        CheckLeftOrRightFoot();
    }

    private void CheckLeftOrRightFoot()
    {
        //判断是左脚还是右脚
        Transform leftFoot = player.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = player.animator.GetBoneTransform(HumanBodyBones.RightFoot);

        Vector3 leftFootLocalPos = player.transform.InverseTransformPoint(leftFoot.position);
        Vector3 rightFootLocalPos = player.transform.InverseTransformPoint(rightFoot.position);
        
        if (leftFootLocalPos.z > rightFootLocalPos.z)
        {
            Debug.Log("左腿在前");
            animancer.Play(moveEndData.moveEnd_L).Events(player).OnEnd = OnStateDefaultEnd;
        }
        else
        {
            Debug.Log("右腿在前");
            animancer.Play(moveEndData.moveEnd_R).Events(player).OnEnd = OnStateDefaultEnd;
        }
      
    }

    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Move.started += OnMoveStart;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
    }
    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Move.started -= OnMoveStart;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
    }
    public override void OnUpdate()
    {
        base.OnUpdate();
        reusableData.rotationValueParameter.TargetValue = angle;
        reusableData.speedValueParameter.TargetValue = speed;
    }

}