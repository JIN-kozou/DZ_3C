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
            player.ApplyBuff(batteryBuffConfig, new PlayerBuffSourceContext(sourceType, gameObject));
        }

        private void OnTriggerExit(Collider other)
        {
            if (!removeBuffOnExit) return;
            if (batteryBuffConfig == null) return;
            Player player = ResolvePlayer(other);
            if (player == null) return;
            player.BuffSystem?.RemoveBuff(batteryBuffConfig.BuffId);
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
