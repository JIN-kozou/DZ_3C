
using Animancer;
using System;
using System.Collections.Generic;
using UnityEngine;
public enum ObstructHeight
{
    low =0,lowMedium =1, medium =2, mediumHight =3,Hight =4,
}
public enum ClimbType
{
    Vault,Climb
}
public enum MatchType
{
    Root,
    RootY,
}
public struct ClimbTargetMatchInfo
{
   public Vector3 TargetPos;//爬上去的目标位置
   public Vector3 InitPos;//开始进行目标位置匹配的初始位置
   public bool setTargetMatchInitPos;//是否完成最后的匹配操作

    public ClimbTargetMatchInfo(Vector3 TargetPos)
    {
        this.TargetPos = TargetPos;

        InitPos = Vector3.zero;
        setTargetMatchInitPos = false;
    }
}
/**************************************************************************
作者: HuHu
邮箱: 3112891874@qq.com
功能: 可变数据复用类，缓存可读可写数据
**************************************************************************/

public class PlayerReusableData
{
    public float currentRotationTime;
    //animancer控制混合树Mixer用到的参数
    public SmoothedFloatParameter standValueParameter { get; set; }
    public SmoothedFloatParameter rotationValueParameter { get; set; }
    public SmoothedFloatParameter speedValueParameter { get; set; }
    public SmoothedFloatParameter lockValueParameter { get; set; }
    public SmoothedFloatParameter lock_X_ValueParameter { get; set; }
    public SmoothedFloatParameter lock_Y_ValueParameter { get; set; }
    //锁敌
    public BindableProperty<Transform> lockTarget { get; set; } = new BindableProperty<Transform>();

    public int drawTargetId = -1;
    public int drawCurrentId = -1;
    public Vector3 targetDir;
    public BindableProperty<float> targetAngle = new BindableProperty<float>();
    public BindableProperty<string> currentState = new BindableProperty<string>();
    public BindableProperty<float> health = new BindableProperty<float>();
    public BindableProperty<float> stamina = new BindableProperty<float>();
    public PlayerBuffRuntimeSnapshot buffSnapshot = PlayerBuffRuntimeSnapshot.Default;

    //IdleState
    public ManualMixerState standIdleMixerState;
    public ManualMixerState crouchIdleMixerState;
    public List<AnimancerState> standIdleList = new List<AnimancerState>();
    public List<AnimancerState> crouchIdleList = new List<AnimancerState>();
    public int currentStandIdleIndex;
    public int currentCrouchIdleIndex;
    public bool isLockIdle = false;
    //攀爬
    public ObstructHeight ObstructHeight;
    public ClimbType ClimbType;
    public ClipTransition targetClimbClip;
    //跳跃
    public float horizontalSpeed;
    public Vector3 inAirMoveDirection;
    //跳跃惯性
    public Vector3 currentInertialVelocity;
    public int cashIndex = 0;
    public readonly static int cashSize = 3;
    public Vector3[] cashVelocity = new Vector3[cashSize];

    //Climb
    public Vector3 vaultPos;
    public RaycastHit hit;
    //打断点检测事件
    public Action inputInterruptionCB { get; set; }
    //是否原地跳跃
    public bool isInPlaceJump;
    //仅在明确按下跳跃后，才允许空中攀爬检测
    public bool canCheckClimbInAirAfterJump = false;

    /// <summary>切换键打开：允许武器 Tick；主动关枪为 false。</summary>
    public bool armedModeActive;

    /// <summary>
    /// 离开 <see cref="PlayerArmedState"/> 时若仍为持枪模式（如起跳/空中/落地），下次再进持枪则跳过掏枪、仅同步 Layer/Mask（不覆盖 Layer0 当前片段）。
    /// 收枪、攀爬打断或 <see cref="PlayerArmedPresentation"/> 强制复位时会清零。
    /// </summary>
    public bool resumeArmedPresentationWithoutDraw;
    /// <summary>自 PlayerArmedState 因攀爬/死亡/下蹲互斥被踢出时置 true，占用结束后回到持枪。</summary>
    public bool resumeArmedAfterBreak;

    /// <summary>主动收枪动画结束后进入 Idle 时覆写 Layer0 淡入（秒）；&lt;0 表示不覆写。</summary>
    public float pendingHolsterExitToIdleLocomotionFadeSeconds = -1f;

    public float ConsumePendingHolsterExitToIdleLocomotionFade()
    {
        float v = pendingHolsterExitToIdleLocomotionFadeSeconds;
        pendingHolsterExitToIdleLocomotionFadeSeconds = -1f;
        return v;
    }
    /// <summary>自持枪因下蹲互斥退出后，站起前禁止武器 Tick（与 <see cref="AllowsArmedWeaponActions"/> 一起用于恢复持枪）。</summary>
    public bool weaponSuppressedUntilStandFromCrouch;
    /// <summary>松开蹲键或自动低矮探头下蹲时因头顶阻挡保持下蹲；头顶按站起射线净空后由状态机自动设回站立。</summary>
    public bool pendingStandWhenCrouchCeilingClears;
    /// <summary>持枪/ADS 时下蹲：已收枪并先站直，站直且蹲键仍按住时再自动下蹲。</summary>
    public bool pendingCrouchAfterStandHolster;
    //外力跳跃
    public float jumpExternalForce = 15;

    //
    public float currentMidInAirMultiplier = 0.6f;
    public PlayerReusableData(AnimancerComponent animancerComponent, PlayerSO playerSO)
    {
        standValueParameter  = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.standValueParameter,0.15f);
        standValueParameter.Parameter.Value = 1;

        rotationValueParameter = new SmoothedFloatParameter(animancerComponent,playerSO.playerParameterData.rotationValueParameter,0.2f);
        speedValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.speedValueParameter, 1f);
        lockValueParameter = new SmoothedFloatParameter(animancerComponent,playerSO.playerParameterData.LockValueParameter,0.1f);
        lock_X_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_X_ValueParameter, 0.3f);
        lock_Y_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_Y_ValueParameter, 0.3f);
    }

    /// <summary>持枪、射击、ADS 仅允许在「几乎完全站立」时生效，与下蹲姿态互斥。</summary>
    public bool AllowsArmedWeaponActions(float standEpsilon = 0.99f)
    {
        return standValueParameter != null && standValueParameter.CurrentValue >= standEpsilon;
    }
}