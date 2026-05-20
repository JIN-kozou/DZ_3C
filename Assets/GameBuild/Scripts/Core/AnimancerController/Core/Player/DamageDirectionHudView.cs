using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界空间受击方向提示：挂在 DamageDirectionHint 根物体上，由 <see cref="DZ_3C.AI.Core.PlayerAIHurtReceiver"/> 在运行时 <c>AddComponent</c> 并 <c>Configure</c>。
/// 根节点面向相机，子箭头在平面内旋转，指向「锚点 → 攻击者」方向。
/// </summary>
[DisallowMultipleComponent]
public sealed class DamageDirectionHudView : MonoBehaviour
{
    [SerializeField, Tooltip("箭头子物体名称（预制体默认 DirectionHint_Image）。")]
    private string arrowChildName = "DirectionHint_Image";

    [SerializeField, Tooltip("箭头子 RectTransform 相对根在 UI 平面上的 Y 偏移（画布单位）。")]
    private float arrowAnchoredYOffset = 120f;

    [SerializeField, Tooltip("箭头绕 Z 的额外旋转（度），用于对齐贴图默认朝向。")]
    private float zRotationOffsetDegrees;

    [SerializeField, Tooltip("相对父锚点的本地位移（米），用于把提示放在胸口/头等位置。")]
    private Vector3 anchorLocalOffset;

    private Transform _followAnchor;
    private float _worldScale = 0.0025f;
    private Camera _worldCamera;
    private RectTransform _rootRect;
    private RectTransform _arrowRect;
    private CanvasGroup _canvasGroup;
    private Coroutine _showCo;

    public void Configure(Transform followAnchor, float worldScale, Camera worldCamera)
    {
        _followAnchor = followAnchor;
        _worldScale = worldScale;
        _worldCamera = worldCamera;
        _rootRect = GetComponent<RectTransform>();
        EnsureWorldCanvas();
        ResolveArrow();
        ApplyHidden();
    }

    public void ShowFromAttacker(GameObject attacker, float visibleSeconds)
    {
        if (attacker == null || visibleSeconds <= 0f || _followAnchor == null)
            return;

        if (_showCo != null)
            StopCoroutine(_showCo);
        _showCo = StartCoroutine(CoShow(attacker.transform, visibleSeconds));
    }

    private void OnDestroy()
    {
        if (_showCo != null)
        {
            StopCoroutine(_showCo);
            _showCo = null;
        }
    }

    private void EnsureWorldCanvas()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = ResolveCamera();
        canvas.sortingOrder = 120;

        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        if (_rootRect != null)
            _rootRect.localScale = Vector3.one * _worldScale;
    }

    private void ResolveArrow()
    {
        var t = FindDeepChild(transform, arrowChildName);
        _arrowRect = t != null ? t.GetComponent<RectTransform>() : null;
        if (_arrowRect == null)
            return;

        _arrowRect.anchorMin = _arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        _arrowRect.pivot = new Vector2(0.5f, 0.5f);
        _arrowRect.anchoredPosition = new Vector2(0f, arrowAnchoredYOffset);

        var img = _arrowRect.GetComponent<Image>();
        if (img != null)
            img.raycastTarget = false;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
            return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var c = FindDeepChild(root.GetChild(i), childName);
            if (c != null)
                return c;
        }
        return null;
    }

    private IEnumerator CoShow(Transform attacker, float seconds)
    {
        var end = Time.time + seconds;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        while (Time.time < end)
        {
            if (_followAnchor == null || attacker == null)
                break;

            UpdatePose(attacker.position);
            yield return null;
        }

        ApplyHidden();
        _showCo = null;
    }

    private void UpdatePose(Vector3 attackerWorld)
    {
        if (_rootRect == null || _followAnchor == null)
            return;

        _rootRect.localPosition = anchorLocalOffset;

        var cam = ResolveCamera();
        if (cam != null)
        {
            var hudWorld = _rootRect.position;
            var toCam = cam.transform.position - hudWorld;
            if (toCam.sqrMagnitude > 1e-8f)
                _rootRect.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        if (_arrowRect == null)
            return;

        var pos = _rootRect.position;
        var forward = _rootRect.forward;
        var toAttacker = attackerWorld - pos;
        var planar = Vector3.ProjectOnPlane(toAttacker, forward);
        if (planar.sqrMagnitude < 1e-6f)
        {
            _arrowRect.gameObject.SetActive(false);
            return;
        }

        _arrowRect.gameObject.SetActive(true);
        planar.Normalize();
        var ang = Mathf.Atan2(Vector3.Dot(planar, _rootRect.right), Vector3.Dot(planar, _rootRect.up)) * Mathf.Rad2Deg;
        _arrowRect.localEulerAngles = new Vector3(0f, 0f, -ang + zRotationOffsetDegrees);
    }

    private void ApplyHidden()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    private Camera ResolveCamera()
    {
        if (_worldCamera != null)
            return _worldCamera;
        return Camera.main;
    }
}
