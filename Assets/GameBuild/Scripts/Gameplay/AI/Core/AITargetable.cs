using UnityEngine;

namespace DZ_3C.AI.Core
{
    public interface IAIAliveProvider
    {
        bool IsAlive { get; }
    }

    public interface IAIReverseEnergyProvider
    {
        float ReverseEnergy { get; }
    }

    [DisallowMultipleComponent]
    public class AITargetable : MonoBehaviour, IAIAliveProvider
    {
        [SerializeField] private int playerId = -1;
        [SerializeField] private bool isPlayer = true;
        [SerializeField] private bool isAlive = true;
        [SerializeField] private float fallbackReverseEnergy;

        public int PlayerId => playerId >= 0 ? playerId : GetInstanceID();
        public bool IsPlayer => isPlayer;
        public bool IsAlive => isAlive;
        public float FallbackReverseEnergy => fallbackReverseEnergy;

        public void SetAlive(bool value) => isAlive = value;
        public void SetFallbackReverseEnergy(float value) => fallbackReverseEnergy = value;
    }
}
