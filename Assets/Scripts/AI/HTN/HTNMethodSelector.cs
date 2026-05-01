using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using UnityEngine;

namespace DZ_3C.AI.HTN
{
    public enum RootBehavior
    {
        Idle,
        Combat,
        Retreat
    }

    public enum IdleMethod
    {
        Patrol,
        Checkpoint,
        AlertPatrol,
        EnergyAvoid
    }

    public enum CombatMethod
    {
        Interfere,
        Assault
    }

    [DisallowMultipleComponent]
    public class HTNMethodSelector : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;

        public RootBehavior CurrentRoot { get; private set; } = RootBehavior.Idle;
        public IdleMethod CurrentIdleMethod { get; private set; } = IdleMethod.Patrol;
        public CombatMethod CurrentCombatMethod { get; private set; } = CombatMethod.Assault;

        private bool retreatPending;
        private Transform retreatTargetPoint;
        private RootBehavior lastRoot = RootBehavior.Idle;
        private float nextCombatRollTime;
        private float combatMethodHoldUntil;

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
        }

        private void Update()
        {
            if (config == null || blackboard == null) return;
            SelectRootBehavior();
        }

        public void NotifyHateClearedEnterRetreat(Transform retreatTarget)
        {
            retreatPending = true;
            retreatTargetPoint = retreatTarget;
            blackboard.IsInRetreat = true;
        }

        public void NotifyRetreatArrived()
        {
            retreatPending = false;
            retreatTargetPoint = null;
            blackboard.IsInRetreat = false;
        }

        private void SelectRootBehavior()
        {
            if (blackboard.HateTarget != null)
            {
                CurrentRoot = RootBehavior.Combat;
                if (lastRoot != RootBehavior.Combat)
                {
                    nextCombatRollTime = 0f; // force an immediate roll when entering combat.
                    combatMethodHoldUntil = 0f;
                }
                SelectCombatMethod();
                lastRoot = CurrentRoot;
                return;
            }

            if (retreatPending && retreatTargetPoint != null)
            {
                CurrentRoot = RootBehavior.Retreat;
                lastRoot = CurrentRoot;
                return;
            }

            CurrentRoot = RootBehavior.Idle;
            SelectIdleMethod();
            lastRoot = CurrentRoot;
        }

        private void SelectIdleMethod()
        {
            if (blackboard.HeardTargets.Count > 0)
            {
                CurrentIdleMethod = IdleMethod.AlertPatrol;
                return;
            }

            if (blackboard.DesignatedCheckpoint != null)
            {
                CurrentIdleMethod = IdleMethod.Checkpoint;
                return;
            }

            if (blackboard.CurrentPositionEnergy > config.energyMinForAvoid)
            {
                CurrentIdleMethod = IdleMethod.EnergyAvoid;
                return;
            }

            CurrentIdleMethod = IdleMethod.Patrol;
        }

        private void SelectCombatMethod()
        {
            if (Time.time < nextCombatRollTime)
            {
                return;
            }
            if (Time.time < combatMethodHoldUntil)
            {
                return;
            }

            nextCombatRollTime = Time.time + Mathf.Max(0.05f, config.combatMethodRollInterval);

            float interfereChance = config.interfereChanceWhenNormalEnergy;
            if (blackboard.HateTarget != null && blackboard.HateTarget.IsPlayer)
            {
                float playerEnergy = blackboard.GetPlayerEnergy(blackboard.HateTarget.PlayerId);
                bool lowEnergy = playerEnergy < config.interfereEnergyThreshold;
                interfereChance = lowEnergy ? config.interfereChanceWhenLowEnergy : config.interfereChanceWhenNormalEnergy;
            }

            CombatMethod next = Random.value < interfereChance ? CombatMethod.Interfere : CombatMethod.Assault;
            if (next != CurrentCombatMethod)
            {
                CurrentCombatMethod = next;
                combatMethodHoldUntil = Time.time + Mathf.Max(0f, config.combatMethodMinHoldSeconds);
            }
            else if (combatMethodHoldUntil <= Time.time)
            {
                combatMethodHoldUntil = Time.time + Mathf.Max(0f, config.combatMethodMinHoldSeconds);
            }
        }
    }
}
