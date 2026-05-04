using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 地面持枪待机：与 <see cref="PlayerIdleState"/> 不同，必须显式响应位移输入；
/// 否则 Layer0 多为 idle、根运动近零，会表现为「持枪走不动」，直到跳跃等流程切到带位移的状态。
/// </summary>
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
        var armedAnim = playerSO.playerMovementData.PlayerArmedAnimationData;
        bool resumeLayers = reusableData.resumeArmedPresentationWithoutDraw;
        reusableData.resumeArmedPresentationWithoutDraw = false;
        player.ArmedPresentation.BeginArmedEnter(_moveLoopData.moveLoop, armedAnim, resumeLayers);
        reusableData.rotationValueParameter.CurrentValue = 0;
        var weapon = player.GetComponent<PlayerWeaponRuntime>();
        weapon?.RefillMagazine();
        TryEnterLocomotionIfMoveAlreadyHeld();
    }

    public override void OnExit()
    {
        reusableData.resumeArmedPresentationWithoutDraw = reusableData.armedModeActive;
        base.OnExit();
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
        inputServer.inputMap.Player.Move.started += OnMoveStart;
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Move.canceled += OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started += OnCrouchFromArmed;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
    }

    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Move.started -= OnMoveStart;
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Move.canceled -= OnCheckMoveEnd;
        inputServer.inputMap.Player.Crouch.started -= OnCrouchFromArmed;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
    }

    private void OnCrouchFromArmed(InputAction.CallbackContext context)
    {
        if (player.ArmedPresentation != null && player.ArmedPresentation.IsExiting)
        {
            return;
        }

        player.ArmedPresentation.BeginArmedExit(() =>
        {
            reusableData.pendingStandWhenCrouchCeilingClears = false;
            reusableData.armedModeActive = false;
            reusableData.resumeArmedAfterBreak = false;
            reusableData.weaponSuppressedUntilStandFromCrouch = false;
            reusableData.pendingCrouchAfterStandHolster = true;
            reusableData.standValueParameter.TargetValue = 1;
            playerStateMachine.ChangeState(playerStateMachine.idleState);
        });
    }

    /// <summary>
    /// 从跑循环切持枪等场景下方向键已按住，<see cref="InputAction.started"/> 不会再触发，需在进态末尾补一次与 <see cref="PlayerLandState"/> 相同的分流。
    /// </summary>
    private void TryEnterLocomotionIfMoveAlreadyHeld()
    {
        if (!player.isOnGround.Value || inputServer.Move == Vector2.zero)
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

    private void OnCheckMoveEnd(InputAction.CallbackContext context)
    {
        if (inputServer.Move != Vector2.zero)
        {
            return;
        }

        if (player.ArmedPresentation != null && player.ArmedPresentation.IsExiting)
        {
            return;
        }

        player.ArmedPresentation.BeginArmedExit(() =>
        {
            playerStateMachine.ChangeState(playerStateMachine.idleState);
        });
    }
}
