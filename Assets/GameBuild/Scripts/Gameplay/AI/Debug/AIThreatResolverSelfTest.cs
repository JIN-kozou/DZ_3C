using System.Collections;
using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using DZ_3C.AI.Threat;
using UnityEngine;

namespace DZ_3C.AI.Debugging
{
    [DisallowMultipleComponent]
    public class AIThreatResolverSelfTest : MonoBehaviour
    {
        [SerializeField] private AIConfigSO config;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private ThreatResolver resolver;
        [SerializeField] private AITargetable targetA;
        [SerializeField] private AITargetable targetB;

        [ContextMenu("Run Threat Self Test")]
        public void RunSelfTest()
        {
            StartCoroutine(RunRoutine());
        }

        private IEnumerator RunRoutine()
        {
            if (config == null || blackboard == null || resolver == null || targetA == null || targetB == null)
            {
                Debug.LogError("[AIThreatResolverSelfTest] Missing references.");
                yield break;
            }

            float now = Time.time;
            blackboard.SetTargets(new System.Collections.Generic.List<TargetFact>
            {
                new TargetFact { target = targetA, distance = 0.5f, timestamp = now, source = ThreatSource.Contact }
            }, ThreatSource.Contact);
            yield return null;
            AssertTrue(blackboard.HateTarget == targetA, "Contact target should be selected first");

            blackboard.SetTargets(new System.Collections.Generic.List<TargetFact>
            {
                new TargetFact { target = targetB, distance = 0.1f, timestamp = Time.time, source = ThreatSource.Sight }
            }, ThreatSource.Sight);
            yield return null;
            AssertTrue(blackboard.HateTarget == targetA, "Lower priority sight target should not override lock");

            targetA.SetAlive(false);
            yield return null;
            AssertTrue(blackboard.HateTarget == null, "Dead hate target should be cleared");

            Debug.Log("[AIThreatResolverSelfTest] Completed.");
        }

        private static void AssertTrue(bool ok, string message)
        {
            if (!ok) Debug.LogError("[AIThreatResolverSelfTest] FAIL: " + message);
            else Debug.Log("[AIThreatResolverSelfTest] PASS: " + message);
        }
    }
}
