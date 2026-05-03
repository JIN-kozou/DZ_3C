using Animancer;
using System;
using UnityEngine;
/**************************************************************************
作者: HuHu
邮箱: 3112891874@qq.com
功能: 逻辑算法复用类
**************************************************************************/
public class PlayerReusableLogic
{
    private const bool DrawClimbDebugRays = true;
    private const float ClimbDebugRayDuration = 0.08f;
    private static readonly Color ClimbScanHitColor = new Color(1f, 0.6f, 0f);
    private static readonly Color ClimbScanMissColor = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color ClimbTopSurfaceHitColor = Color.blue;
    private static readonly Color ClimbTopSurfaceMissColor = new Color(0.3f, 0.3f, 0.8f);

    Player player { get; set; }
    PlayerReusableData reusableData;
    AnimancerComponent animator;
    PlayerSO playerSO;
    PlayerMovementData playerMovementData;
    PlayerNumericConfig numericConfig;
    public PlayerReusableLogic(Player player)
    {
        this.player = player;
        playerSO = player.playerSO;
        playerMovementData = playerSO.playerMovementData;
        numericConfig = playerMovementData.PlayerNumericConfig;
        animator = player.animancer;
        reusableData = player.ReusableData;
    }
    public void InitIldeState()
    {
        var state = animator.Play(playerMovementData.PlayerIdleData.idle);
        bool isUpdateIdleState =( reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 0 )|| (!reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 1);
        if (reusableData.standIdleMixerState == null|| isUpdateIdleState)
        {
            if (reusableData.lockValueParameter.TargetValue == 1)//锁敌
            {
                reusableData.standIdleMixerState = state.GetChild(1).GetChild(1) as ManualMixerState;
                reusableData.isLockIdle = true;
            }
            else
            {
                reusableData.standIdleMixerState = state.GetChild(0).GetChild(1) as ManualMixerState;
                reusableData.isLockIdle = false;
            }
          
        }
        if (reusableData.crouchIdleMixerState == null ||isUpdateIdleState)
        {
            if (reusableData.lockValueParameter.TargetValue == 1)//锁敌
            {
                reusableData.crouchIdleMixerState = state.GetChild(1).GetChild(0) as ManualMixerState;
                reusableData.isLockIdle = true;
            }
            else
            {
                reusableData.crouchIdleMixerState = state.GetChild(0).GetChild(0) as ManualMixerState;
                reusableData.isLockIdle = false;
            }
        }

        if (reusableData.standIdleMixerState != null &&(reusableData.standIdleList.Count != reusableData.standIdleMixerState.ChildCount|| isUpdateIdleState))//拿到idleStandStates
        {
            reusableData.standIdleList.Clear();
            AnimancerState animancerState;
            for (int i = 0; i < reusableData.standIdleMixerState.ChildCount; i++)
            {
                animancerState = reusableData.standIdleMixerState.GetChild(i);
                animancerState.Events(player).OnEnd = PlayNextState;
                reusableData.standIdleList.Add(animancerState);
            }
            reusableData.standIdleList[0].Weight = 1;
        }
        if (reusableData.crouchIdleMixerState != null && (reusableData.crouchIdleList.Count != reusableData.crouchIdleMixerState.ChildCount || isUpdateIdleState))//拿到idleCrouchStates
        {
            reusableData.crouchIdleList.Clear();
            AnimancerState animancerState;
            for (int i = 0; i < reusableData.crouchIdleMixerState.ChildCount; i++)
            {
                animancerState = reusableData.crouchIdleMixerState.GetChild(i);
                if (reusableData.crouchIdleMixerState.ChildCount != 1)
                {
                    animancerState.Events(player).OnEnd = PlayNextState;
                }
                reusableData.crouchIdleList.Add(animancerState);
            }
            reusableData.crouchIdleList[0].Weight = 1;
        }
    }

