using UnityEngine;

[System.Serializable]
public class PlayerNumericConfig
{
    [Header("Move")]
    [Min(0.1f)] public float moveSpeedMultiplier = 1f;
    public float walkSpeedParameter = 1f;
    public float runSpeedParameter = 2f;
    public float inAirMoveSpeed = 2f;
    [Tooltip("键鼠下对离散 WASD 做 SmoothDamp，使 Move 向量接近摇杆手感；状态机门控仍用 MoveDiscrete。")]
    public bool keyboardMoveInputSmoothing = true;
    [Tooltip("键鼠移动输入平滑时间（秒）；仅当 keyboardMoveInputSmoothing 且当前 Move 来自键盘时生效。")]
    [Min(0.0001f)] public float keyboardMoveSmoothTime = 0.12f;

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

    [Header("Character Controller — In air")]
    [Tooltip("离地未接地时（起跳上升、下落、滑落等整段空中）CharacterController 使用的半径；接地后仍按站立/下蹲插值。")]
    [Min(0.01f)] public float fallControllerRadius = 0.5f;
    [Tooltip("离地未接地时 CharacterController 高度。")]
    [Min(0.01f)] public float fallControllerHeight = 2f;
    [Tooltip("离地未接地时 CharacterController 中心（本地空间）。")]
    public Vector3 fallControllerCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("勾选为长按保持下蹲，松键站起；不勾选为按下在蹲/站之间切换（与多数 FPS 一致）。")]
    public bool useHoldForCrouch = false;

    [Header("Character — Ceiling Check")]
    [Tooltip("从当前 CharacterController 胶囊顶部向上的射线长度（米），用于：松蹲站起 / pending 净空站起 / 阻挡起跳 / 阻挡空中触发攀爬。命中 whatIsGround 视为有顶。≤0 关闭该组头顶检测。")]
    [Min(0f)] public float crouchStandCeilingCheckRayLength = 1.2f;
    [Tooltip("从胶囊顶部向上的射线长度（米），仅用于「地面头顶有障碍时自动下蹲」触发；净空站起仍由 crouchStandCeilingCheckRayLength 与 pending 逻辑处理。≤0 关闭自动下蹲触发。")]
    [Min(0f)] public float lowCeilingAutoCrouchCheckRayLength = 1.2f;
    [Tooltip("从「配置中站立胶囊顶」沿世界向上射线长度（米），仅在 pending 净空站起时：若站起后仍会命中该射线，则暂不站起，避免站起瞬间再被自动下蹲拉回导致抽搐。≤0 关闭该预测（仅按站起射线净空）。")]
    [Min(0f)] public float lowCeilingAutoCrouchPostStandPredictRayLength = 1.2f;

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

    [Header("Ledge Walk-Off")]
    [Tooltip("开启后，在地面移动态朝输入方向前探；前方无连续地面或落差可走下时强制离地，避免台缘卡住。")]
    public bool ledgeWalkOffEnabled = true;
    [Tooltip("从脚底平面中心沿移动方向前移的距离（米），用于台缘前探点。")]
    [Min(0f)] public float ledgeProbeForwardDistance = 0.35f;
    [Tooltip("前方地面比脚底低多少以内仍视为可走下（米）；建议不小于 CharacterController.stepOffset。")]
    [Min(0f)] public float ledgeMaxWalkDownHeight = 0.4f;
    [Tooltip("前探点向下射线最大长度（米）。")]
    [Min(0.1f)] public float ledgeProbeDownDistance = 2f;
    [Tooltip("前方地面与脚底高度差小于此值（米）视为仍连着，不强制离地。")]
    [Min(0f)] public float ledgeSameHeightTolerance = 0.08f;
    [Tooltip("为 true 时必须有移动输入（MoveDiscrete）才做台缘检测，避免站边缘误落。")]
    public bool ledgeRequireMoveInput = true;

    [Header("Jump")]
    [Tooltip("台边 Coyote：离地后仍可起跳的秒数；0 表示离地立即进下落（不再使用原 50ms 定时器）。")]
    [Min(0f)] public float coyoteJumpTimeSeconds = 0.12f;
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

    [Header("Weapon / ADS (动画)")]
    [Tooltip("开镜进入动画播放速度倍率；越大开镜动画越快（乘在持枪上半身 adsEnter 片段的播放速度上）。")]
    [Min(0.05f)] public float adsEnterAnimationSpeedScale = 1f;

    [Header("Weapon / Locomotion speed")]
    [Tooltip("持枪稳态（掏枪结束、收枪开始前）相对空手 walk/run 的速度系数；1 表示不降速，0.8 表示降低 20%。")]
    [Min(0.01f)] public float armedLocomotionSpeedMultiplier = 0.8f;
    [Tooltip("开镜稳态在持枪稳态速度上再乘的系数（与 armed 连乘）；1 表示开镜不再额外降速。")]
    [Min(0.01f)] public float adsLocomotionSpeedMultiplier = 0.8f;
}
