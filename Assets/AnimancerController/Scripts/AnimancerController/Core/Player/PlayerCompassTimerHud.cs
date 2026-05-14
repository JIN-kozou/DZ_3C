using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 顶栏：水平罗盘（随相机 yaw 滑动）与 MM:SS 计时。刻度在 <see cref="compassContent"/> 无子物体时于运行时生成。
/// </summary>
[DisallowMultipleComponent]
public class PlayerCompassTimerHud : MonoBehaviour
{
    public enum TimerDisplayMode
    {
        CountUpSinceEnabled,
        CountDownFrom
    }

    [Header("References")]
    [SerializeField, Tooltip("留空则在运行时查找场景中的 Player。")]
    private Player player;

    [SerializeField, Tooltip("留空则使用 Player.camTransform，再退回 Camera.main。")]
    private Transform yawSource;

    [SerializeField, Tooltip("裁切罗盘条带的视口（会确保存在 RectMask2D）。")]
    private RectTransform compassViewport;

    [SerializeField, Tooltip("刻度与方位字的父节点。")]
    private RectTransform compassContent;

    [SerializeField, Tooltip("整块顶栏背景（可选）。")]
    private Image panelBackground;

    [SerializeField, Tooltip("视口中央的朝向指示线（可选）。")]
    private RectTransform centerMarker;

    [SerializeField]
    private Text timerText;

    [Header("Compass")]
    [SerializeField, Min(0.5f), Tooltip("每度水平角对应的水平像素位移。")]
    private float pixelsPerDegree = 3f;

    [SerializeField, Tooltip("叠加到水平角后再显示，用于把「北」对齐到关卡世界方向。")]
    private float northWorldYawOffset;

    [SerializeField, Min(1), Tooltip("次要刻度间隔（度）。")]
    private int minorTickDegrees = 5;

    [SerializeField, Min(1f)] private float minorTickHeight = 10f;

    [SerializeField, Min(1f)] private float majorTickHeight = 18f;

    [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.92f);

    [SerializeField] private Color labelColor = new Color(1f, 1f, 1f, 0.95f);

    [SerializeField, Min(8)] private int labelFontSize = 15;

    [Header("Timer")]
    [SerializeField] private TimerDisplayMode timerMode = TimerDisplayMode.CountUpSinceEnabled;

    [SerializeField, Tooltip("为 true 时用 Time.unscaledTime（暂停菜单时仍走表）。")]
    private bool useUnscaledTime = true;

    [SerializeField, Min(0f), Tooltip("CountDownFrom 模式下的起始秒数。")]
    private float countdownStartSeconds = 300f;

    [SerializeField] private UnityEvent onCountdownReachZero = new UnityEvent();

    private float _clockAtEnable;
    private bool _countdownEndedEventFired;
    private static readonly string[] CardinalLabels =
    {
        "北", "东北", "东", "东南", "南", "西南", "西", "西北"
    };

    private readonly StringBuilder _sb = new StringBuilder(12);

    private void Awake()
    {
        EnsureViewportMask();
        ResolvePlayer();
        BuildCompassIfNeeded();
        if (panelBackground != null)
        {
            panelBackground.raycastTarget = false;
        }

        if (centerMarker != null)
        {
            var cm = centerMarker.GetComponent<Image>();
            if (cm != null)
            {
                cm.raycastTarget = false;
            }
        }
    }

    private void OnEnable()
    {
        _clockAtEnable = GetClock();
        _countdownEndedEventFired = false;
    }

    private void LateUpdate()
    {
        ResolvePlayer();
        UpdateCompass();
    }

    private void Update()
    {
        UpdateTimerText();
    }

    /// <summary>重新开始倒计时（从 <see cref="countdownStartSeconds"/>）或重置累加起点。</summary>
    public void ResetTimer()
    {
        _clockAtEnable = GetClock();
        _countdownEndedEventFired = false;
    }

    /// <summary>将倒计时剩余秒数设为指定值（仅影响 CountDownFrom）。</summary>
    public void SetCountdownRemainingSeconds(float seconds)
    {
        countdownStartSeconds = Mathf.Max(0f, seconds);
        _clockAtEnable = GetClock();
        _countdownEndedEventFired = false;
    }

