using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerArmedState : PlayerMovementState
{
    private PlayerMoveLoopData _moveLoopData;

    public PlayerArmedState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        _moveLoopData = playerSO.playerMovementData.PlayerMoveLoopData;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        reusableData.resumeArmedAfterBreak = false;
        reusableData.weaponSuppressedUntilStandFromCrouch = false;
        animancer.Play(_moveLoopData.moveLoop);
        reusableData.rotationValueParameter.CurrentValue = 0;
        var weapon = player.GetComponent<PlayerWeaponRuntime>();
        weapon?.RefillMagazine();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (inputServer.HolsterWeaponWasPressedThisFrame)
        {
            Holster();
            return;
        }

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

    private void Holster()
    {
        reusableData.armedModeActive = false;
        reusableData.resumeArmedAfterBreak = false;
        reusableData.weaponSuppressedUntilStandFromCrouch = false;
        reusableData.pendingCrouchAfterStandHolster = false;
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }

    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Move.canceled += OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started += OnCrouchFromArmed;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
    }

    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Move.canceled -= OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started -= OnCrouchFromArmed;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
    }

    private void OnCrouchFromArmed(InputAction.CallbackContext context)
    {
        reusableData.pendingStandWhenCrouchCeilingClears = false;
        reusableData.armedModeActive = false;
        reusableData.resumeArmedAfterBreak = false;
        reusableData.weaponSuppressedUntilStandFromCrouch = false;
        reusableData.pendingCrouchAfterStandHolster = true;
        reusableData.standValueParameter.TargetValue = 1;
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }

    private void OnCheckMoveEnd(InputAction.CallbackContext context)
    {
        if (inputServer.Move != Vector2.zero)
        {
            return;
        }

        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
}
