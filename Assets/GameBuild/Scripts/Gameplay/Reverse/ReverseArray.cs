using System;
using TMPro;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 已部署到场景里的逆重阵列（球状 reveal point）。
    /// 由 ReverseArray.prefab 实例化得到，挂在场景里。
    /// 持有：
    ///   - slotIndex：1/2/3 槽位号，用于数字键远程收回（按部署顺序的槽位号）
    ///   - deploymentSequence：单调递增的部署序号，用于 LIFO（Q 键）和复活点选择
    ///   - energy：阵列剩余能量（本期收回直接补满核心，所以暂不消耗）
    /// 不直接控制 ShaderPosition.radius——那个由 Prefab 上预先填好的 5m 决定。
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseArray : MonoBehaviour
    {
        [Header("Identity (身份)")]
        [Tooltip("槽位号（1~maxCoreCount），按部署顺序分配。数字键 1/2/3 远程收回时通过该值匹配。")]
        [SerializeField] private int slotIndex = -1;

        [Tooltip("部署序号（单调递增，永不重复）。Q 键 LIFO 收回时找最大值；复活时同距离也优先选最大值。")]
        [SerializeField] private long deploymentSequence;

        [Header("Energy (能量)")]
        [Tooltip("阵列剩余能量。本期收回直接给核心补满，不消耗能量字段。仅在调试 / 后续扩展用。")]
        [SerializeField] private float energy;

        [Header("Optional UI (可选 UI)")]
        [Tooltip("World-Space Canvas 上展示槽位号的文本（TextMeshProUGUI）。可空。")]
        [SerializeField] private TextMeshProUGUI slotLabel;

        public int SlotIndex => slotIndex;
        public long DeploymentSequence => deploymentSequence;
        public float Energy => energy;

        /// <summary>初始化：在 Instantiate 出来后由 ReverseArrayRegistry / DeploymentInput 调一次。</summary>
        public void Setup(int slotIndex, long deploymentSequence, float energy)
        {
            this.slotIndex = slotIndex;
            this.deploymentSequence = deploymentSequence;
            this.energy = Mathf.Max(0f, energy);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (slotLabel != null)
            {
                slotLabel.text = slotIndex > 0 ? slotIndex.ToString() : "?";
            }
        }

        /// <summary>距离查询助手：玩家死亡时找最近阵列。</summary>
        public float SqrDistanceTo(Vector3 worldPosition)
        {
            return (transform.position - worldPosition).sqrMagnitude;
        }
    }
}
