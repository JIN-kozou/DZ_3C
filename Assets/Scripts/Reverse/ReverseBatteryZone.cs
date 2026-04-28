using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 逆重充能站（地面区域）。
    /// 玩家进入触发器：通过 PlayerBuffSystem 申请一个 Regeneration buff
    ///                （RecoverTargetType=ReverseSystem，由 Player.RecoverResource 路由到
    ///                 ReverseCoreStack.RecoverFromInnermost 链式补血——锚优先，再 cores[0]→cores[N-1]）。
    /// 玩家离开触发器：主动调用 PlayerBuffSystem.RemoveBuff(buffId) 终止回血——
    ///                 这是"走出区域立刻断电"语义，与 PlayerBuffTriggerTransmitter 不同。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ReverseBatteryZone : MonoBehaviour
    {
        [Header("Buff Config")]
        [Tooltip("ReverseBatteryConfig.asset：BuffType=Regeneration、RecoverTargetType=ReverseSystem。")]
        [SerializeField] private PlayerBuffConfigSO batteryBuffConfig;

        [Tooltip("作为 Buff 来源的 sourceType。一般填 Other 即可。")]
        [SerializeField] private PlayerBuffSourceType sourceType = PlayerBuffSourceType.Other;

        [Tooltip("是否在玩家离开时主动 RemoveBuff，实现\"走出区域立刻终止回血\"。" +
                 "关掉则任由 buff 自然过期。")]
        [SerializeField] private bool removeBuffOnExit = true;

        // 记录当前已在本区域内并已申请过 buff 的玩家，避免 OnTriggerStay 重复申请。
        private readonly HashSet<int> appliedPlayerIds = new HashSet<int>();

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (batteryBuffConfig == null) return;
            Player player = ResolvePlayer(other);
            if (player == null) return;
            TryApplyBuff(player);
        }

        private void OnTriggerStay(Collider other)
        {
            // 兜底：某些初始化/物理时序下，角色初始已在触发器内时可能不会立刻触发 Enter。
            // 用 Stay 保证"进入范围就开始恢复"语义，即便角色没有额外位移。
            if (batteryBuffConfig == null) return;
            Player player = ResolvePlayer(other);
            if (player == null) return;
            TryApplyBuff(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!removeBuffOnExit) return;
            if (batteryBuffConfig == null) return;
            Player player = ResolvePlayer(other);
            if (player == null) return;
            appliedPlayerIds.Remove(player.GetInstanceID());
            player.BuffSystem?.RemoveBuff(batteryBuffConfig.BuffId);
        }

        private void TryApplyBuff(Player player)
        {
            if (player == null || batteryBuffConfig == null) return;
            int id = player.GetInstanceID();
            if (appliedPlayerIds.Contains(id)) return;
            player.ApplyBuff(batteryBuffConfig, new PlayerBuffSourceContext(sourceType, gameObject));
            appliedPlayerIds.Add(id);
        }

        private static Player ResolvePlayer(Collider other)
        {
            if (other == null) return null;
            Player p = other.GetComponent<Player>();
            if (p == null) p = other.GetComponentInParent<Player>();
            return p;
        }
    }
}
