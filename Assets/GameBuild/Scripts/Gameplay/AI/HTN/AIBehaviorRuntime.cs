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
            if (selector.CurrentRoot == RootBehavior.Combat)
            {
                return selector.CurrentCombatMethod == CombatMethod.Interfere
                    ? "OrbitAndStrafe"
                    : "DashAndAOEAttack";
            }

            if (selector.CurrentRoot == RootBehavior.Retreat)
            {
                return "MoveToRetreatTarget";
            }

            if (selector.CurrentIdleMethod == IdleMethod.AlertPatrol)
            {
                return "DescendAndMoveToHeardTarget";
            }

            if (selector.CurrentIdleMethod == IdleMethod.Checkpoint)
            {
                return "MoveAndOrbitCheckpoint";
            }

            if (selector.CurrentIdleMethod == IdleMethod.EnergyAvoid)
            {
                return "TurnAndAvoidEnergy";
            }

            return "PatrolMoveAndPause";
        }
    }
}
