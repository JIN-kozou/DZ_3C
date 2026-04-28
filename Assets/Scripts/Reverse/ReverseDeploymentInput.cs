using System;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 部署 / 收回输入控制器。
    ///   - E：扫描 cores（内→外）找第一个满血核心，部署为阵列。
    ///   - Q：远程收回最后部署的阵列（LIFO，无距离限制）。
    ///   - 1/2/3：远程收回对应槽位的阵列。
    ///
    /// 操作流程：
    ///   部署 = ReverseCoreStack.TryDeployInnermostFull(out core)
    ///        + ReverseArrayRegistry.AllocateSlot()
    ///        + Instantiate(prefab) + ReverseArray.Setup(slot, seq, energy)
    ///        + Registry.RegisterDeployed(arr)
    ///   收回 = Registry.PeekLatest() / FindBySlot(k)
    ///        + Registry.UnregisterArray(arr)
    ///        + ReverseCoreStack.AcceptRetrievedCoreAtOutermost()
    ///        + Destroy(arr.gameObject)
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseDeploymentInput : MonoBehaviour
    {
        [Header("References (引用)")]
        [SerializeField] private ReverseCoreStack coreStack;
        [SerializeField] private ReverseArrayRegistry registry;
        [SerializeField] private ReverseConfig config;

        [Header("Prefab (阵列预制体)")]
        [Tooltip("ReverseArray.prefab 的引用。部署时 Instantiate。")]
        [SerializeField] private ReverseArray arrayPrefab;

        [Tooltip("阵列的父级（场景里的容器节点）。空则放到场景根。")]
        [SerializeField] private Transform arrayContainer;

        [Tooltip("部署位置参考点。空则用 transform 自身（玩家本体）。")]
        [SerializeField] private Transform deployAnchor;

        // ---------- 事件（给 UI / debug 用） ----------

        public event Action<ReverseArray> OnDeployed;
        public event Action<ReverseArray> OnRetrieved;

        /// <summary>部署失败原因：NoFullCore / RegistryFull / PrefabMissing。</summary>
        public event Action<string> OnDeployFailed;

        /// <summary>收回失败原因：StackEmpty / SlotEmpty。</summary>
        public event Action<string> OnRetrieveFailed;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            if (registry == null && coreStack != null) registry = coreStack.Registry;
            if (config == null && coreStack != null) config = coreStack.Config;
            if (deployAnchor == null) deployAnchor = transform;
        }

        private void Update()
        {
            if (config == null) return;

            if (Input.GetKeyDown(config.deployKey))
            {
                TryDeploy();
            }

            if (Input.GetKeyDown(config.retrieveLastKey))
            {
                TryRetrieveLast();
            }

            if (config.retrieveBySlotKeys != null)
            {
                for (int i = 0; i < config.retrieveBySlotKeys.Length; i++)
                {
                    if (Input.GetKeyDown(config.retrieveBySlotKeys[i]))
                    {
                        TryRetrieveBySlot(i + 1);
                    }
                }
            }
        }

        // ---------- 部署 ----------

        public bool TryDeploy()
        {
            if (coreStack == null || registry == null)
            {
                OnDeployFailed?.Invoke("MissingRefs");
                return false;
            }
            if (arrayPrefab == null)
            {
                OnDeployFailed?.Invoke("PrefabMissing");
                Debug.LogWarning("[ReverseDeploymentInput] ReverseArray.prefab 没有指定，无法部署。");
                return false;
            }

            int slot = registry.AllocateSlot();
            if (slot < 0)
            {
                OnDeployFailed?.Invoke("RegistryFull");
                return false;
            }
            if (!coreStack.TryDeployInnermostFull(out var deployedCore))
            {
                registry.ReleaseSlot(slot);
                OnDeployFailed?.Invoke("NoFullCore");
                return false;
            }

            Vector3 pos = deployAnchor != null ? deployAnchor.position : transform.position;
            Vector3 fwd = deployAnchor != null ? deployAnchor.forward : transform.forward;
            Vector3 spawnPos = pos
                               + fwd * config.deployOffset.z
                               + (deployAnchor != null ? deployAnchor.right : transform.right) * config.deployOffset.x
                               + Vector3.up * config.deployOffset.y;

            ReverseArray arr = Instantiate(arrayPrefab, spawnPos, Quaternion.identity, arrayContainer);
            long seq = registry.AllocateSequence();
            arr.Setup(slot, seq, config.arrayDefaultEnergy);
            registry.RegisterDeployed(arr);
            OnDeployed?.Invoke(arr);
            return true;
        }

        // ---------- 收回 ----------

        public bool TryRetrieveLast()
        {
            if (registry == null || coreStack == null) return false;
            ReverseArray arr = registry.PeekLatest();
            if (arr == null)
            {
                OnRetrieveFailed?.Invoke("StackEmpty");
                return false;
            }
            return RetrieveImpl(arr);
        }

        public bool TryRetrieveBySlot(int slotIndex)
        {
            if (registry == null || coreStack == null) return false;
            ReverseArray arr = registry.FindBySlot(slotIndex);
            if (arr == null)
            {
                OnRetrieveFailed?.Invoke("SlotEmpty");
                return false;
            }
            return RetrieveImpl(arr);
        }

        private bool RetrieveImpl(ReverseArray arr)
        {
            registry.UnregisterArray(arr);
            coreStack.AcceptRetrievedCoreAtOutermost();
            OnRetrieved?.Invoke(arr);
            Destroy(arr.gameObject);
            return true;
        }
    }
}
