using DZ_3C.Reverse;
using UnityEngine;

/// <summary>
/// 触发器击杀区：玩家进入时通过 <see cref="ReverseCoreStack.ApplyDamage"/> 造成致命伤害并走正常死亡/复活流程。
/// 挂在 Deadplane 等物体上，Collider 需勾选 Is Trigger。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PlayerKillZone : MonoBehaviour
{
    [Tooltip("造成的伤害量，需足以清空核心与锚血。")]
    [Min(0f)]
    [SerializeField] private float damageAmount = 100000f;

    [Tooltip("为 true 时忽略复活后的短暂无敌，坠落/即死区应勾选。")]
    [SerializeField] private bool bypassRespawnInvincibility = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryResolvePlayer(other, out Player player))
        {
            return;
        }

        ReverseCoreStack coreStack = player.GetComponent<ReverseCoreStack>();
        if (coreStack == null)
        {
            return;
        }

        ReverseAnchor anchor = coreStack.Anchor;
        if (anchor != null && anchor.IsDead)
        {
            return;
        }

        if (bypassRespawnInvincibility && coreStack.IsInvincible)
        {
            coreStack.ClearRespawnInvincibility();
        }

        coreStack.ApplyDamage(damageAmount);
    }

    private static bool TryResolvePlayer(Collider other, out Player player)
    {
        player = null;
        if (other == null)
        {
            return false;
        }

        player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        return player != null;
    }
}
