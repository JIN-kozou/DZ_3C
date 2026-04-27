
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
        // 非 Humanoid Avatar 不能调用 GetBoneTransform，降级为按角度选停步动画。
        if (player.animator == null || !player.animator.isHuman)
        {
            PlayFallbackMoveEnd();
            return;
        }

        Transform leftFoot = player.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = player.animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (leftFoot == null || rightFoot == null)
        {
            PlayFallbackMoveEnd();
            return;
        }

        Vector3 leftFootLocalPos = player.transform.InverseTransformPoint(leftFoot.position);
        Vector3 rightFootLocalPos = player.transform.InverseTransformPoint(rightFoot.position);
        bool isLeftFootForward = leftFootLocalPos.z > rightFootLocalPos.z;
        var targetClip = isLeftFootForward ? moveEndData.moveEnd_L : moveEndData.moveEnd_R;
        animancer.Play(targetClip).Events(player).OnEnd = OnStateDefaultEnd;
    }

    private void PlayFallbackMoveEnd()
    {
        // 没有脚骨信息时，按当前转向参数做可重复的左右停步选择。
        var targetClip = angle >= 0f ? moveEndData.moveEnd_R : moveEndData.moveEnd_L;
        animancer.Play(targetClip).Events(player).OnEnd = OnStateDefaultEnd;
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