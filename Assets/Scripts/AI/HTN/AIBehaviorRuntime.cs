using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    [DisallowMultipleComponent]
    public class AIBehaviorRuntime : MonoBehaviour
    {
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private HTNMethodSelector selector;

        public string CurrentAtomicTask { get; private set; }

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
        }

        private void Update()
        {
            if (selector == null || blackboard == null) return;
            CurrentAtomicTask = ResolveTaskName();
        }

        private string ResolveTaskName()
        {
            return selector.CurrentRoot switch
            {
                RootBehavior.Combat => selector.CurrentCombatMethod == CombatMethod.Interfere ? "OrbitAndStrafe" : "DashAndAOEAttack",
                RootBehavior.Retreat => "MoveToRetreatTarget",
                _ => selector.CurrentIdleMethod switch
                {
                    IdleMethod.AlertPatrol => "DescendAndMoveToHeardTarget",
                    IdleMethod.Checkpoint => "MoveAndOrbitCheckpoint",
                    IdleMethod.EnergyAvoid => "TurnAndAvoidEnergy",
                    _ => "PatrolMoveAndPause"
                }
            };
        }
    }
}
