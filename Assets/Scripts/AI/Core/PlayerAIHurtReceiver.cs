using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    public class PlayerAIHurtReceiver : MonoBehaviour, IAIHurtReceiver
    {
        [SerializeField] private ReverseCoreStack reverseCoreStack;
        [SerializeField] private Player player;
        [Header("Hit Visual Buff (optional)")]
        [SerializeField] private PlayerBuffConfigSO hurtVisualBuffConfig;
        [SerializeField] private PlayerBuffSourceType hurtBuffSourceType = PlayerBuffSourceType.Monster;

        private void Awake()
        {
            if (reverseCoreStack == null) reverseCoreStack = GetComponent<ReverseCoreStack>();
            if (player == null) player = GetComponent<Player>();
        }

        public void ReceiveAIDamage(float damage, string buffId, object attacker)
        {
            if (damage <= 0f) return;

            if (reverseCoreStack != null)
            {
                reverseCoreStack.ApplyDamage(damage);
                ApplyHurtVisualBuff(attacker);
                return;
            }

            if (player != null && player.ReusableData != null)
            {
                player.ReusableData.health.Value = Mathf.Max(0f, player.ReusableData.health.Value - damage);
                ApplyHurtVisualBuff(attacker);
            }
        }

        private void ApplyHurtVisualBuff(object attacker)
        {
            if (hurtVisualBuffConfig == null || player == null) return;
            GameObject sourceObject = ResolveSourceObject(attacker);
            player.ApplyBuff(hurtVisualBuffConfig, new PlayerBuffSourceContext(hurtBuffSourceType, sourceObject));
        }

        private static GameObject ResolveSourceObject(object attacker)
        {
            if (attacker is GameObject go) return go;
            if (attacker is Component component) return component.gameObject;
            return null;
        }
    }
}
