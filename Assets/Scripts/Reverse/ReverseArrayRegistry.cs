using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 已部署阵列的全局登记表 + 槽位分配器。
    /// 维护：
    ///   - deployedArrays：当前场景里所有 ReverseArray 实例的引用（不含 destroyed）
    ///   - slotOccupied[i]：槽位 i+1（1-based）是否被占
    ///   - deploymentSequenceCounter：每次 AllocateSequence 单调 +1
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseArrayRegistry : MonoBehaviour
    {
        [SerializeField] private ReverseConfig config;

        private readonly List<ReverseArray> deployedArrays = new List<ReverseArray>();
        private bool[] slotOccupied = Array.Empty<bool>();
        private long deploymentSequenceCounter;

        public IReadOnlyList<ReverseArray> DeployedArrays => deployedArrays;
        public int DeployedCount => deployedArrays.Count;
        public ReverseConfig Config => config;

        public event Action<ReverseArray> OnArrayDeployed;
        public event Action<ReverseArray> OnArrayRetrieved;

        public void SetConfig(ReverseConfig cfg)
        {
            config = cfg;
            EnsureSlotArraySize();
        }

        private void Awake()
        {
            EnsureSlotArraySize();
        }

        private void EnsureSlotArraySize()
        {
            int desired = config != null ? Mathf.Max(1, config.maxCoreCount) : 3;
            if (slotOccupied == null || slotOccupied.Length != desired)
            {
                bool[] next = new bool[desired];
                if (slotOccupied != null)
                {
                    int n = Mathf.Min(slotOccupied.Length, desired);
                    for (int i = 0; i < n; i++) next[i] = slotOccupied[i];
                }
                slotOccupied = next;
            }
        }

        /// <summary>分配最小可用槽位（1-based）。失败返回 -1。</summary>
        public int AllocateSlot()
        {
            EnsureSlotArraySize();
            for (int i = 0; i < slotOccupied.Length; i++)
            {
                if (!slotOccupied[i])
                {
                    slotOccupied[i] = true;
                    return i + 1;
                }
            }
            return -1;
        }

        public void ReleaseSlot(int slotIndex)
        {
            EnsureSlotArraySize();
            int i = slotIndex - 1;
            if (i >= 0 && i < slotOccupied.Length)
            {
                slotOccupied[i] = false;
            }
        }

        public bool IsSlotOccupied(int slotIndex)
        {
            EnsureSlotArraySize();
            int i = slotIndex - 1;
            return i >= 0 && i < slotOccupied.Length && slotOccupied[i];
        }

        public long AllocateSequence()
        {
            deploymentSequenceCounter++;
            return deploymentSequenceCounter;
        }

        public void RegisterDeployed(ReverseArray array)
        {
            if (array == null) return;
            if (!deployedArrays.Contains(array))
            {
                deployedArrays.Add(array);
                OnArrayDeployed?.Invoke(array);
            }
        }

        public void UnregisterArray(ReverseArray array)
        {
            if (array == null) return;
            if (deployedArrays.Remove(array))
            {
                ReleaseSlot(array.SlotIndex);
                OnArrayRetrieved?.Invoke(array);
            }
        }

        /// <summary>Q 键 LIFO：返回 deploymentSequence 最大的阵列；为空返回 null。</summary>
        public ReverseArray PeekLatest()
        {
            ReverseArray best = null;
            long bestSeq = long.MinValue;
            for (int i = 0; i < deployedArrays.Count; i++)
            {
                var a = deployedArrays[i];
                if (a == null) continue;
                if (a.DeploymentSequence > bestSeq)
                {
                    bestSeq = a.DeploymentSequence;
                    best = a;
                }
            }
            return best;
        }

        /// <summary>1/2/3 键：按槽位号查找。找不到返回 null。</summary>
        public ReverseArray FindBySlot(int slotIndex)
        {
            for (int i = 0; i < deployedArrays.Count; i++)
            {
                var a = deployedArrays[i];
                if (a != null && a.SlotIndex == slotIndex) return a;
            }
            return null;
        }

        /// <summary>
        /// 复活点选择：先按距离最小，再按 deploymentSequence 最大（同距离优先最新部署）。
        /// 没有任何阵列时返回 null。
        /// </summary>
        public ReverseArray FindRespawnTarget(Vector3 worldPosition)
        {
            ReverseArray best = null;
            float bestSqr = float.MaxValue;
            long bestSeq = long.MinValue;
            const float distEpsilon = 0.0001f;
            for (int i = 0; i < deployedArrays.Count; i++)
            {
                var a = deployedArrays[i];
                if (a == null) continue;
                float d = a.SqrDistanceTo(worldPosition);
                if (d < bestSqr - distEpsilon)
                {
                    bestSqr = d;
                    bestSeq = a.DeploymentSequence;
                    best = a;
                }
                else if (Mathf.Abs(d - bestSqr) <= distEpsilon && a.DeploymentSequence > bestSeq)
                {
                    bestSeq = a.DeploymentSequence;
                    best = a;
                }
            }
            return best;
        }

        public void ClearAll()
        {
            for (int i = deployedArrays.Count - 1; i >= 0; i--)
            {
                var a = deployedArrays[i];
                if (a != null) Destroy(a.gameObject);
            }
            deployedArrays.Clear();
            for (int i = 0; i < slotOccupied.Length; i++) slotOccupied[i] = false;
        }
    }
}
