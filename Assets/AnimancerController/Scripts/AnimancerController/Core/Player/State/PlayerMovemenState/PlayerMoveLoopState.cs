using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveLoopState : PlayerMovementState
{
    const float StanceLocomotionCrossFade = 0.08f;

    PlayerMoveLoopData moveLoopData;
    int tid = -1;
    bool _moveLoopPlayingCrouchAsset;
    /// <summary>蹲姿松移动：至少再跑一整帧 MoveLoop（让 SmoothMove + blend 收到停步）再进 Idle，减轻与 idle 根图切换的硬跳。</summary>
    int _idleAfterCrouchReleaseNotBeforeFrame = -1;

    public PlayerMoveLoopState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        moveLoopData = playerSO.playerMovementData.PlayerMoveLoopData;
    }
    public override void OnEnter()
    {
        base.OnEnter();
        _idleAfterCrouchReleaseNotBeforeFrame = -1;
        bool crouchIntent = reusableData.standValueParameter.TargetValue < 0.99f;
        _moveLoopPlayingCrouchAsset = crouchIntent;
        PlayMoveLoopTransition(moveLoopData.ResolveMoveLoop(crouchIntent));
        OnCheckInput();
        SyncRotationParameterToLocomotionEntry();
    }

    void PlayMoveLoopTransition(TransitionAsset transition)
    {
        if (transition != null && transition.IsValid)
        {
            animancer.Play((ITransition)transition);
        }
    }


    public override void OnUpdate()
    {
        base.OnUpdate();

        if (inputServer.MoveDiscrete != Vector2.zero)
        {
            _idleAfterCrouchReleaseNotBeforeFrame = -1;
        }
        else if (_idleAfterCrouchReleaseNotBeforeFrame >= 0 &&
                 Time.frameCount >= _idleAfterCrouchReleaseNotBeforeFrame)
        {
            _idleAfterCrouchReleaseNotBeforeFrame = -1;
            playerStateMachine.ChangeState(playerStateMachine.idleState);
            return;
        }

        bool crouchIntent = reusableData.standValueParameter.TargetValue < 0.99f;
        if (crouchIntent != _moveLoopPlayingCrouchAsset)
        {
            _moveLoopPlayingCrouchAsset = crouchIntent;
            TransitionAsset next = moveLoopData.ResolveMoveLoop(crouchIntent);
            if (next != null && next.IsValid)
            {
                animancer.Play((ITransition)next, StanceLocomotionCrossFade);
            }
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

        if (!player.CanBeginArmedPresentationNow())
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
        // 用 MoveDiscrete：松键后 Move 仍可能因 SmoothDamp 非零，否则会卡在 MoveLoop。
        if (inputServer.MoveDiscrete != UnityEngine.Vector2.zero)
        {
            return;
        }

        bool crouchIntent = reusableData.standValueParameter.TargetValue < 0.99f;
        if (crouchIntent)
        {
            // 当前帧之后至少再经过 1 个完整 Update（Time.frameCount+2），本帧已跑过的 MoveLoop.OnUpdate 不算。
            _idleAfterCrouchReleaseNotBeforeFrame = Time.frameCount + 2;
            return;
        }

        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
}
