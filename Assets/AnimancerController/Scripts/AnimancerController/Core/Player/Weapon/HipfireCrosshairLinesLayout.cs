using UnityEngine;

/// <summary>
/// 腰射十字准心四条线：开镜时向中心收拢并与 <see cref="CanvasGroup.alpha"/> 同步渐隐；关镜时同步拉开、渐显。
/// 目标乘子由 <see cref="PlayerArmedPresentation.EvaluateHipfireBracketDisplayMultiplier"/> 给出；对快速连点 ADS 用单一 SmoothDamp 跟目标，避免收拢与透明度脱节。
/// </summary>
[DisallowMultipleComponent]
public class HipfireCrosshairLinesLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("留空则在运行时查找场景中的 Player。")]
    private Player player;

    [SerializeField] private RectTransform lineLeft;
    [SerializeField] private RectTransform lineRight;
    [SerializeField] private RectTransform lineUp;
    [SerializeField] private RectTransform lineDown;

    [SerializeField, Tooltip("通常与本物体上的 CanvasGroup 相同（四条线父节点）。")]
    private CanvasGroup linesGroup;

    [Header("Collapse & fade smoothing")]
    [SerializeField, Min(0.0001f), Tooltip("目标乘子下降（开镜：收拢+变暗）时的平滑时间。")]
    private float bracketFadeOutSmoothTime = 0.07f;

    [SerializeField, Min(0.0001f), Tooltip("目标乘子上升（关镜：拉开+变亮）时的平滑时间；略长可减轻快速连点 ADS 抖动。")]
    private float bracketFadeInSmoothTime = 0.11f;

    [SerializeField, Tooltip("使用 unscaledDeltaTime（一般保持关闭）。")]
    private bool useUnscaledTime;

    private float _baseHalfX = -1f;
    private float _baseHalfY = -1f;
    private float _displayM = 1f;
    private float _bracketSmoothVelocity;

    private void Awake()
    {
        if (linesGroup == null)
        {
            linesGroup = GetComponent<CanvasGroup>();
        }

        ResolvePlayer();
        CacheBaseLineExtents();
    }

    private void OnEnable()
    {
        _bracketSmoothVelocity = 0f;
        if (linesGroup != null)
        {
            _displayM = Mathf.Clamp01(linesGroup.alpha);
        }
        else
        {
            _displayM = 1f;
        }
    }

    private void CacheBaseLineExtents()
    {
        if (lineLeft == null || lineRight == null || lineUp == null || lineDown == null)
        {
            return;
        }

        _baseHalfX = Mathf.Max(0.0001f, Mathf.Abs(lineRight.anchoredPosition.x));
        _baseHalfY = Mathf.Max(0.0001f, Mathf.Abs(lineUp.anchoredPosition.y));
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = FindObjectOfType<Player>();
    }

    private void LateUpdate()
    {
        ResolvePlayer();

        if (linesGroup == null)
        {
            return;
        }

        var weapon = player != null ? player.WeaponRuntime : null;
        var presentation = player != null ? player.ArmedPresentation : null;

        bool adsHeld = weapon != null && weapon.IsAds;
        float targetM = presentation != null
            ? presentation.EvaluateHipfireBracketDisplayMultiplier(adsHeld)
            : (adsHeld ? 0f : 1f);

        targetM = Mathf.Clamp01(targetM);

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float smoothTime = targetM < _displayM ? bracketFadeOutSmoothTime : bracketFadeInSmoothTime;
        _displayM = Mathf.SmoothDamp(
            _displayM,
            targetM,
            ref _bracketSmoothVelocity,
            Mathf.Max(0.0001f, smoothTime),
            Mathf.Infinity,
            dt);

        if (Mathf.Abs(_displayM - targetM) < 0.002f)
        {
            _displayM = targetM;
            _bracketSmoothVelocity = 0f;
        }

        linesGroup.alpha = Mathf.Clamp01(_displayM);

        if (_baseHalfX > 0f && _baseHalfY > 0f &&
            lineLeft != null && lineRight != null && lineUp != null && lineDown != null)
        {
            float x = _baseHalfX * _displayM;
            float y = _baseHalfY * _displayM;

            var al = lineLeft.anchoredPosition;
            var ar = lineRight.anchoredPosition;
            var au = lineUp.anchoredPosition;
            var ad = lineDown.anchoredPosition;

            al.x = -x;
            ar.x = x;
            au.y = y;
            ad.y = -y;

            lineLeft.anchoredPosition = al;
            lineRight.anchoredPosition = ar;
            lineUp.anchoredPosition = au;
            lineDown.anchoredPosition = ad;
        }
    }
}
