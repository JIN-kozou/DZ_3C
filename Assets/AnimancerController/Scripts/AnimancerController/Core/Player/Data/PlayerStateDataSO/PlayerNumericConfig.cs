using UnityEngine;

[System.Serializable]
public class PlayerNumericConfig
{
    [Header("Move")]
    [Min(0.1f)] public float moveSpeedMultiplier = 1f;
    public float walkSpeedParameter = 1f;
    public float runSpeedParameter = 2f;
    public float inAirMoveSpeed = 2f;

    [Header("Character Controller — Stance")]
    [Tooltip("站立时 CharacterController 半径。")]
    [Min(0.01f)] public float standControllerRadius = 0.5f;
    [Tooltip("站立时 CharacterController 高度。")]
    [Min(0.01f)] public float standControllerHeight = 2f;
    [Tooltip("站立时 CharacterController 中心（本地空间）。")]
    public Vector3 standControllerCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("下蹲时 CharacterController 半径。")]
    [Min(0.01f)] public float crouchControllerRadius = 0.5f;
    [Tooltip("下蹲时 CharacterController 高度。")]
    [Min(0.01f)] public float crouchControllerHeight = 1.2f;
    [Tooltip("下蹲时 CharacterController 中心（本地空间）。")]
    public Vector3 crouchControllerCenter = new Vector3(0f, 0.6f, 0f);

    [Header("Character — Ceiling Check")]
    [Tooltip("从当前 CharacterController 胶囊顶部向上的射线长度（米），用于：松蹲站起 / pending 净空站起 / 阻挡起跳 / 阻挡空中触发攀爬。命中 whatIsGround 视为有顶。≤0 关闭该组头顶检测。")]
    [Min(0f)] public float crouchStandCeilingCheckRayLength = 1.2f;
    [Tooltip("从胶囊顶部向上的射线长度（米），仅用于「地面头顶有障碍时自动下蹲」触发；净空站起仍由 crouchStandCeilingCheckRayLength 与 pending 逻辑处理。≤0 关闭自动下蹲触发。")]
    [Min(0f)] public float lowCeilingAutoCrouchCheckRayLength = 1.2f;

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

    [Header("Climb — Detection (空中前推输入时)")]
    [Tooltip("墙面法线与“朝墙前进方向”(-targetDir)的最大夹角（度）。越大越宽容斜墙/转角；过小会很难触发攀爬。")]
    [Min(0f)] public float climbDetectionAngle = 45f;
    [Tooltip("水平朝墙探测的最大距离（米）。用于 GetWallHight 里 Raycast/SphereCast 的 forward 距离。")]
    [Min(0f)] public float wallProbeDistance = 1f;
    [Tooltip("水平朝墙探测的球体半径（米）。用于 SphereCast 兜底，减少薄墙/边缘漏检。")]
    [Min(0f)] public float wallProbeRadius = 0.2f;
    [Tooltip("竖直扫描最高点（米，相对脚底）。与 canClimbMinHeight 一起决定空中攀爬扫描的竖直采样区间上界。")]
    [Min(0f)] public float canClimbMaxHeight = 3.2f;
    [Tooltip("竖直扫描最低点（米，相对脚底）。与 canClimbMaxHeight 一起决定竖直采样区间下界。")]
    [Min(0f)] public float canClimbMinHeight = 0.3f;
    [Tooltip("竖直方向采样段数。越大越精细但更耗；影响“翻越高度”估算的步进。")]
    [Min(1)] public int climbDetectionSamplingCount = 30;
    [Tooltip("连续多少帧检测命中后才进入攀爬，用于防抖（避免缝/角抖动误触发）。")]
    [Min(1)] public int climbConfirmFrames = 2;

    [Header("Climb — Obstacle Height Bands (相对脚底 hit 高度)")]
    [Tooltip("低于该高度差视为 low 档（对应 climbs[0] 与 climbSettings[0]）。")]
    [Min(0f)] public float lowClimbMaxHeight = 0.35f;
    [Tooltip("lowMedium 档上界（不含等于 medium 上界时的分界逻辑由代码区间决定）。")]
    [Min(0f)] public float lowMediumClimbMaxHeight = 1f;
    [Tooltip("medium 档上界。")]
    [Min(0f)] public float mediumClimbMaxHeight = 1.7f;
    [Tooltip("mediumHigh 排除区间下界：落在此高度范围内会强制不进入攀爬（保留跳跃）。")]
    [Min(0f)] public float mediumHighClimbMinHeight = 2f;
    [Tooltip("mediumHigh 排除区间上界。")]
    [Min(0f)] public float mediumHighClimbMaxHeight = 2.5f;

    [Header("State Transition")]
    [Min(0f)] public float stateSwitchCooldown = 0.1f;
}
