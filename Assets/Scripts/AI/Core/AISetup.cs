using System;
using System.Collections.Generic;
using System.Reflection;
using DZ_3C.AI.Config;
using DZ_3C.AI.Debugging;
using DZ_3C.AI.HTN;
using DZ_3C.AI.Perception;
using DZ_3C.AI.Threat;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    public class AISetup : MonoBehaviour
    {
        [Header("Required Config")]
        [SerializeField] private AIConfigSO config;
        [SerializeField] private MonsterStatConfigSO monsterStat;

        [Header("Optional Scene Links")]
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private List<PatrolPoint> patrolPoints = new();
        [SerializeField] private List<CheckpointCounter> checkpoints = new();

        [Header("Optional Behavior Params")]
        [SerializeField] private float patrolRadius = 20f;
        [SerializeField] private float patrolPrecision = 1.5f;
        [SerializeField] private float stayAtSpawnSeconds = 2f;
        [SerializeField] private float retreatArriveDistance = 1f;

        [ContextMenu("Auto Setup AI Components")]
        public void AutoSetup()
        {
            if (config == null)
            {
                Debug.LogError("[AISetup] Missing AIConfigSO reference.", this);
                return;
            }
            if (monsterStat == null)
            {
                Debug.LogWarning("[AISetup] MonsterStatConfigSO is not assigned. Driver will be added but disabled by missing config.", this);
            }

            AIBlackboard blackboard = GetOrAdd<AIBlackboard>();
            HTNMethodSelector selector = GetOrAdd<HTNMethodSelector>();
            MonsterCharacter monsterCharacter = GetOrAdd<MonsterCharacter>();
            MonsterAICharacterDriver driver = GetOrAdd<MonsterAICharacterDriver>();
            MonsterAttackRelay attackRelay = GetOrAdd<MonsterAttackRelay>();

            VisionPerceptor vision = GetOrAdd<VisionPerceptor>();
            HearingPerceptor hearing = GetOrAdd<HearingPerceptor>();
            DistancePerceptor distance = GetOrAdd<DistancePerceptor>();
            HitPerceptor hit = GetOrAdd<HitPerceptor>();
            EnergyPerceptor energy = GetOrAdd<EnergyPerceptor>();

            ThreatResolver threat = GetOrAdd<ThreatResolver>();
            PatrolPlanner patrol = GetOrAdd<PatrolPlanner>();
            RetreatController retreat = GetOrAdd<RetreatController>();
            AIBehaviorRuntime runtime = GetOrAdd<AIBehaviorRuntime>();
            AIPerceptionDebugGizmos gizmos = GetOrAdd<AIPerceptionDebugGizmos>();
            AIOverheadRuntimeUI overheadUI = GetOrAdd<AIOverheadRuntimeUI>();

            // Base links.
            BindCommon(vision, blackboard);
            BindCommon(hearing, blackboard);
            BindCommon(distance, blackboard);
            BindCommon(energy, blackboard);
            SetField(hit, "blackboard", blackboard);
            SetField(hit, "owner", transform);
            SetField(threat, "config", config);
            SetField(threat, "blackboard", blackboard);
            SetField(threat, "owner", transform);
            SetField(selector, "config", config);
            SetField(selector, "blackboard", blackboard);
            SetField(monsterCharacter, "statConfig", monsterStat);
            SetField(driver, "monsterStat", monsterStat);
            SetField(driver, "blackboard", blackboard);
            SetField(driver, "selector", selector);
            SetField(driver, "threatResolver", threat);
            SetField(driver, "character", monsterCharacter);
            SetField(driver, "attackHandler", attackRelay);
            SetField(runtime, "blackboard", blackboard);
            SetField(runtime, "selector", selector);
            SetField(gizmos, "config", config);
            SetField(gizmos, "blackboard", blackboard);
            SetField(overheadUI, "selector", selector);
            SetField(overheadUI, "runtime", runtime);

            // Patrol links.
            SetField(patrol, "config", config);
            SetField(patrol, "blackboard", blackboard);
            SetField(patrol, "spawnCenter", spawnCenter != null ? spawnCenter : transform);
            SetField(patrol, "patrolRadius", patrolRadius);
            SetField(patrol, "patrolPrecision", patrolPrecision);
            SetField(patrol, "stayAtSpawnSeconds", stayAtSpawnSeconds);
            SetField(patrol, "patrolPoints", patrolPoints);
            SetField(patrol, "checkpoints", checkpoints);

            // Retreat links.
            SetField(retreat, "config", config);
            SetField(retreat, "blackboard", blackboard);
            SetField(retreat, "selector", selector);
            SetField(retreat, "patrolPoints", patrolPoints);
            SetField(retreat, "arriveDistance", retreatArriveDistance);

            Debug.Log("[AISetup] AI components auto setup completed.", this);
        }

        private void BindCommon(Component perceptor, AIBlackboard blackboard)
        {
            SetField(perceptor, "config", config);
            SetField(perceptor, "blackboard", blackboard);
            SetField(perceptor, "owner", transform);
        }

        private T GetOrAdd<T>() where T : Component
        {
            T existing = GetComponent<T>();
            if (existing != null) return existing;
            return gameObject.AddComponent<T>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            Type type = target.GetType();
            FieldInfo field = null;

            while (type != null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null) break;
                type = type.BaseType;
            }

            if (field == null) return;
            if (value != null && !field.FieldType.IsInstanceOfType(value)) return;
            field.SetValue(target, value);
        }
    }
}

