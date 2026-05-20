using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class AIEnergySource : MonoBehaviour
    {
        [Min(0f)] public float energy = 1f;
    }
}
