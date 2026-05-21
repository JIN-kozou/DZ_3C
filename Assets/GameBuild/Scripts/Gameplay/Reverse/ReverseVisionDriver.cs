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
    [RequireComponent(typeof(ShaderPositionRadiusTransition))]
    public class ReverseVisionDriver : MonoBehaviour
    {
        [Header("References (引用)")]
        [SerializeField] private ReverseCoreStack coreStack;

        [Tooltip("玩家身上的 ShaderPosition。空则在 Awake/Start 自动 GetComponent。如缺失会自动 AddComponent。")]
        [SerializeField] private ShaderPosition shaderPosition;

        [SerializeField] private ShaderPositionRadiusTransition radiusTransition;

        [Tooltip("初始 radius，进游戏的瞬间值。Awake 后立刻被 ReverseCoreStack 覆盖。")]
        [SerializeField] private float initialRadius = 5f;

        [Header("Point Light Sync (点光源联动)")]
        [Tooltip("用于跟随视野半径缩放 range 的点光源。空则自动在子节点查找第一个 Point Light。")]
        [SerializeField] private Light pointLight;

        private float lightRangeBase = -1f;
        private float lightRadiusBase = -1f;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            EnsureShaderPosition();
            EnsureRadiusTransition();
            EnsurePointLight();

            if (coreStack != null && coreStack.Config != null)
            {
                radiusTransition.SetConfig(coreStack.Config);
            }
        }

        private void OnEnable()
        {
            if (radiusTransition != null)
            {
                radiusTransition.RadiusChanged += SyncPointLightRange;
            }

            if (coreStack != null)
            {
                coreStack.OnViewRadiusChanged += HandleViewRadiusChanged;
                if (radiusTransition != null)
                {
                    float radius = coreStack.CurrentViewRadius > 0f
                        ? coreStack.CurrentViewRadius
                        : initialRadius;
                    radiusTransition.ApplyRadiusImmediate(radius);
                }
            }
        }

        private void OnDisable()
        {
            if (coreStack != null)
            {
                coreStack.OnViewRadiusChanged -= HandleViewRadiusChanged;
            }

            if (radiusTransition != null)
            {
                radiusTransition.RadiusChanged -= SyncPointLightRange;
            }
        }

        private void Start()
        {
            // 兜底：CoreStack.Start 后才把初值算出来；这里再同步一次。
            if (coreStack != null && radiusTransition != null)
            {
                radiusTransition.ApplyRadiusImmediate(coreStack.ComputeViewRadius());
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
            }
        }

        private void EnsureRadiusTransition()
        {
            if (radiusTransition == null)
            {
                radiusTransition = GetComponent<ShaderPositionRadiusTransition>();
            }

            if (radiusTransition == null)
            {
                radiusTransition = gameObject.AddComponent<ShaderPositionRadiusTransition>();
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
            float radiusBase = radiusTransition != null && radiusTransition.CurrentRadius > 0f
                ? radiusTransition.CurrentRadius
                : initialRadius;
            if (lightRadiusBase <= 0f) lightRadiusBase = Mathf.Max(0.0001f, radiusBase);
        }

        private void HandleViewRadiusChanged(float newRadius)
        {
            if (radiusTransition == null) EnsureRadiusTransition();
            if (radiusTransition == null) return;

            radiusTransition.SetTargetRadius(newRadius);
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