    public void PlayNextState()
    {
        if (reusableData.standValueParameter.TargetValue == 1)
        {
            if (reusableData.standIdleList.Count == 0) return;
            // 计算下一个动画的索引
           reusableData.currentStandIdleIndex = (reusableData.currentStandIdleIndex + 1) % reusableData.standIdleList.Count;

            for (int i = 0; i < reusableData.standIdleList.Count; i++)
            {
                if (i == reusableData.currentStandIdleIndex)
                {
                    reusableData.standIdleList[i].SetWeight(1);
                    reusableData.standIdleList[i].Play();
                }
                else
                {
                    reusableData.standIdleList[i].SetWeight(0);
                    reusableData.standIdleList[i].Stop();
                }
            }
        }
        else if (reusableData.standValueParameter.TargetValue == 0)
        {
            if (reusableData.crouchIdleList.Count == 0) return;
            // 计算下一个动画的索引
            reusableData.currentCrouchIdleIndex = (reusableData.currentCrouchIdleIndex + 1) % reusableData.crouchIdleList.Count;

            for (int i = 0; i < reusableData.crouchIdleList.Count; i++)
            {
                if (i == reusableData.currentCrouchIdleIndex)
                {
                    reusableData.crouchIdleList[i].SetWeight(1);
                    reusableData.crouchIdleList[i].Play();
                }
                else
                {
                    reusableData.crouchIdleList[i].SetWeight(0);
                    reusableData.crouchIdleList[i].Stop();
                }
            }
        }

    }

    #region 按跳跃时的逻辑
    float detectionAngle => numericConfig != null ? numericConfig.climbDetectionAngle : 45f;
    float detectionDistance => numericConfig != null ? numericConfig.wallProbeDistance : 1f;
    float wallProbeRadius => numericConfig != null ? numericConfig.wallProbeRadius : 0.2f;
    float canClimbMaxHight => numericConfig != null ? numericConfig.canClimbMaxHeight : 3.2f;
    float canClimbMinHeight => numericConfig != null ? numericConfig.canClimbMinHeight : 0.3f;
    int detectionSamplingCount => numericConfig != null ? numericConfig.climbDetectionSamplingCount : 30;
    int climbConfirmFrames => numericConfig != null ? numericConfig.climbConfirmFrames : 2;
    private int climbConfirmCounter;
    /// <summary>
    /// 玩家按下跳跃键时
    /// </summary>
    public void OnJump()
    {
        // 跳跃按下时只进入跳跃状态，不在地面瞬间触发攀爬。
        player.StateMachine.ChangeState(player.StateMachine.jumpState);
    }

    /// <summary>
    /// 检测障碍物的最大高度
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="vaultHight"></param>
    /// <param name="wallNormal"></param>
    /// <returns></returns>
    private RaycastHit GetWallHight(Vector3 targetDir,float startDetectionHight,float maxDetectionHight, float detectionLength,ref float vaultHight,ref float obstructHeight,int detectionSamplingCount)
    {
        RaycastHit currentHit = default;
        Vector3 h = Vector3.zero;
        bool anyHit = false;
        float footY = player.GetCapsuleFootWorldY();
        Vector3 planarOrigin = new Vector3(player.transform.position.x, footY, player.transform.position.z);
        Vector3 startPos = planarOrigin + Vector3.up * startDetectionHight;
        float sampleQuantityPerUnit = (maxDetectionHight - startDetectionHight) / detectionSamplingCount;

        for (int i = 0; i <= detectionSamplingCount+1; i++)
        {
            Vector3 currentPos = startPos + Vector3.up * sampleQuantityPerUnit * i;
            if (TryAcquireWallHit(currentPos, targetDir, detectionLength, out var hitInfo))
            {
                currentHit = hitInfo;
                h = currentPos;
                anyHit = true;
                DrawRay(currentPos, targetDir, detectionLength, true, ClimbScanHitColor, ClimbScanMissColor);
            }
            else
            {
                DrawRay(currentPos, targetDir, detectionLength, false, ClimbScanHitColor, ClimbScanMissColor);
            }
        }
        if (!anyHit)
        {
            return default;
        }
        obstructHeight = currentHit.point.y - footY;
        if (obstructHeight >= canClimbMaxHight)//认为检测点高于最高爬的高度
        {
           // this.Log("障碍物太高");
            return default;
        }
        else if (obstructHeight<=0)//认为没有检测到任何障碍物
        {
          //  this.Log("障碍物太低或者没有");
            return default;
        }
        else
        {
            if (Physics.Raycast(h, -currentHit.normal, out var hitInfo, detectionDistance, player.whatIsGround, QueryTriggerInteraction.Ignore))
            {
                currentHit.point = hitInfo.point;
                DrawRay(h, -currentHit.normal, detectionDistance, true, ClimbTopSurfaceHitColor, ClimbTopSurfaceMissColor);
                //翻越高度 = 最高碰撞点高度 + 每单位采样高度
                vaultHight = currentHit.point.y + sampleQuantityPerUnit;
                return currentHit;
            }
            DrawRay(h, -currentHit.normal, detectionDistance, false, ClimbTopSurfaceHitColor, ClimbTopSurfaceMissColor);
            return default;
        }
    }
    #endregion

