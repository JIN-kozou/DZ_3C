using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    public class ReverseCoreStackEnergyProvider : MonoBehaviour, IAIReverseEnergyProvider
    {
        [SerializeField] private ReverseCoreStack coreStack;

        public float ReverseEnergy
        {
            get
            {
                if (coreStack == null) return 0f;

                float total = 0f;
                if (coreStack.Anchor != null)
                {
                    total += coreStack.Anchor.CurrentHealth;
                }

                var cores = coreStack.Cores;
                for (int i = 0; i < cores.Count; i++)
                {
                    if (cores[i] != null) total += cores[i].Health;
                }

                return total;
            }
        }

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
        }
    }
}
