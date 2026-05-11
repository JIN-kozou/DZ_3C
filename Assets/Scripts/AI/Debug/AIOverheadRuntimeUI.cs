using DZ_3C.AI.Config;
using DZ_3C.AI.Core;
using DZ_3C.AI.HTN;
using TMPro;
using UnityEngine;

namespace DZ_3C.AI.Debugging
{
    [DisallowMultipleComponent]
    public class AIOverheadRuntimeUI : MonoBehaviour
    {
        [SerializeField] private HTNMethodSelector selector;
        [SerializeField] private AIBehaviorRuntime runtime;
        [SerializeField] private AIBlackboard blackboard;
        [SerializeField] private AIConfigSO aiConfig;
        [SerializeField] private MonsterAICharacterDriver driver;
        [SerializeField] private MonsterHurtReceiver hurtReceiver;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float textScale = 0.12f;

        private TextMeshPro textMeshPro;
        private Transform cameraTransform;

        private void Awake()
        {
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
            if (runtime == null) runtime = GetComponent<AIBehaviorRuntime>();
            if (blackboard == null) blackboard = GetComponent<AIBlackboard>();
            if (driver == null) driver = GetComponent<MonsterAICharacterDriver>();
            if (hurtReceiver == null) hurtReceiver = GetComponent<MonsterHurtReceiver>();
            EnsureTMPText();
        }

        private void LateUpdate()
        {
            if (selector == null || runtime == null) return;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            Transform tr = textMeshPro != null ? textMeshPro.transform : transform;
            tr.position = transform.position + worldOffset;
            if (cameraTransform != null)
            {
                tr.forward = cameraTransform.forward;
            }

            string root = driver != null ? driver.CurrentRootDebug : selector.CurrentRoot.ToString();
            string method = driver != null
                ? driver.CurrentMethodDebug
                : selector.CurrentRoot switch
                {
                    RootBehavior.Combat => selector.CurrentCombatMethod.ToString(),
                    RootBehavior.Retreat => "RetreatMove",
                    _ => selector.CurrentIdleMethod.ToString()
                };
            string atomic = driver != null
                ? driver.CurrentAtomicTaskDebug
                : (string.IsNullOrEmpty(runtime.CurrentAtomicTask) ? "None" : runtime.CurrentAtomicTask);

            string hpLine = BuildHealthLine();
            string energyLine = BuildEnergyLine();
            string content = $"Root: {root}\nMethod: {method}\nAtomic: {atomic}\n{energyLine}\n{hpLine}";
            if (textMeshPro != null) textMeshPro.text = content;
        }

        private string BuildEnergyLine()
        {
            if (blackboard == null)
            {
                return "Energy: — (no blackboard)";
            }

            float e = blackboard.CurrentPositionEnergy;
            if (aiConfig != null)
            {
                return $"Energy: {e:0.###}  (avoid if > {aiConfig.energyMinForAvoid:0.###})";
            }

            return $"Energy: {e:0.###}";
        }

        private string BuildHealthLine()
        {
            if (hurtReceiver == null)
            {
                return "HP: —";
            }

            if (hurtReceiver.IsDead)
            {
                return $"HP: 0 / {hurtReceiver.MaxHealth:0} (dead)";
            }

            return $"HP: {hurtReceiver.CurrentHealth:0} / {hurtReceiver.MaxHealth:0}";
        }

        private void EnsureTMPText()
        {
            Transform existing = transform.Find("AIRuntimeTMP");
            if (existing != null)
            {
                textMeshPro = existing.GetComponent<TextMeshPro>();
                return;
            }

            GameObject go = new GameObject("AIRuntimeTMP");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = worldOffset;
            go.transform.localScale = Vector3.one * textScale;

            textMeshPro = go.AddComponent<TextMeshPro>();
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.fontSize = 4f;
            textMeshPro.color = Color.yellow;
            textMeshPro.enableWordWrapping = false;
        }
    }
}