    #region 在空中的检测
    public void InAirMoveCheck(Vector3 targetDir)
    {
        if (!reusableData.canCheckClimbInAirAfterJump)
        {
            climbConfirmCounter = 0;
            return;
        }
        Vector2 moveInput = player.InputService.Move;
        if (moveInput.y <= 0)
        {
            climbConfirmCounter = 0;
            return;
        }

        // 只在空中且有朝向墙面的前向输入时，才触发攀爬/挂墙判定。
        if (TryTriggerClimbInAir(targetDir))
        {
            return;
        }

    }

    private bool TryTriggerClimbInAir(Vector3 targetDir)
    {
        float vaultHeight = 0;
        float obstructHeight = 0;
        RaycastHit hit = GetWallHight(targetDir, canClimbMinHeight, canClimbMaxHight, detectionDistance, ref vaultHeight, ref obstructHeight, detectionSamplingCount);
        if (hit.collider == null)
        {
            climbConfirmCounter = 0;
            return false;
        }

        float angle = Vector3.Angle(-targetDir.normalized, hit.normal);
        if (angle > detectionAngle)
        {
            climbConfirmCounter = 0;
            return false;
        }

        float mediumHighMin = numericConfig != null ? numericConfig.mediumHighClimbMinHeight : 2f;
        float mediumHighMax = numericConfig != null ? numericConfig.mediumHighClimbMaxHeight : 2.5f;
        float mediumMax = numericConfig != null ? numericConfig.mediumClimbMaxHeight : 1.7f;
        float lowMediumMax = numericConfig != null ? numericConfig.lowMediumClimbMaxHeight : 1f;
        float lowMax = numericConfig != null ? numericConfig.lowClimbMaxHeight : 0.35f;

        // 中高障碍仍不进入攀爬，保留为普通跳跃处理。
        if (obstructHeight >= mediumHighMin && obstructHeight < mediumHighMax)
        {
            climbConfirmCounter = 0;
            return false;
        }

        Vector3 vaultStartPos = new Vector3(hit.point.x, vaultHeight, hit.point.z);
        reusableData.vaultPos = vaultStartPos;
        reusableData.hit = hit;

        if (obstructHeight >= lowMediumMax && obstructHeight < mediumMax)
        {
            climbConfirmCounter++;
            if (climbConfirmCounter < climbConfirmFrames)
            {
                return false;
            }
            climbConfirmCounter = 0;
            reusableData.ObstructHeight = ObstructHeight.medium;
            reusableData.ClimbType = ClimbType.Climb;
            player.StateMachine.ChangeState(player.StateMachine.climbState);
            return true;
        }

        if (obstructHeight >= lowMax && obstructHeight < lowMediumMax)
        {
            climbConfirmCounter++;
            if (climbConfirmCounter < climbConfirmFrames)
            {
                return false;
            }
            climbConfirmCounter = 0;
            reusableData.ObstructHeight = ObstructHeight.lowMedium;
            reusableData.ClimbType = ClimbType.Climb;
            player.StateMachine.ChangeState(player.StateMachine.climbState);
            return true;
        }

        if (obstructHeight < lowMax)
        {
            climbConfirmCounter++;
            if (climbConfirmCounter < climbConfirmFrames)
            {
                return false;
            }
            climbConfirmCounter = 0;
            reusableData.ObstructHeight = ObstructHeight.low;
            reusableData.ClimbType = ClimbType.Climb;
            player.StateMachine.ChangeState(player.StateMachine.climbState);
            return true;
        }

        climbConfirmCounter = 0;
        return false;
    }
    #endregion
    /// <summary>
    /// 攀爬的位置（x\y\z）匹配,放在OnAnimationUpdate中调用，确保cc组件禁用，开启角色根运动
    /// </summary>
    public void ClimbTargetMatch(AnimancerState animancerState,ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
    {
        float currentTime = animancerState.NormalizedTime;
        if (!climbTargetMatchInfo.setTargetMatchInitPos && animator.States.Current.NormalizedTime > startNormalizedTime)
        {
            climbTargetMatchInfo.setTargetMatchInitPos = true;
            climbTargetMatchInfo.InitPos = player.transform.position;
        }
        if (currentTime > startNormalizedTime && currentTime < endNormalizedTime)
        {
            float t = (currentTime - startNormalizedTime) / (endNormalizedTime - startNormalizedTime);
            player.transform.position = Vector3.Lerp(climbTargetMatchInfo.InitPos, climbTargetMatchInfo.TargetPos, t);
        }
    }
    /// <summary>
    /// 攀爬的位置(只对Y)匹配,放在OnAnimationUpdate中调用，确保cc组件禁用，开启角色根运动
    /// </summary>
    public void ClimbTargetMatch_Y(AnimancerState animancerState,ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
    {
        float currentTime = animancerState.NormalizedTime;
        if (!climbTargetMatchInfo.setTargetMatchInitPos && animator.States.Current.NormalizedTime > startNormalizedTime)
        {
            climbTargetMatchInfo.setTargetMatchInitPos = true;
            climbTargetMatchInfo.InitPos = player.transform.position;
        }
        if (currentTime > startNormalizedTime && currentTime < endNormalizedTime)
        {
            float t = (currentTime - startNormalizedTime) / (endNormalizedTime - startNormalizedTime);

            Vector3 targetPos = new Vector3(player.transform.position.x, Mathf.Lerp(climbTargetMatchInfo.InitPos.y, climbTargetMatchInfo.TargetPos.y, t), player.transform.position.z);
            player.transform.position = targetPos;
        }
    }
    public void SetClimbTarget_Y_Task(AnimancerState animancerState, ref ClimbTargetMatchInfo climbTargetMatchInfo, float startNormalizedTime, float endNormalizedTime)
    {
        // 在方法开始处复制 ref 参数到一个局部变量
        var localClimbTargetMatchInfo = climbTargetMatchInfo;

        animationTask = () =>
        {
            // 使用局部变量替代 ref 参数
            ClimbTargetMatch_Y(animancerState, ref localClimbTargetMatchInfo, startNormalizedTime, endNormalizedTime);
        };

        // 在方法结束时返回修改后的值给原始 ref 参数
        climbTargetMatchInfo = localClimbTargetMatchInfo;

    }

    public void RemoveClimbTarget_Y_Task()
    {
        animationTask = null;
    }

    Action animationTask;
    public void SetAnimationMotionCompensationTask(Vector3 compensationMovement,AnimancerState targetAnimation, float startNormalTime=0,float endNormalizedTime = 1)
    {
        animationTask = () =>
        {
            if (!targetAnimation.IsPlaying)
            {
                return;
            }
            if (player.applyFullRootMotion)
            {
                player.applyFullRootMotion = true;
                player.controller.enabled = true;
            }
            float time = targetAnimation.Duration;
            float startSeconds = time * startNormalTime;
            float endSeconds = time * endNormalizedTime;
            if (targetAnimation.NormalizedTime >= startNormalTime && targetAnimation.NormalizedTime <= endNormalizedTime)
            {
                //计算目标时间内每帧的补偿量
                Vector3 frameCompensation = compensationMovement * (Time.deltaTime / (endSeconds - startSeconds));
                player.animatorDeltaPositionOffset = frameCompensation;
                Debug.Log("正在补偿位移：" + player.animatorDeltaPositionOffset);
            }
            else
            {
                player.animatorDeltaPositionOffset = Vector3.zero;
            }
        };
    }
    public void RemoveAnimationMotionCompensationTask()
    {
        player.animatorDeltaPositionOffset = Vector3.zero;
        animationTask = null;
    }

    private void DrawRay(Vector3 origin, Vector3 direction, float length, bool hit, Color hitColor, Color missColor)
    {
#if UNITY_EDITOR
        if (!DrawClimbDebugRays || direction == Vector3.zero)
        {
            return;
        }
        Color color = hit ? hitColor : missColor;
        Debug.DrawRay(origin, direction.normalized * length, color, ClimbDebugRayDuration);
#endif
    }

    private bool TryAcquireWallHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit hitInfo)
    {
        if (Physics.Raycast(origin, direction, out hitInfo, distance, player.whatIsGround, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        Vector3 castOrigin = origin - direction.normalized * wallProbeRadius;
        if (Physics.SphereCast(castOrigin, wallProbeRadius, direction, out hitInfo, distance + wallProbeRadius, player.whatIsGround, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return false;
    }

}