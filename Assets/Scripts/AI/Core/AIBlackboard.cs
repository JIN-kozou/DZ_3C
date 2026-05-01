using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    public enum ThreatSource
    {
        None = 0,
        Contact = 1,
        Attacker = 2,
        Sight = 3
    }

    [Serializable]
    public struct TargetFact
    {
        public AITargetable target;
        public float distance;
        public float timestamp;
        public ThreatSource source;

        public bool IsValid => target != null;
    }

    [DisallowMultipleComponent]
    public class AIBlackboard : MonoBehaviour
    {
        [SerializeField] private List<TargetFact> inSightTargets = new();
        [SerializeField] private List<TargetFact> heardTargets = new();
        [SerializeField] private List<TargetFact> contactTargets = new();
        [SerializeField] private List<TargetFact> attackers = new();
        [SerializeField] private List<TargetFact> energyTargets = new();

        private readonly Dictionary<int, float> playerEnergyById = new();

        public IReadOnlyList<TargetFact> InSightTargets => inSightTargets;
        public IReadOnlyList<TargetFact> HeardTargets => heardTargets;
        public IReadOnlyList<TargetFact> ContactTargets => contactTargets;
        public IReadOnlyList<TargetFact> Attackers => attackers;
        public IReadOnlyList<TargetFact> EnergyTargets => energyTargets;
        public IReadOnlyDictionary<int, float> PlayerEnergyById => playerEnergyById;

        public AITargetable HateTarget { get; private set; }
        public ThreatSource HateSource { get; private set; }
        public int HatePriority { get; private set; }
        public float HateLockedUntil { get; private set; }

        public float OutOfSightElapsed { get; set; }
        public float AssaultTaskElapsed { get; set; }
        public float CurrentPositionEnergy { get; set; }

        public Transform PatrolTarget { get; set; }
        public Transform NextPatrolTarget { get; set; }
        public Transform DesignatedCheckpoint { get; set; }

        public bool IsInCombat { get; set; }
        public bool IsInRetreat { get; set; }
        public bool IsInHateProtection { get; set; }

        public event Action OnPerceptionUpdated;
        public event Action<AITargetable> OnHateTargetChanged;

        public void SetTargets(List<TargetFact> values, ThreatSource source)
        {
            List<TargetFact> dst = GetTargetBucket(source);
            dst.Clear();
            if (values != null) dst.AddRange(values);
            OnPerceptionUpdated?.Invoke();
        }

        public void SetEnergyTargets(List<TargetFact> values)
        {
            energyTargets.Clear();
            if (values != null) energyTargets.AddRange(values);
            OnPerceptionUpdated?.Invoke();
        }

        public void UpdatePlayerEnergy(int playerId, float energyValue)
        {
            playerEnergyById[playerId] = energyValue;
        }

        public float GetPlayerEnergy(int playerId)
        {
            return playerEnergyById.TryGetValue(playerId, out float value) ? value : 0f;
        }

        public void SetHateTarget(AITargetable target, ThreatSource source, int priority, float lockedUntil)
        {
            if (HateTarget == target && HateSource == source && HatePriority == priority)
            {
                HateLockedUntil = lockedUntil;
                return;
            }

            HateTarget = target;
            HateSource = source;
            HatePriority = priority;
            HateLockedUntil = lockedUntil;
            OnHateTargetChanged?.Invoke(target);
        }

        public void ClearHateTarget()
        {
            SetHateTarget(null, ThreatSource.None, 0, 0f);
            OutOfSightElapsed = 0f;
            AssaultTaskElapsed = 0f;
        }

        private List<TargetFact> GetTargetBucket(ThreatSource source)
        {
            return source switch
            {
                ThreatSource.Contact => contactTargets,
                ThreatSource.Attacker => attackers,
                ThreatSource.Sight => inSightTargets,
                _ => heardTargets
            };
        }

    }
}
