
using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementState : StateBase
{
    protected PlayerStateMachine playerStateMachine;
    protected PlayerSO playerSO;
    protected PlayerNumericConfig numericConfig;
    protected float stateSwitchCooldown => numericConfig != null ? numericConfig.stateSwitchCooldown : 0.1f;
    private float lastFallSwitchTime = -10f;
    private float lastLandSwitchTime = -10f;
    //属性变量
    public PlayerMovementState(PlayerStateMachine stateMachine) : base(stateMachine.player)
    {
        playerStateMachine = stateMachine;
        playerSO = player.playerSO;
        numericConfig = playerSO?.playerMovementData?.PlayerNumericConfig;
    }
    public override void OnEnter()
    {
        if (!(this is PlayerJumpState) &&
            !(this is PlayerFallLoopState))
        {
            reusableData.canCheckClimbInAirAfterJump = false;
        }
        ForceLockOn();
        AddEventListening();
    }
    protected override void AddEventListening()//注册的顺序也决定了优先级
    {
    }

   
    protected override void RemoveEventListening()
    {
    }
    public override void OnExit()
    {
        RemoveEventListening();
    }

    public override void OnUpdate()
    {
        ForceLockOn();
        //处理索敌
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            UpdateLockRotation(5,null);
            //更新参数：
            UpdateLockValue();
        }
        //处理打断委托
        reusableData.inputInterruptionCB?.Invoke();
        TryApplyPendingStandWhenCrouchCeilingClears();
        TryLowCeilingAutoCrouchOnGround();
    }

    /// <summary>仅在地面时尝试「头顶挡则自动下蹲」；跳跃/下落/攀爬等状态重写为 false。</summary>
    protected virtual bool ShouldApplyLowCeilingAutoCrouchOnGround()
    {
        return player.isOnGround.Value;
    }

    /// <summary>
    /// 头顶（自动下蹲射线）有障碍时拉成下蹲，并与松蹲被挡共用 <see cref="PlayerReusableData.pendingStandWhenCrouchCeilingClears"/>，
    /// 由 <see cref="TryApplyPendingStandWhenCrouchCeilingClears"/> 在净空（站起射线）后站起，避免单独「净空起身」与自动蹲来回打架导致抽搐。
    /// </summary>
    private void TryLowCeilingAutoCrouchOnGround()
    {
        float autoRayLen = numericConfig != null ? numericConfig.lowCeilingAutoCrouchCheckRayLength : 1.2f;
        if (autoRayLen <= 0f)
        {
            return;
        }

        if (!ShouldApplyLowCeilingAutoCrouchOnGround())
        {
            return;
        }

        if (reusableData.pendingCrouchAfterStandHolster)
        {
            return;
        }

        if (!HasCeilingObstacleForAutoCrouchOnGround())
        {
            return;
        }

        if (reusableData.standValueParameter.TargetValue < 0.99f)
        {
            return;
        }

        reusableData.pendingStandWhenCrouchCeilingClears = true;
        reusableData.standValueParameter.TargetValue = 0;
    }

    private void UpdateLockValue()
    {
        reusableData.lock_X_ValueParameter.TargetValue = inputServer.Move.x * reusableData.speedValueParameter.TargetValue;
        reusableData.lock_Y_ValueParameter.TargetValue = inputServer.Move.y * reusableData.speedValueParameter.TargetValue;
    }

    public override void OnAnimationEnd()
    {

    }
    public override void OnAnimationUpdate()
    {
    }
    private void ForceLockOn()
    {
        reusableData.lockValueParameter.TargetValue = 1;
        reusableData.lockTarget.Value = cam;
    }

    protected void OnCrouch(InputAction.CallbackContext context)
    {
        reusableData.pendingStandWhenCrouchCeilingClears = false;
        if (reusableData.armedModeActive)
        {
            reusableData.armedModeActive = false;
            reusableData.resumeArmedAfterBreak = false;
            reusableData.weaponSuppressedUntilStandFromCrouch = false;
            reusableData.pendingCrouchAfterStandHolster = true;
            reusableData.standValueParameter.TargetValue = 1;
            return;
        }

        reusableData.standValueParameter.TargetValue = 0;
    }

    protected void OnCrouchRelease(InputAction.CallbackContext context)
    {
        if (reusableData.standValueParameter.CurrentValue >= 0.99f)
        {
            reusableData.pendingStandWhenCrouchCeilingClears = false;
            reusableData.standValueParameter.TargetValue = 1;
            return;
        }

        if (HasCeilingObstacleForStanceAndJump())
        {
            reusableData.pendingStandWhenCrouchCeilingClears = true;
            return;
        }
        reusableData.pendingStandWhenCrouchCeilingClears = false;
        reusableData.standValueParameter.TargetValue = 1;
    }

    /// <summary>松蹲站起 / pending 净空站起 / 阻挡起跳。射线长度见 <see cref="PlayerNumericConfig.crouchStandCeilingCheckRayLength"/>。</summary>
    private bool HasCeilingObstacleForStanceAndJump()
    {
        float rayLength = numericConfig != null ? numericConfig.crouchStandCeilingCheckRayLength : 1.2f;
        return player.RaycastCeilingAboveCapsule(rayLength);
    }

    /// <summary>仅用于「地面自动下蹲」触发判定。射线长度见 <see cref="PlayerNumericConfig.lowCeilingAutoCrouchCheckRayLength"/>。</summary>
    private bool HasCeilingObstacleForAutoCrouchOnGround()
    {
        float rayLength = numericConfig != null ? numericConfig.lowCeilingAutoCrouchCheckRayLength : 1.2f;
        return player.RaycastCeilingAboveCapsule(rayLength);
    }

    private void TryApplyPendingStandWhenCrouchCeilingClears()
    {
        if (!reusableData.pendingStandWhenCrouchCeilingClears)
        {
            return;
        }

        if (HasCeilingObstacleForStanceAndJump())
        {
            return;
        }

        float predictLen = numericConfig != null ? numericConfig.lowCeilingAutoCrouchPostStandPredictRayLength : 1.2f;
        if (predictLen > 0f && WouldPredictedStandCapsuleTopHitCeiling(predictLen))
        {
            return;
        }

        reusableData.standValueParameter.TargetValue = 1;
        reusableData.pendingStandWhenCrouchCeilingClears = false;
    }

    /// <summary>
    /// 用配置的站立 CC 中心与高度推算胶囊顶（不改动当前 CC），用于预测站起后头顶是否仍挡。
    /// </summary>
    private bool WouldPredictedStandCapsuleTopHitCeiling(float rayLength)
    {
        if (numericConfig == null || rayLength <= 0f)
        {
            return false;
        }

        Transform t = player.transform;
        Vector3 standCenterWorld = t.TransformPoint(numericConfig.standControllerCenter);
        float topY = standCenterWorld.y + numericConfig.standControllerHeight * 0.5f - 0.02f;
        Vector3 origin = new Vector3(standCenterWorld.x, topY, standCenterWorld.z);

        return Physics.Raycast(origin, Vector3.up, rayLength, player.whatIsGround, QueryTriggerInteraction.Ignore);
    }
    protected float UpdateSpeed()
    {
       float walkSpeed = numericConfig != null ? numericConfig.walkSpeedParameter : 1f;
       float runSpeed = numericConfig != null ? numericConfig.runSpeedParameter : 2f;
       float baseSpeed = inputServer.Shift ? runSpeed : walkSpeed;
       float finalSpeed = (baseSpeed + reusableData.buffSnapshot.moveSpeedAdditive) * reusableData.buffSnapshot.moveSpeedMultiplier;
       return reusableData.speedValueParameter.TargetValue = Mathf.Max(0f, finalSpeed);
    }
    protected float UpdateRotation(bool isUpdateRotationParameter = true, float rotationSmoothTime = 0.7f, bool isRotationCompensation = true, float rotationSize = 1.4f)
    {
        float angle = GetTargetAngle();
        if (isUpdateRotationParameter)
        {
            reusableData.rotationValueParameter.SmoothTime = rotationSmoothTime;
            reusableData.rotationValueParameter.TargetValue = angle * Mathf.Deg2Rad;
        }
        if (inputServer.Move != Vector2.zero)
        {
            if (isRotationCompensation)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(reusableData.targetDir), Time.deltaTime * rotationSize);
            }
            return angle;
        }
        return 0;
    }
    protected void UpdateLockRotation(float rotationSize ,Transform lockTarget= null)
    {
        if (lockTarget == null)
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)),Time.deltaTime*rotationSize);
        }
        else
        {
            Vector3 dir  = (lockTarget.position - player.transform.position).normalized;
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up)), Time.deltaTime * rotationSize);
        }
    }
    protected void UpdateLockRotation(float rotationSize, Vector3 normal = default)
    {
        if (normal == default)
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)), Time.deltaTime * rotationSize);
        }
        else
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up)), Time.deltaTime * rotationSize);
        }
    }

    protected float GetTargetAngle()
    {
        reusableData.targetDir = GetTargetDir();
        reusableData.targetAngle.Value = ToolFunction.GetDeltaAngle(player.transform, reusableData.targetDir);
        return reusableData.targetAngle.Value;
    }
    protected Vector3 GetTargetDir()
    {
        return Quaternion.Euler(0, cam.eulerAngles.y, 0) * new Vector3(inputServer.Move.x, 0, inputServer.Move.y);
    }

    /// <summary>
    /// 检测是否有输入打断（事件打断）
    /// </summary>
    protected virtual void OnInputInterruption()
    {
        Debug.Log("添加打断检测");
        reusableData.inputInterruptionCB = () =>
            {
                if (inputServer.Move != Vector2.zero)
                {
                    if (player.isOnGround.Value)
                    {
                        playerStateMachine.ChangeState(playerStateMachine.moveStartState);
                        reusableData.inputInterruptionCB = null;
                    }
                }
            };
    }
    
    protected void OnJumpStart(InputAction.CallbackContext context)
    {
        if (HasCeilingObstacleForStanceAndJump())
        {
            return;
        }
        reusableData.canCheckClimbInAirAfterJump = true;
        reusableLogic.OnJump();
    }
    protected void OnEnterFall()
    {
        if (Time.time - lastFallSwitchTime < stateSwitchCooldown)
        {
            return;
        }
        lastFallSwitchTime = Time.time;
        playerStateMachine.ChangeState(playerStateMachine.fallLoopState);
    }
    protected void OnMoveStart(InputAction.CallbackContext context)
    {
        playerStateMachine.ChangeState(playerStateMachine.moveStartState);
    }
    protected void OnCheckFall(bool isGround)
    {
        if (!isGround)
        {
            timerServer.AddTimer(50, OnLandToFall);
        }
    }
    protected void OnFallToLand(bool onGround)
    {
        if (onGround)
        {
            if (Time.time - lastLandSwitchTime < stateSwitchCooldown)
            {
                return;
            }
            lastLandSwitchTime = Time.time;
            playerStateMachine.ChangeState(playerStateMachine.landState);
        }
    }

    protected void OnLandToFall()
    {
        if (!player.isOnGround.Value)
        {
            OnEnterFall();
        }
        else
        {
            OnStateDefaultEnd();
        }
    }
    protected void InAirMove()
    {
        if (player.isOnGround.Value)
        {
            return;
        }
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float inAirSpeedCap = numericConfig != null ? numericConfig.inAirSpeedCap : 6f;
        float inAirSpeedDecay = numericConfig != null ? numericConfig.inAirSpeedDecay : 3.5f;
        float maintainAcceleration = numericConfig != null ? numericConfig.inAirInputMaintainAcceleration : 8f;
        float inAirMoveSpeed = numericConfig != null ? numericConfig.inAirMoveSpeed : 2f;
        float finalInAirSpeed = (inAirMoveSpeed + reusableData.buffSnapshot.moveSpeedAdditive) * reusableData.buffSnapshot.moveSpeedMultiplier;
        float cappedMaintainSpeed = Mathf.Min(inAirSpeedCap, Mathf.Max(0f, finalInAirSpeed));

        // 入空后速度逐帧衰减；有输入时可把速度维持/回补到期望空中巡航速度。
        reusableData.horizontalSpeed = Mathf.Max(0f, reusableData.horizontalSpeed - inAirSpeedDecay * dt);
        if (inputServer.Move != Vector2.zero)
        {
            Vector3 inputDir = GetTargetDir();
            if (inputDir.sqrMagnitude > 0.0001f)
            {
                reusableData.inAirMoveDirection = inputDir.normalized;
            }
            if (reusableData.horizontalSpeed < cappedMaintainSpeed)
            {
                reusableData.horizontalSpeed = Mathf.MoveTowards(reusableData.horizontalSpeed, cappedMaintainSpeed, maintainAcceleration * dt);
            }
        }
        reusableData.horizontalSpeed = Mathf.Min(reusableData.horizontalSpeed, inAirSpeedCap);

        Vector3 moveDirection = reusableData.inAirMoveDirection.sqrMagnitude > 0.0001f
            ? reusableData.inAirMoveDirection
            : player.transform.forward;
        Vector3 airVelocity = moveDirection * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier;
        if (reusableData.lockValueParameter.TargetValue == 1)//索敌
        {
            player.AddHorizontalVelocityInAir(airVelocity);
        }
        else
        {
            player.AddHorizontalVelocityInAir(airVelocity);
        }
    }
    /// <summary>
    /// 在地面时刷新
    /// </summary>
    /// <param name="horizontalSpeed"></param>
    public void UpdateCashVelocity(Vector3 horizontalSpeed)
    {
        reusableData.cashIndex = (reusableData.cashIndex + 1) % PlayerReusableData.cashSize;
        reusableData.cashVelocity[reusableData.cashIndex] = horizontalSpeed;
    }
    /// <summary>
    /// 离开地面时获取
    /// </summary>
    /// <returns></returns>
    public Vector3 GetInertialVelocity()
    {
        Vector3 inertialVelocity = Vector3.zero;
        for (int i = 0; i < reusableData.cashVelocity.Length; i++)
        {
            inertialVelocity += reusableData.cashVelocity[i];
        }
        return inertialVelocity / reusableData.cashVelocity.Length;
    }

    /// <summary>
    /// 默认播放Idle
    /// </summary>
    protected void OnStateDefaultEnd()
    {
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }

}