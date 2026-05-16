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
        /// <summary>感知权重（听觉强度等）；非听觉感知可保持为 0。</summary>
        public float intensity;

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

        [Header("Heard Focus")]
        [SerializeField] private bool hasHeardFocus;
        [SerializeField] private Vector3 heardFocusWorldPosition;
        [SerializeField] private Vector3 heardFocusDirection = Vector3.forward;
        [SerializeField] private float heardFocusIntensity;
        [SerializeField] private float heardFocusLastUpdatedTime;

        private readonly Dictionary<int, float> playerEnergyById = new();

        public IReadOnlyList<TargetFact> InSightTargets => inSightTargets;
        public IReadOnlyList<TargetFact> HeardTargets => heardTargets;
        public IReadOnlyList<TargetFact> ContactTargets => contactTargets;
        public IReadOnlyList<TargetFact> Attackers => attackers;
        public IReadOnlyList<TargetFact> EnergyTargets => energyTargets;
        public IReadOnlyDictionary<int, float> PlayerEnergyById => playerEnergyById;

        public bool HasHeardFocus => hasHeardFocus;
        public Vector3 HeardFocusWorldPosition => heardFocusWorldPosition;
        public Vector3 HeardFocusDirection => heardFocusDirection;
        public float HeardFocusIntensity => heardFocusIntensity;
        public float HeardFocusLastUpdatedTime => heardFocusLastUpdatedTime;

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

        public void SetHeardFocus(Vector3 worldPosition, Vector3 planarDirection, float totalIntensity, float updatedTime)
        {
            hasHeardFocus = true;
            heardFocusWorldPosition = worldPosition;
            heardFocusDirection = planarDirection.sqrMagnitude > 0.0001f ? planarDirection.normalized : Vector3.forward;
            heardFocusIntensity = Mathf.Max(0f, totalIntensity);
            heardFocusLastUpdatedTime = updatedTime;
        }

        public void ClearHeardFocus()
        {
            if (!hasHeardFocus) return;

            hasHeardFocus = false;
            heardFocusWorldPosition = Vector3.zero;
            heardFocusDirection = Vector3.forward;
            heardFocusIntensity = 0f;
            heardFocusLastUpdatedTime = 0f;
        }

        private List<TargetFact> GetTargetBucket(ThreatSource source)
        {
            if (source == ThreatSource.Contact)
            {
                return contactTargets;
            }

            if (source == ThreatSource.Attacker)
            {
                return attackers;
            }

            if (source == ThreatSource.Sight)
            {
                return inSightTargets;
            }

            return heardTargets;
        }

    }
}
