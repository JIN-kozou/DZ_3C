using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按弹夹容量生成等量槽位，网格排列：每行 <see cref="barsPerRow"/> 格，先填满上一行再换行。
/// 消耗顺序为从左到右、从上到下（最先打掉的在最左上，空槽从左上往右下扩展）。
/// 仅在持枪模式下显示；下一发回弹中的槽位用填充进度表现。
/// </summary>
[DisallowMultipleComponent]
public class PlayerAmmoBarsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("留空则在运行时查找场景中的 Player。")]
    private Player player;

    [SerializeField, Tooltip("子弹槽的父节点（Grid 子物体挂点）。可与本脚本同物体：隐藏时用 CanvasGroup，不会 SetActive(false) 关掉自身导致无法再显示。")]
    private RectTransform barsRoot;

    [Header("Grid layout")]
    [SerializeField, Min(1), Tooltip("每一行摆放几个槽位。")]
    private int barsPerRow = 10;

    [SerializeField, Min(1f), Tooltip("单个槽位的宽度（像素）。")]
    private float barWidth = 6f;

    [SerializeField, Min(1f), Tooltip("单个槽位的高度（像素）。")]
    private float barHeight = 22f;

    [SerializeField, Min(0f), Tooltip("同一行内相邻槽位之间的水平间距。")]
    private float horizontalBarSpacing = 3f;

    [SerializeField, Min(0f), Tooltip("行与行之间的垂直间距。")]
    private float verticalRowSpacing = 3f;

    [SerializeField, Tooltip("弹夹超过该数量时只显示前 N 格并在 Console 打警告。")]
    private int maxBarCount = 80;

    [Header("Graphics (子弹图形，可选)")]
    [SerializeField, Tooltip("有弹时显示的 Sprite。可与空弹共用一张图，仅用颜色区分。")]
    private Sprite filledBulletSprite;

    [SerializeField, Tooltip("空弹槽显示的 Sprite。留空则空槽沿用有弹图并套用空弹颜色。")]
    private Sprite emptyBulletSprite;

    [SerializeField, Tooltip("使用 Sprite 时是否保持宽高比，避免拉伸变形。")]
    private bool preserveSpriteAspect = true;

    [SerializeField, Tooltip("满弹槽 Image 的绘制类型（Simple 或 Sliced 等）。回弹进度格强制使用 Filled。")]
    private Image.Type bulletImageType = Image.Type.Simple;

    [Header("Colors")]
    [SerializeField, Tooltip("有弹槽着色；与 Sprite 相乘。")]
    private Color filledColor = new Color(1f, 0.85f, 0.2f, 1f);

    [SerializeField, Tooltip("空弹槽着色；与 Sprite 相乘。")]
    private Color emptyColor = new Color(0.25f, 0.25f, 0.28f, 0.85f);

    [Header("Visibility")]
    [SerializeField, Tooltip("无武器配置时是否隐藏整条 UI。")]
    private bool hideWhenNoWeapon = true;

    private Image[] _barImages;
    private int _cachedMagazineSize = -1;
    private bool _loggedMagazineCapWarning;
    private Sprite _lastFilledSpriteRef;
    private Sprite _lastEmptySpriteRef;

    private int _cachedBarsPerRow = -1;
    private Vector2 _cachedCellSize = Vector2.negativeInfinity;
    private Vector2 _cachedGridSpacing = Vector2.negativeInfinity;

    private bool _hudHostIsBarsRoot;
    private CanvasGroup _barsHostCanvasGroup;

    private void Awake()
    {
        ResolvePlayer();

        if (barsRoot == null)
        {
            barsRoot = GetComponent<RectTransform>();
        }

        CacheVisibilityDriver();
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

    private void CacheVisibilityDriver()
    {
        if (barsRoot == null)
        {
            return;
        }

        _hudHostIsBarsRoot = barsRoot.gameObject == gameObject;
        if (_hudHostIsBarsRoot)
        {
            _barsHostCanvasGroup = barsRoot.GetComponent<CanvasGroup>();
            if (_barsHostCanvasGroup == null)
            {
                _barsHostCanvasGroup = barsRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (barsRoot == null)
        {
            return;
        }

        if (_hudHostIsBarsRoot && _barsHostCanvasGroup != null)
        {
            _barsHostCanvasGroup.alpha = visible ? 1f : 0f;
            _barsHostCanvasGroup.interactable = visible;
            _barsHostCanvasGroup.blocksRaycasts = visible;
            return;
        }

        barsRoot.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (barsRoot == null)
        {
            return;
        }

        ResolvePlayer();

        var weapon = player != null ? player.WeaponRuntime : null;
        var cfg = weapon != null ? weapon.GunConfig : null;

        if (cfg == null)
        {
            if (hideWhenNoWeapon)
            {
                SetHudVisible(false);
            }

            return;
        }

        if (player.ReusableData == null || !player.ReusableData.armedModeActive)
        {
            SetHudVisible(false);
            return;
        }

        SetHudVisible(true);

        int magazine = Mathf.Max(1, cfg.magazineSize);
        if (magazine > maxBarCount)
        {
            if (!_loggedMagazineCapWarning)
            {
                _loggedMagazineCapWarning = true;
                Debug.LogWarning($"[PlayerAmmoBarsUI] magazineSize={magazine} 超过 maxBarCount={maxBarCount}，显示已截断。", this);
            }

            magazine = maxBarCount;
        }
        else
        {
            _loggedMagazineCapWarning = false;
        }

        int columns = Mathf.Max(1, barsPerRow);
        var cell = new Vector2(barWidth, barHeight);
        var gridSpacing = new Vector2(horizontalBarSpacing, verticalRowSpacing);
        bool gridParamsChanged =
            _cachedBarsPerRow != columns ||
            (_cachedCellSize - cell).sqrMagnitude > 1e-6f ||
            (_cachedGridSpacing - gridSpacing).sqrMagnitude > 1e-6f;

        if (_cachedMagazineSize != magazine || _barImages == null || _barImages.Length != magazine || gridParamsChanged)
        {
            RebuildBars(magazine);
            _cachedMagazineSize = magazine;
            _cachedBarsPerRow = columns;
            _cachedCellSize = cell;
            _cachedGridSpacing = gridSpacing;
        }

        if (filledBulletSprite != _lastFilledSpriteRef || emptyBulletSprite != _lastEmptySpriteRef)
        {
            _lastFilledSpriteRef = filledBulletSprite;
            _lastEmptySpriteRef = emptyBulletSprite;
        }

        int ammo = Mathf.Clamp(weapon.CurrentAmmo, 0, magazine);
        int spent = magazine - ammo;
        bool regenEnabled = cfg.ammoRegenIntervalSeconds > 0f;
        float regenNorm = regenEnabled ? weapon.AmmoRegenProgressNormalized : 0f;
        int regenSlotIndex = spent > 0 ? spent - 1 : -1;

        for (int i = 0; i < _barImages.Length; i++)
        {
            if (_barImages[i] != null)
            {
                bool hasRound = i >= spent;
                bool isRegenSlot = !hasRound && i == regenSlotIndex;
                ApplyBarState(_barImages[i], hasRound, isRegenSlot, regenNorm);
            }
        }
    }

    private void RebuildBars(int count)
    {
        for (int i = barsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(barsRoot.GetChild(i).gameObject);
        }

        EnsureLayout();

        _barImages = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"AmmoBar_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(barsRoot, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var img = go.GetComponent<Image>();
            ApplyBarState(img, false, false, 0f);
            img.raycastTarget = false;

            _barImages[i] = img;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(barsRoot);
    }

    private void ApplyBarState(Image img, bool hasRound, bool isRegenSlot, float regenProgress)
    {
        regenProgress = Mathf.Clamp01(regenProgress);

        if (hasRound)
        {
            img.type = bulletImageType;
            img.fillAmount = 1f;
            ApplyFilledOrEmptySprite(img, true);
            return;
        }

        if (isRegenSlot && regenProgress > 0.0001f)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.fillAmount = regenProgress;
            bool anySprite = filledBulletSprite != null || emptyBulletSprite != null;
            if (anySprite)
            {
                img.sprite = filledBulletSprite != null ? filledBulletSprite : emptyBulletSprite;
                img.preserveAspect = preserveSpriteAspect;
            }
            else
            {
                img.sprite = null;
                img.preserveAspect = false;
            }

            img.color = Color.Lerp(emptyColor, filledColor, regenProgress);
            return;
        }

        img.type = Image.Type.Simple;
        img.fillAmount = 1f;
        ApplyFilledOrEmptySprite(img, false);
    }

    private void ApplyFilledOrEmptySprite(Image img, bool filled)
    {
        bool anySprite = filledBulletSprite != null || emptyBulletSprite != null;
        if (!anySprite)
        {
            img.sprite = null;
            img.preserveAspect = false;
            img.color = filled ? filledColor : emptyColor;
            return;
        }

        Sprite s = filled
            ? (filledBulletSprite != null ? filledBulletSprite : emptyBulletSprite)
            : (emptyBulletSprite != null ? emptyBulletSprite : filledBulletSprite);

        img.sprite = s;
        img.preserveAspect = preserveSpriteAspect;
        img.color = filled ? filledColor : emptyColor;
    }

    private void EnsureLayout()
    {
        var horizontal = barsRoot.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            Destroy(horizontal);
        }

        var grid = barsRoot.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = barsRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        int columns = Mathf.Max(1, barsPerRow);
        grid.cellSize = new Vector2(barWidth, barHeight);
        grid.spacing = new Vector2(horizontalBarSpacing, verticalRowSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(0, 0, 0, 0);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxBarCount = Mathf.Max(1, maxBarCount);
        barsPerRow = Mathf.Max(1, barsPerRow);
        barWidth = Mathf.Max(1f, barWidth);
        barHeight = Mathf.Max(1f, barHeight);
        if (barsRoot != null && Application.isPlaying)
        {
            CacheVisibilityDriver();
        }
    }
#endif
}
