
using UnityEngine;
using UnityEngine.InputSystem;
/**************************************************************************
????: HuHu
????: 3112891874@qq.com
????: ????????????????InputSystem
**************************************************************************/
public class InputService : MonoSingleton<InputService>
{
    public InputMap inputMap;

    Vector2 _moveSmoothed;
    Vector2 _moveSmoothVelocity;
    bool _moveSmoothPrimed;

    protected override void Awake()
    {
        base.Awake();
        if (inputMap == null)
        {
            inputMap = new InputMap();
        }
        inputMap.Enable();
        _moveSmoothed = Vector2.zero;
        _moveSmoothVelocity = Vector2.zero;
        _moveSmoothPrimed = false;
    }
    private void OnDestroy()
    {
        inputMap.Disable();
    }

    /// <summary>
    /// ???????????PC????????????????
    /// </summary>
    public Vector2 GetMoveHorizontalValue
    {
        get
        {
            //???
#if UNITY_ANDROID
                return inputMap.Player.Move.ReadValue<Vector2>();
            //????
#elif !UNITY_ANDROID
            Vector2 dir = inputMap.Player.Move.ReadValue<Vector2>();
            bool isShift = inputMap.Player.Shift.ReadValue<float>()!=0;

            if (dir != Vector2.zero && isShift)
            {
                dir.y = 0;
                return dir.normalized;
            }
            else if (dir != Vector2.zero && !isShift)
            {
                dir.y = 0;
                return dir.normalized;
            }
            else
            {
                return Vector2.zero;
            }
#else
                return 0f; // ????
#endif
        }
    }
    public Vector2 GetMoveVerticalValue
    {
        get
        {
            Vector2 dir = inputMap.Player.Move.ReadValue<Vector2>();

            if (dir != Vector2.zero)
            {
                dir.x = 0;
                return dir.normalized;
            }
            else
            {
                return Vector2.zero;
            }
        }
    }

    public bool Interactive => inputMap.Player.Interactive.ReadValue<float>()!= 0;

    public bool Shift
    {
       get
        {
           return inputMap.Player.Shift.ReadValue<float>() != 0;
        }
    }

    /// <summary>
    /// 当前帧离散移动意图（-1/0/1），用于状态机门控、松键判定等，无平滑记忆。
    /// </summary>
    public Vector2 MoveDiscrete
    {
        get
        {
            if (inputMap == null)
            {
                return Vector2.zero;
            }

            return QuantizeMove(inputMap.Player.Move.ReadValue<Vector2>());
        }
    }

    /// <summary>
    /// 平滑后的移动向量（键鼠摇杆模拟）；用于朝向、锁敌混合、空中输入等连续量。
    /// 在 <see cref="TickMoveSmoothing"/> 之前读取时，与 <see cref="MoveDiscrete"/> 相同。
    /// </summary>
    public Vector2 Move => _moveSmoothPrimed ? _moveSmoothed : MoveDiscrete;

    static Vector2 QuantizeMove(Vector2 vector2)
    {
        if (vector2.x > 0)
        {
            vector2.x = 1;
        }
        else if (vector2.x < 0)
        {
            vector2.x = -1;
        }
        else
        {
            vector2.x = 0;
        }

        if (vector2.y > 0)
        {
            vector2.y = 1;
        }
        else if (vector2.y < 0)
        {
            vector2.y = -1;
        }
        else
        {
            vector2.y = 0;
        }

        return vector2;
    }

    /// <summary>
    /// 每帧由 <see cref="Player"/> 在状态机更新前调用，刷新 <see cref="Move"/>。
    /// </summary>
    public void TickMoveSmoothing(float deltaTime, float smoothTime, bool applyKeyboardSmoothing)
    {
        if (inputMap == null)
        {
            return;
        }

        Vector2 discrete = QuantizeMove(inputMap.Player.Move.ReadValue<Vector2>());
        if (!_moveSmoothPrimed)
        {
            _moveSmoothed = discrete;
            _moveSmoothVelocity = Vector2.zero;
            _moveSmoothPrimed = true;
            return;
        }

        bool fromKeyboard = inputMap.Player.Move.activeControl?.device is Keyboard;
        if (applyKeyboardSmoothing && fromKeyboard && smoothTime > 0.0001f)
        {
            _moveSmoothed = Vector2.SmoothDamp(
                _moveSmoothed,
                discrete,
                ref _moveSmoothVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);
        }
        else
        {
            _moveSmoothed = discrete;
            _moveSmoothVelocity = Vector2.zero;
        }
    }
    public Vector2 Scroll =>inputMap.Player.Scroll.ReadValue<Vector2>();

    public bool FireHeld => inputMap != null && inputMap.Player.Fire.ReadValue<float>() > 0f;

    public bool FireWasPressedThisFrame =>
        inputMap != null && inputMap.Player.Fire.WasPressedThisFrame();

    public bool ADSHeld => inputMap != null && inputMap.Player.ADS.ReadValue<float>() > 0f;

    public bool CrouchHeld => inputMap != null && inputMap.Player.Crouch.ReadValue<float>() > 0f;

    public bool ToggleWeaponWasPressedThisFrame =>
        inputMap != null && inputMap.Player.ToggleWeapon.WasPressedThisFrame();

    public bool HolsterWeaponWasPressedThisFrame =>
        inputMap != null && inputMap.Player.HolsterWeapon.WasPressedThisFrame();

}