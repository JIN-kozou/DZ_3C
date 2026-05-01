using UnityEngine;
using UnityEngine.Events;

namespace DZ_3C.AI.Core
{
    [System.Serializable]
    public class MonsterAttackEvent : UnityEvent<GameObject, float, float, string>
    {
    }

    [DisallowMultipleComponent]
    public class MonsterAttackRelay : MonoBehaviour, IMonsterAttack
    {
        [SerializeField] private MonsterAttackEvent onAttack;

        public void PerformAttack(AITargetable target, float baseDamage, float aoeRadius, string buffId)
        {
            if (target == null) return;
            onAttack?.Invoke(target.gameObject, baseDamage, aoeRadius, buffId);
        }
    }
}
