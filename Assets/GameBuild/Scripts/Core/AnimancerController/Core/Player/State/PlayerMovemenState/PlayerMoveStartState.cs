using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;
/**************************************************************************
作者: HuHu
邮箱: 3112891874@qq.com
功能: 玩家起步状态
**************************************************************************/
public class PlayerMoveStartState : PlayerMovementState
{
    PlayerMoveStartData moveStartData;
    float targetAngle;
    bool isForwardMove;
    int tid = -1;
    AnimancerState state = null;
    public PlayerMoveStartState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        moveStartData = playerSO.playerMovementData.PlayerMoveStartData;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
            return;
        }

        bool crouchIntent = reusableData.standValueParameter.TargetValue < 0.99f;
        targetAngle = UpdateRotation();
        TransitionAsset startTrans = moveStartData.ResolveMoveStart(targetAngle, crouchIntent);
        if (startTrans == null || !startTrans.IsValid)
        {
            playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
            return;
        }

        state = animancer.Play(startTrans);
        isForwardMove = targetAngle < 22.5f && targetAngle >= 0f || targetAngle >= -22.5f && targetAngle <= 0f;
        state.Events(player).OnEnd = OnMoveStartEnd;
    }
    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Move.canceled += OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
        inputServer.inputMap.Player.ToggleWeapon.started += OnToggleWeapon;
    }
    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Move.canceled -= OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
        inputServer.inputMap.Player.ToggleWeapon.started -= OnToggleWeapon;
    }

    private void OnToggleWeapon(InputAction.CallbackContext context)
    {
        player.TryEnterArmedStateSameAsToggleWeaponInput();
    }

    private void OnCheckMoveEnd(InputAction.CallbackContext context)
    {
        OnCheckInput();
    }

    private void OnCheckInput()
    {
        if (inputServer.MoveDiscrete != UnityEngine.Vector2.zero)
        {
            return;
        }
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
    public override void OnExit()
    {
        base.OnExit();
        if (tid > 0)
        {
            timerServer.RemoveTimer(tid);
            tid = -1;
        }
        isForwardMove = false;
    }
    private void OnMoveStartEnd()
    {
        playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        UpdateCashVelocity(player.AnimationVelocity);
        if (state.NormalizedTime > 0.4f|| isForwardMove)
        {
            UpdateRotation(false, 0.7f, true, 1.8f);
        }
        UpdateSpeed();
    }
}