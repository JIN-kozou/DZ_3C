using System;
using System.Collections.Generic;
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
    /// <summary>罗盘上指示的「场景物品」：相对玩家（或相机锚点）的水平方位。</summary>
    [Serializable]
    public class CompassWorldTargetEntry
    {
        [Tooltip("要指示的世界物体 Transform；可空则该项不显示。")]
        public Transform worldTransform;

        [Tooltip("留空则使用 HUD 上的默认图标或内置白块。")]
        public Sprite icon;

        [Tooltip("与图标相乘的颜色。")]
        public Color tint = Color.white;

        [Tooltip("图标方形边长（画布像素）；≤0 时使用 TopHudBar 上的全局 worldTargetIconSize。")]
        [Min(0f)]
        public float iconSize;

        [Tooltip("是否参与绘制。")]
        public bool show = true;
    }

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

    [SerializeField, Tooltip("物品图标的父节点；留空则在运行时挂在 CompassViewport 下（在刻度条之上、中线之下）。")]
    private RectTransform worldTargetMarkerLayer;

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

    [Header("Compass world targets")]
    [SerializeField, Tooltip("在罗盘刻度下方显示的物品方位图标（相对玩家水平角）。")]
    private List<CompassWorldTargetEntry> worldTargetMarkers = new List<CompassWorldTargetEntry>();

    [SerializeField, Min(4f), Tooltip("图标方形边长（画布像素）。")]
    private float worldTargetIconSize = 20f;

    [SerializeField, Tooltip("图标锚在视口底边，向上偏移的像素（应在刻度高度之下/贴底条区域）。")]
    private float worldTargetIconAnchoredY = 6f;

    [SerializeField, Min(0f), Tooltip("相对视口左右边界的留白，用于夹紧越界目标。")]
    private float worldTargetEdgeMarginPx = 6f;

    [SerializeField, Tooltip("为 true 时方位相对相机水平前向（与罗盘中心一致）；为 false 时相对玩家 Transform 的水平前向。")]
    private bool worldTargetsRelativeToCameraForward = true;

    [SerializeField, Range(0.2f, 1f), Tooltip("目标在身后（与参考前向点积为负）时的图标透明度乘数。")]
    private float worldTargetBehindAlphaScale = 0.45f;

    [Header("Timer")]
    [SerializeField] private TimerDisplayMode timerMode = TimerDisplayMode.CountUpSinceEnabled;

    [SerializeField, Tooltip("为 true 时用 Time.unscaledTime（暂停菜单时仍走表）。")]
    private bool useUnscaledTime = true;

    [SerializeField, Min(0f), Tooltip("CountDownFrom 模式下的起始秒数。")]
    private float countdownStartSeconds = 300f;

    [SerializeField] private UnityEvent onCountdownReachZero = new UnityEvent();

    private float _clockAtEnable;
    private bool _countdownEndedEventFired;

    /// <summary>上一帧归一化水平角，用于 <see cref="Mathf.DeltaAngle"/> 无缝过北。</summary>
    private float _lastYawNorm;

    /// <summary>罗盘条水平偏移（像素），由角度增量累加，周期性折叠到约 ±180° 对应范围。</summary>
    private float _compassOffsetPx;

    private bool _compassAngleInitialized;

    private readonly List<Image> _worldTargetMarkerPool = new List<Image>();

    private static Sprite _runtimeWhiteSprite;

    private static readonly string[] CardinalLabels =
    {
       //"黑洞", " ", " ", " ", "飞船", " ", " ", " "
    };

    private readonly StringBuilder _sb = new StringBuilder(12);

    private void Awake()
    {
        EnsureViewportMask();
        ResolvePlayer();
        BuildCompassIfNeeded();
        EnsureWorldTargetMarkerLayer();
        EnsureWorldTargetMarkerPoolSize();
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
        _compassAngleInitialized = false;
    }

    private void LateUpdate()
    {
        ResolvePlayer();
        UpdateCompass();
        UpdateWorldTargetMarkers();
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

    /// <summary>运行时替换指定索引的世界目标（索引越界则忽略）。</summary>
    public void SetWorldTargetTransform(int index, Transform target)
    {
        if (index < 0 || index >= worldTargetMarkers.Count)
        {
            return;
        }

        worldTargetMarkers[index].worldTransform = target;
    }

    /// <summary>运行时设置目标列表长度（仅增删末尾槽位）。</summary>
    public void SetWorldTargetMarkerCount(int count)
    {
        count = Mathf.Max(0, count);
        while (worldTargetMarkers.Count < count)
        {
            worldTargetMarkers.Add(new CompassWorldTargetEntry());
        }

        while (worldTargetMarkers.Count > count)
        {
            worldTargetMarkers.RemoveAt(worldTargetMarkers.Count - 1);
        }

        EnsureWorldTargetMarkerPoolSize();
    }

    /// <summary>世界目标标记数量（<see cref="worldTargetMarkers"/> 列表长度）。</summary>
    public int WorldTargetMarkerCount => worldTargetMarkers.Count;

    /// <summary>外部调用：设置指定索引目标图标的显示颜色（写入 entry.tint，下一帧绘制生效）。</summary>
    public void SetWorldTargetTint(int index, Color color)
    {
        if (!TryGetWorldTargetEntry(index, out var entry))
        {
            return;
        }

        entry.tint = color;
        RefreshWorldTargetMarkerVisual(index);
    }

    /// <summary>与 <see cref="SetWorldTargetTint"/> 相同，便于外部按「改颜色」语义调用。</summary>
    public void SetWorldTargetColor(int index, Color color) => SetWorldTargetTint(index, color);

    /// <summary>外部调用：设置指定索引目标图标的方形边长（画布像素）。</summary>
    public void SetWorldTargetIconSize(int index, float sizePixels)
    {
        if (!TryGetWorldTargetEntry(index, out var entry))
        {
            return;
        }

        entry.iconSize = Mathf.Max(0f, sizePixels);
        RefreshWorldTargetMarkerVisual(index);
    }

    /// <summary>外部调用：设置指定索引目标是否显示。</summary>
    public void SetWorldTargetVisible(int index, bool visible)
    {
        if (!TryGetWorldTargetEntry(index, out var entry))
        {
            return;
        }

        entry.show = visible;
        RefreshWorldTargetMarkerVisual(index);
    }

    private bool TryGetWorldTargetEntry(int index, out CompassWorldTargetEntry entry)
    {
        entry = null;
        if (index < 0 || index >= worldTargetMarkers.Count)
        {
            return false;
        }

        entry = worldTargetMarkers[index];
        return entry != null;
    }

    private float ResolveWorldTargetIconSize(CompassWorldTargetEntry entry)
    {
        if (entry == null || entry.iconSize <= 0f)
        {
            return worldTargetIconSize;
        }

        return entry.iconSize;
    }

    private void RefreshWorldTargetMarkerVisual(int index)
    {
        if (index < 0 || index >= _worldTargetMarkerPool.Count)
        {
            return;
        }

        var img = _worldTargetMarkerPool[index];
        if (img == null)
        {
            return;
        }

        var entry = index < worldTargetMarkers.Count ? worldTargetMarkers[index] : null;
        float size = ResolveWorldTargetIconSize(entry);
        img.rectTransform.sizeDelta = new Vector2(size, size);
        if (entry != null)
        {
            img.color = entry.tint;
        }
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
        var band = 360f * pixelsPerDegree;
        var halfBand = 180f * pixelsPerDegree;

        if (!_compassAngleInitialized)
        {
            _lastYawNorm = yaw;
            _compassOffsetPx = (180f - yaw) * pixelsPerDegree;
            _compassAngleInitialized = true;
        }
        else
        {
            var delta = Mathf.DeltaAngle(_lastYawNorm, yaw);
            _lastYawNorm = yaw;
            _compassOffsetPx -= delta * pixelsPerDegree;
            while (_compassOffsetPx > halfBand)
            {
                _compassOffsetPx -= band;
            }

            while (_compassOffsetPx < -halfBand)
            {
                _compassOffsetPx += band;
            }
        }

        compassContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, band * 3f);
        compassContent.anchoredPosition = new Vector2(_compassOffsetPx, compassContent.anchoredPosition.y);
    }

    private void BuildCompassIfNeeded()
    {
        if (compassContent == null)
        {
            return;
        }

        if (compassContent.childCount > 0 && compassContent.Find("Major_-1_0") == null)
        {
            for (var i = compassContent.childCount - 1; i >= 0; i--)
            {
                Destroy(compassContent.GetChild(i).gameObject);
            }
        }

        if (compassContent.childCount > 0)
        {
            return;
        }

        var ppd = pixelsPerDegree;
        var band = 360f * ppd;

        for (var k = -1; k <= 1; k++)
        {
            var kOffset = k * band;
            for (var a = 0; a < 360; a += minorTickDegrees)
            {
                var isCardinal = a % 45 == 0;
                var tickGo = new GameObject(
                    isCardinal ? $"Major_{k}_{a}" : $"Minor_{k}_{a}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                tickGo.transform.SetParent(compassContent, false);
                var rt = tickGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                var x = (a - 180f) * ppd + kOffset;
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
                        var textGo = new GameObject($"Label_{k}_{a}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
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
        }

        compassContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, band * 3f);
    }

    private void EnsureWorldTargetMarkerLayer()
    {
        if (worldTargetMarkerLayer != null || compassViewport == null)
        {
            return;
        }

        var go = new GameObject("WorldTargetMarkers", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(compassViewport, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        worldTargetMarkerLayer = rt;

        if (compassContent != null)
        {
            rt.SetSiblingIndex(compassContent.GetSiblingIndex() + 1);
        }

        if (centerMarker != null)
        {
            centerMarker.SetAsLastSibling();
        }
    }

    private void EnsureWorldTargetMarkerPoolSize()
    {
        EnsureWorldTargetMarkerLayer();
        if (worldTargetMarkerLayer == null)
        {
            return;
        }

        while (_worldTargetMarkerPool.Count < worldTargetMarkers.Count)
        {
            var idx = _worldTargetMarkerPool.Count;
            var go = new GameObject($"WorldTargetIcon_{idx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(worldTargetMarkerLayer, false);
            var irt = go.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0f);
            irt.pivot = new Vector2(0.5f, 0f);
            var entryForSize = idx < worldTargetMarkers.Count ? worldTargetMarkers[idx] : null;
            float size = ResolveWorldTargetIconSize(entryForSize);
            irt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            _worldTargetMarkerPool.Add(img);
        }

        for (var i = 0; i < _worldTargetMarkerPool.Count; i++)
        {
            var img = _worldTargetMarkerPool[i];
            if (img == null)
            {
                continue;
            }

            var rt = img.rectTransform;
            var entryForSize = i < worldTargetMarkers.Count ? worldTargetMarkers[i] : null;
            float size = ResolveWorldTargetIconSize(entryForSize);
            rt.sizeDelta = new Vector2(size, size);
        }
    }

    private static Sprite GetRuntimeWhiteSprite()
    {
        if (_runtimeWhiteSprite != null)
        {
            return _runtimeWhiteSprite;
        }

        var tex = Texture2D.whiteTexture;
        _runtimeWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _runtimeWhiteSprite;
    }

    private void UpdateWorldTargetMarkers()
    {
        EnsureWorldTargetMarkerLayer();
        EnsureWorldTargetMarkerPoolSize();

        if (worldTargetMarkerLayer == null)
        {
            return;
        }

        var yawT = ResolveYawTransform();
        if (yawT == null)
        {
            HideAllWorldTargetMarkers();
            return;
        }

        var origin = ResolveWorldOriginPosition();
        var flatRef = ResolveFlatReferenceForward(yawT);
        if (flatRef.sqrMagnitude < 1e-6f)
        {
            HideAllWorldTargetMarkers();
            return;
        }

        flatRef.Normalize();

        var halfW = worldTargetMarkerLayer.rect.width * 0.5f - worldTargetEdgeMarginPx;
        if (halfW < 1f)
        {
            halfW = 1f;
        }

        for (var i = 0; i < _worldTargetMarkerPool.Count; i++)
        {
            var img = _worldTargetMarkerPool[i];
            if (img == null)
            {
                continue;
            }

            if (i >= worldTargetMarkers.Count)
            {
                img.gameObject.SetActive(false);
                continue;
            }

            var entry = worldTargetMarkers[i];
            if (!entry.show || entry.worldTransform == null)
            {
                img.gameObject.SetActive(false);
                continue;
            }

            var toT = entry.worldTransform.position - origin;
            toT.y = 0f;
            if (toT.sqrMagnitude < 1e-8f)
            {
                img.gameObject.SetActive(false);
                continue;
            }

            toT.Normalize();
            var bearing = Vector3.SignedAngle(flatRef, toT, Vector3.up);
            var x = Mathf.Clamp(bearing * pixelsPerDegree, -halfW, halfW);
            var rt = img.rectTransform;
            float iconSize = ResolveWorldTargetIconSize(entry);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = new Vector2(x, worldTargetIconAnchoredY);
            var sp = entry.icon != null ? entry.icon : GetRuntimeWhiteSprite();
            img.sprite = sp;
            var a = entry.tint.a;
            if (Vector3.Dot(flatRef, toT) < 0f)
            {
                a *= worldTargetBehindAlphaScale;
            }

            var c = entry.tint;
            c.a = a;
            img.color = c;
            img.gameObject.SetActive(true);
        }
    }

    private Vector3 ResolveWorldOriginPosition()
    {
        if (player != null)
        {
            return player.transform.position;
        }

        var yawT = ResolveYawTransform();
        return yawT != null ? yawT.position : Vector3.zero;
    }

    private Vector3 ResolveFlatReferenceForward(Transform yawT)
    {
        if (worldTargetsRelativeToCameraForward || player == null)
        {
            var f = yawT.forward;
            f.y = 0f;
            return f;
        }

        var pf = player.transform.forward;
        pf.y = 0f;
        return pf;
    }

    private void HideAllWorldTargetMarkers()
    {
        for (var i = 0; i < _worldTargetMarkerPool.Count; i++)
        {
            if (_worldTargetMarkerPool[i] != null)
            {
                _worldTargetMarkerPool[i].gameObject.SetActive(false);
            }
        }
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
