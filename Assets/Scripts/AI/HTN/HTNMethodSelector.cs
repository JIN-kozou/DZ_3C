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
        /// <summary>进入「有声音目标」状态的起始时刻；无声音时为 -1。</summary>
        private float alertPatrolPhaseStartTime = -1f;

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
                alertPatrolPhaseStartTime = -1f;
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
            bool hasHeard = blackboard.HasHeardFocus;
            if (!hasHeard)
            {
                alertPatrolPhaseStartTime = -1f;
            }
            else
            {
                if (alertPatrolPhaseStartTime < 0f)
                {
                    alertPatrolPhaseStartTime = Time.time;
                }

                bool alertPriorityUnlimited = config.alertPatrolPrioritySeconds <= 0f;
                bool withinAlertPriorityWindow =
                    alertPriorityUnlimited ||
                    Time.time - alertPatrolPhaseStartTime < config.alertPatrolPrioritySeconds;

                if (withinAlertPriorityWindow)
                {
                    CurrentIdleMethod = IdleMethod.AlertPatrol;
                    return;
                }
            }

            if (blackboard.DesignatedCheckpoint != null)
            {
                CurrentIdleMethod = IdleMethod.Checkpoint;
                return;
            }

            float panicThreshold = Mathf.Max(config.energyPanicThreshold, config.energyMinForAvoid + 0.01f);
            if (blackboard.CurrentPositionEnergy >= panicThreshold)
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
