using UnityEngine.InputSystem;
public class PlayerIdleState : PlayerMovementState
{
    PlayerIdleData idleData;
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        idleData = playerSO.playerMovementData.PlayerIdleData;
    }
    public override void OnEnter()
    {
        if (!reusableData.armedModeActive)
        {
            player.ArmedPresentation?.NotifyArmedStateForceQuit();
        }

        base.OnEnter();
        reusableData.currentCrouchIdleIndex = -1;
        reusableData.currentStandIdleIndex = -1;
        float holsterLocomotionFade = reusableData.ConsumePendingHolsterExitToIdleLocomotionFade();
        reusableLogic.InitIldeState(holsterLocomotionFade);
        reusableLogic.PlayNextState();
    }
    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Move.started += MoveStart;
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
        //�������
        reusableData.lockValueParameter.Parameter.OnValueChanged += LockValueChange;
        inputServer.inputMap.Player.ToggleWeapon.started += OnToggleWeapon;
    }
    private void LockValueChange(float obj)
    {
       if (obj == 1||obj==0)//����
       {
            playerStateMachine.ChangeState(playerStateMachine.idleState);
       }
    }
    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Move.started -= MoveStart;
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
        reusableData.lockValueParameter.Parameter.OnValueChanged -= LockValueChange;
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
    private void MoveStart(InputAction.CallbackContext context)
    {
        playerStateMachine.ChangeState(playerStateMachine.moveStartState);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        UpdateCashVelocity(player.AnimationVelocity);
        UpdateSpeed();
    }


}
