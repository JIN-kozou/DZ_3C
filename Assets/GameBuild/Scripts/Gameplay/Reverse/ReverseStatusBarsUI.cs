using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 把 Reverse 系统状态映射到 UGUI 条形控件：
    /// - Anchor: 正常/低血(<=20%)两种颜色
    /// - Core: 满血/非满血两种颜色
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseStatusBarsUI : MonoBehaviour
    {
        [Header("References (引用)")]
        [SerializeField] private ReverseCoreStack coreStack;
        [SerializeField] private ReverseAnchor anchor;

        [Header("Anchor UI")]
        [Tooltip("Anchor 血量条。value 使用 0~1。")]
        [SerializeField] private Slider anchorBar;
        [Tooltip("Anchor 状态颜色目标。可拖 fill Image，不拖则尝试使用 anchorBar.fillRect。")]
        [SerializeField] private Graphic anchorStateGraphic;
        [Range(0f, 1f)]
        [Tooltip("Anchor 血量比例 <= 该阈值时，进入低血量状态。默认 20%。")]
        [SerializeField] private float anchorLowHealthRatio = 0.2f;
        [SerializeField] private Color anchorNormalColor = new Color(0.20f, 0.90f, 0.30f, 1f);
        [SerializeField] private Color anchorLowColor = new Color(1.00f, 0.25f, 0.25f, 1f);

        [Header("Core UI")]
        [Tooltip("Core 条数组。索引 i 显示 cores[i]。")]
        [SerializeField] private Slider[] coreBars;
        [Tooltip("Core 状态颜色目标数组。索引 i 对应 cores[i]。可拖 fill Image。")]
        [SerializeField] private Graphic[] coreStateGraphics;
        [Tooltip("Core 的 Fill Area 容器数组。空则自动用对应 Slider 的 fillRect.parent。核心耗尽时会隐藏。")]
        [SerializeField] private RectTransform[] coreFillAreas;
        [SerializeField] private Color coreFullColor = new Color(0.30f, 0.75f, 1.00f, 1f);
        [SerializeField] private Color coreNotFullColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [Tooltip("当 UI 条数量 > 当前核心数量时，是否隐藏多余条。")]
        [SerializeField] private bool hideUnusedCoreBars = true;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponentInParent<ReverseCoreStack>();
            if (anchor == null && coreStack != null) anchor = coreStack.Anchor;
            if (anchorStateGraphic == null && anchorBar != null && anchorBar.fillRect != null)
            {
                anchorStateGraphic = anchorBar.fillRect.GetComponent<Graphic>();
            }
            AutoResolveCoreUiTargets();
        }

        private void OnEnable()
        {
            if (coreStack != null) coreStack.OnCoresChanged += RefreshAll;
            if (anchor != null) anchor.OnAnchorHealthChanged += HandleAnchorHealthChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (coreStack != null) coreStack.OnCoresChanged -= RefreshAll;
            if (anchor != null) anchor.OnAnchorHealthChanged -= HandleAnchorHealthChanged;
        }

        private void Update()
        {
            // 核心血量在 TickFlow/ApplyDamage 中会持续变化，当前没有逐帧事件，故这里做轻量轮询刷新。
            RefreshAll();
        }

        private void RefreshAll()
        {
            AutoResolveCoreUiTargets();
            RefreshAnchorOnly();
            RefreshCoresOnly();
        }

        private void HandleAnchorHealthChanged(float _)
        {
            RefreshAnchorOnly();
        }

        private void RefreshAnchorOnly()
        {
            if (anchor == null) return;

            float max = Mathf.Max(0.0001f, anchor.MaxHealth);
            float ratio = Mathf.Clamp01(anchor.CurrentHealth / max);

            if (anchorBar != null)
            {
                anchorBar.minValue = 0f;
                anchorBar.maxValue = 1f;
                anchorBar.value = ratio;
            }

            if (anchorStateGraphic != null)
            {
                anchorStateGraphic.color = ratio <= anchorLowHealthRatio ? anchorLowColor : anchorNormalColor;
            }
        }

        private void RefreshCoresOnly()
        {
            if (coreStack == null) return;
            var cores = coreStack.Cores;

            int barCount = coreBars != null ? coreBars.Length : 0;
            for (int i = 0; i < barCount; i++)
            {
                bool hasCore = i < cores.Count && cores[i] != null;
                var bar = coreBars[i];
                if (bar != null)
                {
                    if (hideUnusedCoreBars) bar.gameObject.SetActive(hasCore);
                    if (hasCore)
                    {
                        float max = Mathf.Max(0.0001f, cores[i].MaxHealth);
                        float ratio = Mathf.Clamp01(cores[i].Health / max);
                        bar.minValue = 0f;
                        bar.maxValue = 1f;
                        bar.value = ratio;
                    }
                }
            }

            int graphicCount = coreStateGraphics != null ? coreStateGraphics.Length : 0;
            for (int i = 0; i < graphicCount; i++)
            {
                var g = coreStateGraphics[i];
                if (g == null) continue;

                bool hasCore = i < cores.Count && cores[i] != null;
                if (hideUnusedCoreBars) g.gameObject.SetActive(hasCore);
                if (!hasCore) continue;

                g.color = cores[i].IsFull ? coreFullColor : coreNotFullColor;
            }

            int fillAreaCount = coreFillAreas != null ? coreFillAreas.Length : 0;
            for (int i = 0; i < fillAreaCount; i++)
            {
                var fillArea = coreFillAreas[i];
                if (fillArea == null) continue;

                bool hasCore = i < cores.Count && cores[i] != null;
                if (!hasCore)
                {
                    if (hideUnusedCoreBars) fillArea.gameObject.SetActive(false);
                    continue;
                }

                bool isEmpty = cores[i].IsEmpty;
                fillArea.gameObject.SetActive(!isEmpty);
            }
        }

        private void AutoResolveCoreUiTargets()
        {
            int barCount = coreBars != null ? coreBars.Length : 0;
            if (barCount <= 0) return;

            if (coreStateGraphics == null || coreStateGraphics.Length != barCount)
            {
                var newArray = new Graphic[barCount];
                if (coreStateGraphics != null)
                {
                    int copy = Mathf.Min(coreStateGraphics.Length, newArray.Length);
                    for (int i = 0; i < copy; i++) newArray[i] = coreStateGraphics[i];
                }
                coreStateGraphics = newArray;
            }

            if (coreFillAreas == null || coreFillAreas.Length != barCount)
            {
                var newArray = new RectTransform[barCount];
                if (coreFillAreas != null)
                {
                    int copy = Mathf.Min(coreFillAreas.Length, newArray.Length);
                    for (int i = 0; i < copy; i++) newArray[i] = coreFillAreas[i];
                }
                coreFillAreas = newArray;
            }

            for (int i = 0; i < barCount; i++)
            {
                var bar = coreBars[i];
                if (bar == null || bar.fillRect == null) continue;

                if (coreStateGraphics[i] == null)
                {
                    coreStateGraphics[i] = bar.fillRect.GetComponent<Graphic>();
                }

                if (coreFillAreas[i] == null && bar.fillRect.parent is RectTransform parentRt)
                {
                    coreFillAreas[i] = parentRt;
                }
            }
        }
    }
}
