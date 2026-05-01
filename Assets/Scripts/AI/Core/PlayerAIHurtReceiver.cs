using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    public class PlayerAIHurtReceiver : MonoBehaviour, IAIHurtReceiver
    {
        [SerializeField] private ReverseCoreStack reverseCoreStack;
        [SerializeField] private Player player;

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
                return;
            }

            if (player != null && player.ReusableData != null)
            {
                player.ReusableData.health.Value = Mathf.Max(0f, player.ReusableData.health.Value - damage);
            }
        }
    }
}
