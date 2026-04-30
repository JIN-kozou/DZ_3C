using UnityEngine;

[System.Serializable]
public class PlayerNumericConfig
{
    [Header("Move")]
    [Min(0.1f)] public float moveSpeedMultiplier = 1f;
    public float walkSpeedParameter = 1f;
    public float runSpeedParameter = 2f;
    public float inAirMoveSpeed = 2f;

    [Header("Gravity & Ground")]
    public float gravity = -12f;
    public Vector2 velocityLimit = new Vector2(-20f, 60f);
    public float groundDetectedOffset = -0.06f;
    public float groundRadius = 0.5f;
    [Min(0.1f)] public float groundProbeDistance = 1.5f;
    [Min(1)] public int groundNormalSmoothFrames = 3;
    [Tooltip("从脚底地面探测球心（detectedOrigin）再向上偏移后发射向下射线取法线；相对“从角色中心向下”更不易误打到前方陡坡。")]
    [Min(0.05f)] public float groundNormalRayStartHeight = 0.35f;
    [Min(0.1f)] public float groundNormalRayDistance = 4f;

    [Header("Slope")]
    [Range(0f, 89f)] public float maxWalkableSlopeAngle = 45f;
    [Range(0f, 89f)] public float slideStartAngle = 52f;
    [Tooltip("实际下滑阈值 = min(slideStartAngle, maxWalkableSlopeAngle + 本值)，避免可走坡与下滑阈值之间出现长时间“既不蹭上坡也不下滑”的真空带。")]
    [Min(0f)] public float slideLeadPastWalkableDegrees = 2f;
    [Min(0f)] public float slideAcceleration = 16f;
    [Min(0f)] public float slideMaxSpeed = 8f;
    [Min(0f)] public float slideControlDamping = 4f;
    [Range(0f, 1f)] public float minGroundNormalY = 0.1f;

    [Header("Jump")]
    [Min(0f)] public float defaultJumpHeight = 0.8f;
    [Min(0f)] public float outPlaceJumpHeight = 0.8f;
    [Min(0f)] public float platformerJumpHeight = 15f;
    [Min(0f)] public float jumpInertiaTriggerSpeedThreshold = 0.2f;
    [Min(0f)] public float inAirSpeedCap = 6f;
    [Min(0f)] public float inAirSpeedDecay = 3.5f;
    [Min(0f)] public float inAirInputMaintainAcceleration = 8f;

    [Header("Climb Detection")]
    [Min(0f)] public float climbDetectionAngle = 45f;
    [Min(0f)] public float climbDetectionDistance = 1f;
    [Min(0f)] public float wallProbeRadius = 0.2f;
    [Min(0f)] public float wallProbeDistance = 1f;
    [Min(0f)] public float canClimbMaxHeight = 3.2f;
    [Min(0f)] public float canClimbMinHeight = 0.3f;
    [Min(1)] public int climbDetectionSamplingCount = 30;
    [Min(1)] public int climbConfirmFrames = 2;

    [Header("Climb Obstacle Height Ranges")]
    [Min(0f)] public float lowClimbMaxHeight = 0.35f;
    [Min(0f)] public float lowMediumClimbMaxHeight = 1f;
    [Min(0f)] public float mediumClimbMaxHeight = 1.7f;
    [Min(0f)] public float mediumHighClimbMinHeight = 2f;
    [Min(0f)] public float mediumHighClimbMaxHeight = 2.5f;

    [Header("State Transition")]
    [Min(0f)] public float stateSwitchCooldown = 0.1f;
}
