using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.Debugging
{
    [DisallowMultipleComponent]
    public class AIPerceptionDebugGizmos : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private MonsterStatConfigSO monsterStat;
        [SerializeField] private AIBlackboard blackboard;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (monsterStat == null)
            {
                var character = GetComponent<MonsterCharacter>();
                if (character != null) monsterStat = character.StatConfig;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            if (monsterStat == null)
            {
                var character = GetComponent<MonsterCharacter>();
                if (character != null) monsterStat = character.StatConfig;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, config.contactDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, config.hearingDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.visionRadius);

            Vector3 left = Quaternion.Euler(0f, -config.visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, config.visionAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + left * config.visionRadius);
            Gizmos.DrawLine(transform.position, transform.position + right * config.visionRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, config.energyDetectRadius);

            if (monsterStat != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // orange: attack trigger radius.
                Gizmos.DrawWireSphere(transform.position, monsterStat.combatAttackDistance);

                Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f); // red-ish: aoe damage radius.
                Gizmos.DrawWireSphere(transform.position, monsterStat.aoeRadius);
            }

            if (blackboard != null && blackboard.HateTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, blackboard.HateTarget.transform.position);
            }
        }
    }
}
