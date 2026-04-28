using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 把 ReverseCoreStack 计算出的视野半径同步到玩家身上的 ShaderPosition.radius。
    /// 部署到场景的 ReverseArray 各自带 ShaderPosition（fixed 5m），由它们独立 reveal，
    /// 本脚本只负责"玩家本体"这 1 个 reveal 点。
    ///
    /// 现有 ShaderPosition 全局最多 4 个 instance（见其内部 instances[0..3]），
    /// 1（玩家） + 3（部署阵列上限）正好 = 4，刚好够用。
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseVisionDriver : MonoBehaviour
    {
        [Header("References (引用)")]
        [SerializeField] private ReverseCoreStack coreStack;

        [Tooltip("玩家身上的 ShaderPosition。空则在 Awake/Start 自动 GetComponent。如缺失会自动 AddComponent。")]
        [SerializeField] private ShaderPosition shaderPosition;

        [Tooltip("初始 radius，进游戏的瞬间值。Awake 后立刻被 ReverseCoreStack 覆盖。")]
        [SerializeField] private float initialRadius = 5f;

        [Header("Point Light Sync (点光源联动)")]
        [Tooltip("用于跟随视野半径缩放 range 的点光源。空则自动在子节点查找第一个 Point Light。")]
        [SerializeField] private Light pointLight;

        [Header("Fallback Transition (兜底过渡参数)")]
        [Tooltip("当未绑定 ReverseConfig 时，半径过渡的默认时长（秒）。0 表示瞬变。")]
        [Min(0f)]
        [SerializeField] private float fallbackTransitionDuration = 0.25f;

        [Tooltip("当未绑定 ReverseConfig 时，半径过渡曲线。X=归一化时间(0~1)，Y=归一化插值(0~1)。")]
        [SerializeField] private AnimationCurve fallbackTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float currentRadius;
        private float transitionStartRadius;
        private float transitionTargetRadius;
        private float transitionElapsed;
        private bool isTransitioning;
        private float lightRangeBase = -1f;
        private float lightRadiusBase = -1f;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            EnsureShaderPosition();
            EnsurePointLight();
        }

        private void OnEnable()
        {
            if (coreStack != null)
            {
                coreStack.OnViewRadiusChanged += HandleViewRadiusChanged;
                if (shaderPosition != null)
                {
                    currentRadius = coreStack.CurrentViewRadius > 0f
                        ? coreStack.CurrentViewRadius
                        : initialRadius;
                    shaderPosition.radius = currentRadius;
                    SyncPointLightRange(currentRadius);
                    isTransitioning = false;
                }
            }
        }

        private void OnDisable()
        {
            if (coreStack != null)
            {
                coreStack.OnViewRadiusChanged -= HandleViewRadiusChanged;
            }
        }

        private void Start()
        {
            // 兜底：CoreStack.Start 后才把初值算出来；这里再同步一次。
            if (coreStack != null && shaderPosition != null)
            {
                currentRadius = coreStack.ComputeViewRadius();
                shaderPosition.radius = currentRadius;
                SyncPointLightRange(currentRadius);
                isTransitioning = false;
            }
        }

        private void Update()
        {
            if (!isTransitioning || shaderPosition == null) return;

            float duration = GetTransitionDuration();
            if (duration <= 0f)
            {
                ApplyRadiusImmediate(transitionTargetRadius);
                return;
            }

            transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transitionElapsed / duration);
            float easedT = EvaluateTransitionCurve(t);

            currentRadius = Mathf.LerpUnclamped(transitionStartRadius, transitionTargetRadius, easedT);
            shaderPosition.radius = currentRadius;
            SyncPointLightRange(currentRadius);

            if (t >= 1f)
            {
                ApplyRadiusImmediate(transitionTargetRadius);
            }
        }

        private void EnsureShaderPosition()
        {
            if (shaderPosition != null) return;
            shaderPosition = GetComponent<ShaderPosition>();
            if (shaderPosition == null)
            {
                shaderPosition = gameObject.AddComponent<ShaderPosition>();
                shaderPosition.radius = initialRadius;
                currentRadius = initialRadius;
            }
        }

        private void EnsurePointLight()
        {
            if (pointLight == null)
            {
                var lights = GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Point)
                    {
                        pointLight = lights[i];
                        break;
                    }
                }
            }

            if (pointLight == null || pointLight.type != LightType.Point) return;

            if (lightRangeBase <= 0f) lightRangeBase = pointLight.range;
            if (lightRadiusBase <= 0f) lightRadiusBase = Mathf.Max(0.0001f, currentRadius > 0f ? currentRadius : initialRadius);
        }

        private void HandleViewRadiusChanged(float newRadius)
        {
            if (shaderPosition == null) EnsureShaderPosition();
            if (shaderPosition == null) return;

            float duration = GetTransitionDuration();
            if (duration <= 0f)
            {
                ApplyRadiusImmediate(newRadius);
                return;
            }

            transitionStartRadius = currentRadius > 0f ? currentRadius : shaderPosition.radius;
            transitionTargetRadius = newRadius;
            transitionElapsed = 0f;
            isTransitioning = true;
        }

        private float GetTransitionDuration()
        {
            if (coreStack != null && coreStack.Config != null)
            {
                return Mathf.Max(0f, coreStack.Config.viewRadiusTransitionDuration);
            }
            return Mathf.Max(0f, fallbackTransitionDuration);
        }

        private float EvaluateTransitionCurve(float normalizedTime)
        {
            AnimationCurve curve = null;
            if (coreStack != null && coreStack.Config != null)
            {
                curve = coreStack.Config.viewRadiusTransitionCurve;
            }
            if (curve == null || curve.length == 0)
            {
                curve = fallbackTransitionCurve;
            }
            if (curve == null || curve.length == 0)
            {
                return normalizedTime;
            }
            return curve.Evaluate(normalizedTime);
        }

        private void ApplyRadiusImmediate(float radius)
        {
            currentRadius = radius;
            transitionTargetRadius = radius;
            isTransitioning = false;
            shaderPosition.radius = radius;
            SyncPointLightRange(radius);
        }

        private void SyncPointLightRange(float radius)
        {
            EnsurePointLight();
            if (pointLight == null || pointLight.type != LightType.Point) return;

            float ratio = radius / Mathf.Max(0.0001f, lightRadiusBase);
            pointLight.range = Mathf.Max(0f, lightRangeBase * ratio);
        }
    }
}
