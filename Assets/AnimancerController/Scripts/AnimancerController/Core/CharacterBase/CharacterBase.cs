using System;
using UnityEngine;

[RequireComponent(typeof(Animator),typeof(CharacterController))]
public class CharacterBase : MonoBehaviour
{
    public CharacterController controller { get; private set; }
    public  Animator animator { get; private set; }
    //重力的配置
    [Header("重力设置")]
    [SerializeField] public float gravity = -12;
    [SerializeField] public Vector2 velocityLimit = new Vector2(-20, 60);
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] private float groundDetectedOffset = -0.06f;
    [SerializeField] private float groundRadius = 1.2f;
    [SerializeField] private float groundProbeDistance = 1.5f;
    [SerializeField] private float groundNormalRayStartHeight = 0.35f;
    [SerializeField] private float groundNormalRayDistance = 4f;
    [SerializeField, Range(0f, 89f)] private float maxWalkableSlopeAngle = 45f;
    [SerializeField, Range(0f, 89f)] private float slideStartAngle = 52f;
    [SerializeField] private float slideLeadPastWalkableDegrees = 2f;
    [SerializeField] private float slideAcceleration = 16f;
    [SerializeField] private float slideMaxSpeed = 8f;
    [SerializeField] private float slideControlDamping = 4f;
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.1f;
    private Vector3 detectedOrigin;
    private Vector3 smoothedGroundNormal = Vector3.up;
    private Vector3 lastProbeGroundNormal = Vector3.up;
    private int groundNormalSmoothFrames = 3;
    private Vector3 slopeSlideVelocity;
    public BindableProperty<bool> isOnGround { set; get; } = new BindableProperty<bool>();
    //角色垂直速度
    public float verticalSpeed { get; set; }
    private Vector3 verticalVelocity;
    //角色的水平速度:不包含动画位移
    private Vector3 horizontalVelocityInAir;
    private Vector3 animationVelocity;
    public Vector3 AnimationVelocity => animationVelocity;
    //角色的运动
    private Vector3 moveDir;
    public Vector3 animatorDeltaPositionOffset{ get; set; }
    public bool applyFullRootMotion { get; set; } = false;
    [SerializeField,Range(0.1f,10)] public float moveSpeedMult =1;
    public bool disEnableRootMotion { get; set; }//不采用任何根运动信息，禁用OnAnimatorMove方法
    public bool ignoreRootMotionY { get; set; } = false;//忽视根运动的Y量
    public bool disEnableGravity { get; set; } = false;//是否禁用程序重力
    public bool ignoreRotationRootMotion { get; set; } = false;//是否忽略根运动的转向
    public Vector3 GroundNormal => smoothedGroundNormal;
    public float SlopeAngle { get; private set; }
    public bool IsOnSteepSlope { get; private set; }
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// CharacterController 胶囊底部世界坐标 Y。攀爬/障碍高度等应相对“脚底”计算，而不是直接用 transform.position.y
    /// （改 center/height 后两者不再一致）。
    /// </summary>
    public float GetCapsuleFootWorldY()
    {
        if (controller == null)
        {
            return transform.position.y;
        }

        return transform.position.y + controller.center.y - controller.height * 0.5f;
    }

    public void ApplyNumericConfig(PlayerNumericConfig config)
    {
        if (config == null)
        {
            return;
        }

        gravity = config.gravity;
        velocityLimit = config.velocityLimit;
        groundDetectedOffset = config.groundDetectedOffset;
        groundRadius = config.groundRadius;
        groundProbeDistance = config.groundProbeDistance;
        groundNormalSmoothFrames = Mathf.Max(1, config.groundNormalSmoothFrames);
        groundNormalRayStartHeight = config.groundNormalRayStartHeight;
        groundNormalRayDistance = config.groundNormalRayDistance;
        maxWalkableSlopeAngle = config.maxWalkableSlopeAngle;
        slideStartAngle = config.slideStartAngle;
        slideLeadPastWalkableDegrees = config.slideLeadPastWalkableDegrees;
        slideAcceleration = config.slideAcceleration;
        slideMaxSpeed = config.slideMaxSpeed;
        slideControlDamping = config.slideControlDamping;
        minGroundNormalY = Mathf.Clamp01(config.minGroundNormalY);
        if (controller != null)
        {
            controller.slopeLimit = maxWalkableSlopeAngle;
        }
        moveSpeedMult = config.moveSpeedMultiplier;
    }
    protected virtual void Update()
    {
        CheckOnGround();
        CharacterGravity();
        CharacterVerticalVelocity();
        ResetHorizontalVelocity();
    }

  

    #region 重力的处理
    /// <summary>
    /// 地面检测
    /// </summary>
    private bool CheckOnGround()
    {
        detectedOrigin = transform.position - groundDetectedOffset * Vector3.up;
        var isHit = Physics.CheckSphere(detectedOrigin, groundRadius, whatIsGround, QueryTriggerInteraction.Ignore);
        Vector3 bestNormal = Vector3.up;
        bool haveGroundNormal = false;
        if (isHit && Physics.SphereCast(detectedOrigin, groundRadius * 0.8f, Vector3.down, out var hitInfo, groundProbeDistance, whatIsGround, QueryTriggerInteraction.Ignore))
        {
            bestNormal = hitInfo.normal.sqrMagnitude > 0f ? hitInfo.normal.normalized : Vector3.up;
            haveGroundNormal = true;
        }

        // 从脚底探测点附近起算，避免角色尚未踏上斜坡时，从身体中心垂直向下误打到前方陡坡/竖直碰撞体。
        Vector3 rayOrigin = detectedOrigin + Vector3.up * groundNormalRayStartHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out var rayHit, groundNormalRayDistance, whatIsGround, QueryTriggerInteraction.Ignore))
        {
            Vector3 rayNormal = rayHit.normal.sqrMagnitude > 0f ? rayHit.normal.normalized : Vector3.up;
            if (!haveGroundNormal || Vector3.Angle(rayNormal, Vector3.up) > Vector3.Angle(bestNormal, Vector3.up))
            {
                bestNormal = rayNormal;
            }
            haveGroundNormal = true;
        }

        if (haveGroundNormal)
        {
            lastProbeGroundNormal = bestNormal;
            float diff = Vector3.Angle(smoothedGroundNormal, bestNormal);
            float t = Mathf.Clamp01((1f / Mathf.Max(1, groundNormalSmoothFrames)) + diff / 180f * 0.35f);
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, bestNormal, t);
            SlopeAngle = Vector3.Angle(bestNormal, Vector3.up);
        }
        else
        {
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, Vector3.up, 0.25f);
            lastProbeGroundNormal = Vector3.Slerp(lastProbeGroundNormal, Vector3.up, 0.25f);
            SlopeAngle = 0f;
        }

        bool controllerGrounded = controller != null && controller.isGrounded;
        bool overlapGroundedWithValidNormal = isHit && haveGroundNormal && bestNormal.y >= minGroundNormalY;
        bool controllerGroundedWithValidNormal = controllerGrounded && (haveGroundNormal ? bestNormal.y >= minGroundNormalY : lastProbeGroundNormal.y >= minGroundNormalY);
        // 只允许“脚底接触到地面(Overlap)或CC明确判定接地”触发落地；远距离向下射线仅用于法线采样，不参与接地判定。
        isOnGround.Value = (overlapGroundedWithValidNormal || controllerGroundedWithValidNormal) && verticalSpeed <= 0.05f;
        float slideThreshold = Mathf.Min(slideStartAngle, maxWalkableSlopeAngle + Mathf.Max(0f, slideLeadPastWalkableDegrees));
        IsOnSteepSlope = isOnGround.Value && SlopeAngle > slideThreshold;
        return isOnGround.Value;
    }
    private void CharacterGravity()
    {
        if (disEnableGravity)
        {
            return;
        }
        if (isOnGround.Value)
        {
            verticalSpeed = -2;
            UpdateSlopeSlideVelocity();
        }
        else
        {
            verticalSpeed += Time.deltaTime * gravity;
            verticalSpeed = Mathf.Clamp(verticalSpeed, velocityLimit.x, velocityLimit.y);
            slopeSlideVelocity = Vector3.Lerp(slopeSlideVelocity, Vector3.zero, 1 - Mathf.Exp(-slideControlDamping * Time.deltaTime));
        }
        verticalVelocity = new Vector3(0, verticalSpeed, 0);
    }

    #endregion

    #region 玩家移动
    private void ResetHorizontalVelocity()
    {
        if (isOnGround.Value)
        {
            if (horizontalVelocityInAir!= Vector3.zero)
            {
                horizontalVelocityInAir = Vector3.zero;
            }
        }
    }
    //程序上控制重力速度和水平速度
    private void CharacterVerticalVelocity()
    {
        if (disEnableGravity)
        {
            verticalVelocity = Vector3.zero;
        }
        if (controller.enabled)
        {
            controller.Move((verticalVelocity + horizontalVelocityInAir + slopeSlideVelocity) * Time.deltaTime);
        }

    }
    protected virtual void OnAnimatorMove()//在播放动画时调用次方法,没有动画不会执行
    {
        if (disEnableRootMotion)
        {
            return;
        }

        if (applyFullRootMotion) //开启角色的根运动，重力默认为角色自带的向下的动画位移量
        {
            animator.ApplyBuiltinRootMotion();
        }
        else//不启用根运动，但是采样的也是角色根运动信息(位移)
        {
            Vector3 animationMovement = animator.deltaPosition+ animatorDeltaPositionOffset;
            if (ignoreRootMotionY)
            {
                animationMovement.y = 0;
            }
            moveDir = SetDirOnSlop(animationMovement) * moveSpeedMult;
            UpdateCharacterMove(moveDir,animator.deltaRotation);
        }
    }
    public void UpdateCharacterMove(Vector3 deltaDir,Quaternion deltaRotation)
    {
        if (!ignoreRotationRootMotion)
        {
            if (deltaRotation != Quaternion.identity)
            {
                transform.rotation = deltaRotation * transform.rotation;
            }
        }
        //每帧移动Dir个单位
        if (controller.enabled == true)
        {
            animationVelocity = deltaDir;
            Vector3 stableMove = deltaDir;
            // 轻微贴地，减少地面接缝/台阶边缘处的瞬时离地导致的卡脚。
            if (isOnGround.Value && !disEnableGravity && verticalSpeed <= 0.05f)
            {
                stableMove += Vector3.down * 0.03f;
            }
            controller.Move(stableMove);
        }
      
    }
    public float ChangeVerticalSpeed(float verticalSpeed)
    {
        return this.verticalSpeed = verticalSpeed;
    }
    public void AddHorizontalVelocityInAir(Vector3 vector3)
    {
        horizontalVelocityInAir = new Vector3(vector3.x,0, vector3.z);
    }
    public void ClearHorizontalVelocity()
    {
        horizontalVelocityInAir = Vector3.zero;
    }

    #endregion

    #region 斜坡的处理
    private Vector3 SetDirOnSlop(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.000001f)
        {
            return dir;
        }

        // 优先使用地面检测阶段平滑后的法线，降低接缝处法线跳变导致的卡顿。
        if (isOnGround.Value && smoothedGroundNormal.sqrMagnitude > 0.0001f)
        {
            float slopeAngle = Vector3.Angle(smoothedGroundNormal, Vector3.up);
            if (slopeAngle > 0.1f && slopeAngle <= maxWalkableSlopeAngle + 2f)
            {
                Vector3 projected = Vector3.ProjectOnPlane(dir, smoothedGroundNormal);
                if (projected.sqrMagnitude > 0.000001f)
                {
                    return projected;
                }
            }
        }

        // 兜底：脚底附近做球射线而不是角色中心单射线，提升不平整地形稳定性。
        float probeRadius = controller != null ? Mathf.Max(0.05f, controller.radius * 0.35f) : 0.1f;
        Vector3 probeOrigin = detectedOrigin + Vector3.up * 0.2f;
        if (Physics.SphereCast(probeOrigin, probeRadius, Vector3.down, out var hitInfo, 0.9f, whatIsGround, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Dot(hitInfo.normal, Vector3.up) < 0.9999f)
            {
                Vector3 projected = Vector3.ProjectOnPlane(dir, hitInfo.normal);
                if (projected.sqrMagnitude > 0.000001f)
                {
                    return projected;
                }
            }
        }

        return dir;
    }
    #endregion

    private void UpdateSlopeSlideVelocity()
    {
        if (!IsOnSteepSlope)
        {
            slopeSlideVelocity = Vector3.Lerp(slopeSlideVelocity, Vector3.zero, 1 - Mathf.Exp(-slideControlDamping * Time.deltaTime));
            slopeSlideVelocity.y = 0f;
            return;
        }

        Vector3 slideNormal = lastProbeGroundNormal.sqrMagnitude > 0.0001f ? lastProbeGroundNormal.normalized : smoothedGroundNormal.normalized;
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slideNormal).normalized;
        if (slideDirection.sqrMagnitude <= 0.0001f)
        {
            slopeSlideVelocity = Vector3.zero;
            return;
        }

        slopeSlideVelocity += slideDirection * slideAcceleration * Time.deltaTime;
        float maxSlideSpeed = Mathf.Max(0.01f, slideMaxSpeed);
        if (slopeSlideVelocity.magnitude > maxSlideSpeed)
        {
            slopeSlideVelocity = slopeSlideVelocity.normalized * maxSlideSpeed;
        }
        slopeSlideVelocity.y = 0f;
    }

    private void OnDrawGizmos()
    {
        if (CheckOnGround())
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawWireSphere(transform.position - groundDetectedOffset * Vector3.up, groundRadius);
    }
}