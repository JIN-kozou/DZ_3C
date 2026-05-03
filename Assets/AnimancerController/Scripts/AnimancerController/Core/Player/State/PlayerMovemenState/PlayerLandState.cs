/*************************************************
作者: HuHu
邮箱: 3112891874@qq.com
功能: 着陆状态
*************************************************/
using Animancer;
using UnityEngine.InputSystem;


public class PlayerLandState : PlayerMovementState
{
    public PlayerLandState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        //
        if (player.isOnGround.Value)
        {
            AnimancerState state = null;
            int index = 0;
            if (player.verticalSpeed < -15)
            {
                index = 1;
            }
            if (playerSO.playerMovementData.PlayerJumpFallAndLandData.placeJumpLand.Length == 1)
            {
                index = 0;
            }
            state = animancer.Play(playerSO.playerMovementData.PlayerJumpFallAndLandData.placeJumpLand[index]);
            state.Events(player).SetCallback(playerSO.playerParameterData.moveInterruptEvent, OnInputInterruption);
            state.Events(player).OnEnd = OnLandAnimationEnd;
        }
        else
        {
            OnLandAnimationEnd();
        }
    }

    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Move.started += OnMoveStart;
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled += OnCrouchRelease;
        player.isOnGround.ValueChanged += OnCheckFall;
        inputServer.inputMap.Player.ToggleWeapon.started += OnToggleWeapon;
    }

    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Move.started -= OnMoveStart;
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        inputServer.inputMap.Player.Crouch.canceled -= OnCrouchRelease;
        player.isOnGround.ValueChanged -= OnCheckFall;
        inputServer.inputMap.Player.ToggleWeapon.started -= OnToggleWeapon;
        reusableData.inputInterruptionCB = null;
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

    private void OnLandAnimationEnd()
    {
        if (reusableData.armedModeActive && reusableData.AllowsArmedWeaponActions())
        {
            playerStateMachine.ChangeState(playerStateMachine.armedState);
            return;
        }

        OnStateDefaultEnd();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (player.isOnGround.Value && inputServer.Move != UnityEngine.Vector2.zero)
        {
            if (reusableData.armedModeActive && reusableData.AllowsArmedWeaponActions())
            {
                playerStateMachine.ChangeState(playerStateMachine.armedState);
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
    }
}