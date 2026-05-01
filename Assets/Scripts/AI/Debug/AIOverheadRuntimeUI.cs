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
        [SerializeField] private MonsterAICharacterDriver driver;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float textScale = 0.12f;

        private TextMeshPro textMeshPro;
        private Transform cameraTransform;

        private void Awake()
        {
            if (selector == null) selector = GetComponent<HTNMethodSelector>();
            if (runtime == null) runtime = GetComponent<AIBehaviorRuntime>();
            if (driver == null) driver = GetComponent<MonsterAICharacterDriver>();
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

            string content = $"Root: {root}\nMethod: {method}\nAtomic: {atomic}";
            if (textMeshPro != null) textMeshPro.text = content;
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
