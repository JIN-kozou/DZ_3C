
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
            !(this is PlayerFallLoopState) &&
            !(this is PlayerPlatformerUpState))
        {
            reusableData.canCheckLedgeInAirAfterJump = false;
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
        reusableData.standValueParameter.TargetValue = 0;
    }

    protected void OnCrouchRelease(InputAction.CallbackContext context)
    {
        reusableData.standValueParameter.TargetValue = 1;
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
        reusableData.canCheckLedgeInAirAfterJump = true;
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
        float inAirMoveSpeed = numericConfig != null ? numericConfig.inAirMoveSpeed : 2f;
        float finalInAirSpeed = (inAirMoveSpeed + reusableData.buffSnapshot.moveSpeedAdditive) * reusableData.buffSnapshot.moveSpeedMultiplier;
        reusableData.horizontalSpeed = Mathf.Lerp(reusableData.horizontalSpeed, inputServer.Move != Vector2.zero ? Mathf.Max(0f, finalInAirSpeed) : 0, 1 - Mathf.Exp(-8 * Time.deltaTime));
        if (reusableData.lockValueParameter.TargetValue == 1)//索敌
        {
            //控制水平移动
            player.AddHorizontalVelocityInAir(GetTargetDir() * reusableData.horizontalSpeed*reusableData.currentMidInAirMultiplier+reusableData.currentInertialVelocity/Time.deltaTime);
        }
        else
        {
            //控制水平移动
            player.AddHorizontalVelocityInAir(player.transform.forward * reusableData.horizontalSpeed*reusableData.currentMidInAirMultiplier +reusableData.currentInertialVelocity / Time.deltaTime);
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