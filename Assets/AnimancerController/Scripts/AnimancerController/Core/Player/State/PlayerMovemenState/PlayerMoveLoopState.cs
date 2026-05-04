using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveLoopState : PlayerMovementState
{
    PlayerMoveLoopData moveLoopData;
    int tid = -1;
    public PlayerMoveLoopState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        moveLoopData = playerSO.playerMovementData.PlayerMoveLoopData;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        animancer.Play(moveLoopData.moveLoop);
        OnCheckInput();
        reusableData.rotationValueParameter.CurrentValue = 0;
    }


    public override void OnUpdate()
    {
        base.OnUpdate();
        UpdateCashVelocity(player.AnimationVelocity);
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            UpdateRotation(true, 0.5f, false);
        }
        else
        {
            UpdateRotation(true, 0.4f, true, 1.4f);
        }
        UpdateSpeed();
         
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
        if (!reusableData.AllowsArmedWeaponActions())
        {
            return;
        }

        reusableData.armedModeActive = true;
        reusableData.resumeArmedAfterBreak = false;
        reusableData.weaponSuppressedUntilStandFromCrouch = false;
        reusableData.pendingCrouchAfterStandHolster = false;
        playerStateMachine.ChangeState(playerStateMachine.armedState);
    }
    public override void OnExit()
    {
        base.OnExit();
        if (tid > 0)
        {
            timerServer.RemoveTimer(tid);
            tid = -1;
        }
    }
    private void OnCheckMoveEnd(InputAction.CallbackContext context)
    {
        OnCheckInput();
    }

    private void OnCheckInput()
    {
        if (inputServer.Move != UnityEngine.Vector2.zero)
        {
            return;
        }
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
}