    private void EnsureViewportMask()
    {
        if (compassViewport == null)
        {
            return;
        }

        if (compassViewport.GetComponent<RectMask2D>() == null)
        {
            compassViewport.gameObject.AddComponent<RectMask2D>();
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = GetComponentInParent<Player>();
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private Transform ResolveYawTransform()
    {
        if (yawSource != null)
        {
            return yawSource;
        }

        if (player != null && player.camTransform != null)
        {
            return player.camTransform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private static float Normalize360(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
        {
            degrees += 360f;
        }

        return degrees;
    }

    private static float HorizontalYawDegrees(Transform t)
    {
        var f = t.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 1e-6f)
        {
            return 0f;
        }

        f.Normalize();
        return Normalize360(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
    }

    private void UpdateCompass()
    {
        if (compassContent == null)
        {
            return;
        }

        var src = ResolveYawTransform();
        if (src == null)
        {
            return;
        }

        var yaw = Normalize360(HorizontalYawDegrees(src) + northWorldYawOffset);
        var bandWidth = 360f * pixelsPerDegree;
        compassContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bandWidth);
        compassContent.anchoredPosition = new Vector2((180f - yaw) * pixelsPerDegree, compassContent.anchoredPosition.y);
    }

    private void BuildCompassIfNeeded()
    {
        if (compassContent == null || compassContent.childCount > 0)
        {
            return;
        }

        var ppd = pixelsPerDegree;
        var halfBand = 180f * ppd;

        for (var a = 0; a < 360; a += minorTickDegrees)
        {
            var isCardinal = a % 45 == 0;
            var tickGo = new GameObject(isCardinal ? $"Major_{a}" : $"Minor_{a}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tickGo.transform.SetParent(compassContent, false);
            var rt = tickGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            var x = (a - 180f) * ppd;
            rt.anchoredPosition = new Vector2(x, 0f);
            var h = isCardinal ? majorTickHeight : minorTickHeight;
            rt.sizeDelta = new Vector2(isCardinal ? 2f : 1f, h);

            var img = tickGo.GetComponent<Image>();
            img.sprite = null;
            img.color = tickColor;
            img.raycastTarget = false;

            if (isCardinal)
            {
                var labelIdx = a / 45;
                if (labelIdx >= 0 && labelIdx < CardinalLabels.Length)
                {
                    var textGo = new GameObject($"Label_{a}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    textGo.transform.SetParent(compassContent, false);
                    var trt = textGo.GetComponent<RectTransform>();
                    trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
                    trt.pivot = new Vector2(0.5f, 0f);
                    trt.anchoredPosition = new Vector2(x, h + 2f);
                    trt.sizeDelta = new Vector2(56f, 28f);
                    var tx = textGo.GetComponent<Text>();
                    tx.text = CardinalLabels[labelIdx];
                    tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (tx.font == null)
                    {
                        tx.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }

                    tx.fontSize = labelFontSize;
                    tx.color = labelColor;
                    tx.alignment = TextAnchor.LowerCenter;
                    tx.horizontalOverflow = HorizontalWrapMode.Overflow;
                    tx.verticalOverflow = VerticalWrapMode.Overflow;
                    tx.raycastTarget = false;
                    tx.supportRichText = false;
                }
            }
        }

        // 占位：保证首次布局前即有宽度
        compassContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, halfBand * 2f);
    }

    private float GetClock() => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        var now = GetClock();
        float seconds;
        if (timerMode == TimerDisplayMode.CountUpSinceEnabled)
        {
            seconds = Mathf.Max(0f, now - _clockAtEnable);
        }
        else
        {
            seconds = countdownStartSeconds - (now - _clockAtEnable);
            if (seconds <= 0f)
            {
                seconds = 0f;
                if (!_countdownEndedEventFired)
                {
                    _countdownEndedEventFired = true;
                    onCountdownReachZero?.Invoke();
                }
            }
        }

        var total = Mathf.FloorToInt(seconds + 1e-4f);
        var m = total / 60;
        var s = total % 60;
        _sb.Clear();
        if (m < 10)
        {
            _sb.Append('0');
        }

        _sb.Append(m);
        _sb.Append(':');
        if (s < 10)
        {
            _sb.Append('0');
        }

        _sb.Append(s);
        timerText.text = _sb.ToString();
    }
}
