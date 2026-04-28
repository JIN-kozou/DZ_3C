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

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            EnsureShaderPosition();
        }

        private void OnEnable()
        {
            if (coreStack != null)
            {
                coreStack.OnViewRadiusChanged += HandleViewRadiusChanged;
                if (shaderPosition != null)
                {
                    shaderPosition.radius = coreStack.CurrentViewRadius > 0f
                        ? coreStack.CurrentViewRadius
                        : initialRadius;
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
                shaderPosition.radius = coreStack.ComputeViewRadius();
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

        private void HandleViewRadiusChanged(float newRadius)
        {
            if (shaderPosition == null) EnsureShaderPosition();
            if (shaderPosition != null) shaderPosition.radius = newRadius;
        }
    }
}
