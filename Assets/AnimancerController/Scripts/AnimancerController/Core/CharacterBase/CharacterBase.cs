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
    [SerializeField, Range(0f, 89f)] private float maxWalkableSlopeAngle = 45f;
    [SerializeField, Range(0f, 89f)] private float slideStartAngle = 52f;
    [SerializeField] private float slideAcceleration = 16f;
    [SerializeField] private float slideMaxSpeed = 8f;
    [SerializeField] private float slideControlDamping = 4f;
    private Vector3 detectedOrigin;
    private Vector3 smoothedGroundNormal = Vector3.up;
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
        maxWalkableSlopeAngle = config.maxWalkableSlopeAngle;
        slideStartAngle = config.slideStartAngle;
        slideAcceleration = config.slideAcceleration;
        slideMaxSpeed = config.slideMaxSpeed;
        slideControlDamping = config.slideControlDamping;
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
        if (isHit && Physics.SphereCast(detectedOrigin, groundRadius * 0.8f, Vector3.down, out var hitInfo, groundProbeDistance, whatIsGround, QueryTriggerInteraction.Ignore))
        {
            Vector3 hitNormal = hitInfo.normal.sqrMagnitude > 0f ? hitInfo.normal.normalized : Vector3.up;
            float t = 1f / Mathf.Max(1, groundNormalSmoothFrames);
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, hitNormal, t);
            SlopeAngle = Vector3.Angle(smoothedGroundNormal, Vector3.up);
        }
        else
        {
            smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, Vector3.up, 0.25f);
            SlopeAngle = 0f;
        }
        isOnGround.Value = isHit && verticalSpeed < 0;
        IsOnSteepSlope = isOnGround.Value && SlopeAngle > slideStartAngle;
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
            controller.Move(deltaDir);
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
        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, groundProbeDistance + 0.2f))
        {
            if (Vector3.Dot(hitInfo.normal, Vector3.up) != 1)
            {
                return Vector3.ProjectOnPlane(dir, hitInfo.normal);
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
            return;
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, smoothedGroundNormal).normalized;
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