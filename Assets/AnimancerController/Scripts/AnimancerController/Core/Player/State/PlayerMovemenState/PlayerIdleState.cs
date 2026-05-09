using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerIdleState : PlayerMovementState
{
    const float LocomotionToIdleCrossFadeSeconds = 0.22f;

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

        bool fromLocomotion =
            ReferenceEquals(playerStateMachine.lastState, playerStateMachine.moveLoopState) ||
            ReferenceEquals(playerStateMachine.lastState, playerStateMachine.moveStartState) ||
            ReferenceEquals(playerStateMachine.lastState, playerStateMachine.armedState);

        if (fromLocomotion)
        {
            inputServer.SnapMoveSmoothToCurrentDiscrete();
            // ForceLockOn 只写 Target；混合树若读 Current，会有一帧未完全进入锁敌/蹲姿权重，松移动切 Idle 时易闪一下。
            reusableData.lockValueParameter.CurrentValue = 1f;
            reusableData.standValueParameter.CurrentValue = reusableData.standValueParameter.TargetValue;
        }

        if (!fromLocomotion)
        {
            reusableData.currentCrouchIdleIndex = -1;
            reusableData.currentStandIdleIndex = -1;
        }

        float holsterLocomotionFade = reusableData.ConsumePendingHolsterExitToIdleLocomotionFade();
        float idleCrossFade = holsterLocomotionFade > 0f ? holsterLocomotionFade : LocomotionToIdleCrossFadeSeconds;
        reusableLogic.InitIldeState(idleCrossFade);

        if (inputServer.MoveDiscrete == Vector2.zero)
        {
            reusableData.lock_X_ValueParameter.CurrentValue = 0f;
            reusableData.lock_Y_ValueParameter.CurrentValue = 0f;
            reusableData.rotationValueParameter.CurrentValue = 0f;
        }

        if (fromLocomotion)
        {
            reusableData.currentStandIdleIndex = reusableData.standIdleList.Count > 0 ? 0 : -1;
            reusableData.currentCrouchIdleIndex = reusableData.crouchIdleList.Count > 0 ? 0 : -1;
        }
        else
        {
            reusableLogic.PlayNextState();
        }

        // 与 <see cref="PlayerArmedState.TryEnterLocomotionIfMoveAlreadyHeld"/> 一致：收枪等切入 Idle 时若方向键一直按住，<see cref="InputAction.started"/> 不会再触发。
        TryEnterLocomotionIfMoveAlreadyHeld();
    }

    /// <summary>
    /// 键已按住时 <see cref="InputAction.started"/> 不会触发；否则收枪后进 Idle 会一直停到松键再按。
    /// </summary>
    private void TryEnterLocomotionIfMoveAlreadyHeld()
    {
        if (!player.isOnGround.Value || inputServer.MoveDiscrete == Vector2.zero)
        {
            return;
        }

        if (inputServer.Shift)
        {
            playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
        }
        else
        {
            playerStateMachine.ChangeState(playerStateMachine.moveStartState);
        }
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
        player.TryEnterArmedStateSameAsToggleWeaponInput();
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
