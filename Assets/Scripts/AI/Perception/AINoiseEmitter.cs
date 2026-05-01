using UnityEngine;

namespace DZ_3C.AI.Perception
{
    [DisallowMultipleComponent]
    public class AINoiseEmitter : MonoBehaviour
    {
        [Min(0f)] public float loudness = 1f;
        public bool isEmitting = true;
    }
}
