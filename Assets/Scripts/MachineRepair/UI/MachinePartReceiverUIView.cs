using System.Collections;

using System.Collections.Generic;

using DZ_3C.MachineRepair;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace DZ_3C.MachineRepair.UI

{

    /// <summary>

    /// World-space receiver panel: show on player proximity, refresh requirement lines and repair status.

    /// Wire under MachinePartReceiver/ReceiverUI; keep uiRoot inactive in the scene until the player enters range.

    /// </summary>

    [DisallowMultipleComponent]

    public class MachinePartReceiverUIView : MonoBehaviour

    {

        private const int MaxRequirementLines = 4;

        private const string StatusPending = "仍待修复";

        private const string StatusSuccess = "修复成功";

        private const string MarkSatisfied = "\u2611";

        private const string MarkPending = "\u2610";

        private static readonly Color DefaultSatisfiedColor = new Color(0.33823532f, 0.72356665f, 1f, 1f);



        [SerializeField] private GameObject uiRoot;

        [SerializeField] private Text[] requirementLines = new Text[MaxRequirementLines];

        [SerializeField] private TextMeshProUGUI statusTextTmp;

        [SerializeField] private Text satisfiedStyleReference;

        [SerializeField] private TextMeshProUGUI satisfiedStyleReferenceTmp;

        [SerializeField] private Color pendingTextColor = Color.white;



        private InterfaceAnimManager panelIam;

        private Color satisfiedTextColor = DefaultSatisfiedColor;

        private MachinePartReceiver coroutineHost;

        private Coroutine visibilityRoutine;

        private bool isVisible;

        private RequirementLineLayoutSnapshot[] requirementLineLayouts;



        private struct RequirementLineLayoutSnapshot

        {

            public bool captured;

            public int fontSize;

            public float lineSpacing;

            public TextAnchor alignment;

            public HorizontalWrapMode horizontalOverflow;

            public VerticalWrapMode verticalOverflow;

            public bool resizeTextForBestFit;

            public int resizeTextMinSize;

            public int resizeTextMaxSize;

            public Vector2 sizeDelta;

        }



        private void Awake()

        {

            if (uiRoot == null)

            {

                uiRoot = gameObject;

            }



            coroutineHost = GetComponentInParent<MachinePartReceiver>();

            TryAutoBindReferences();

            ResolveSatisfiedColor();

            CacheRequirementLineLayouts();



            panelIam = uiRoot.GetComponent<InterfaceAnimManager>();

            if (panelIam != null)

            {

                panelIam.autoStart = false;

            }



            if (uiRoot != gameObject && !uiRoot.activeSelf)

            {

                return;

            }



            if (uiRoot.activeSelf)

            {

                uiRoot.SetActive(false);

            }

        }



        private void Reset()

        {

            TryAutoBindReferences();

        }



        public void TryAutoBindReferences()

        {

            if (uiRoot == null)

            {

                uiRoot = gameObject;

            }



            Transform searchRoot = transform.Find("Panel");

            if (searchRoot == null)

            {

                searchRoot = transform;

            }



            if (statusTextTmp == null)

            {

                Transform status = searchRoot.Find("winorLose");

                if (status != null)

                {

                    statusTextTmp = status.GetComponent<TextMeshProUGUI>();

                }

            }



            List<Text> amounts = CollectRequirementLineTexts(searchRoot);

            for (int i = 0; i < MaxRequirementLines; i++)

            {

                requirementLines[i] = i < amounts.Count ? amounts[i] : null;

            }



            if (satisfiedStyleReference == null && requirementLines[0] != null)

            {

                satisfiedStyleReference = requirementLines[0];

            }



            if (satisfiedStyleReferenceTmp == null && requirementLines[0] != null)

            {

                satisfiedStyleReferenceTmp = requirementLines[0].GetComponent<TextMeshProUGUI>();

            }



            ResolveSatisfiedColor();

            CacheRequirementLineLayouts();

        }



        private void CacheRequirementLineLayouts()

        {

            if (requirementLineLayouts == null || requirementLineLayouts.Length != MaxRequirementLines)

            {

                requirementLineLayouts = new RequirementLineLayoutSnapshot[MaxRequirementLines];

            }



            for (int i = 0; i < MaxRequirementLines; i++)

            {

                requirementLineLayouts[i] = CaptureLayout(requirementLines[i]);

            }

        }



        private static RequirementLineLayoutSnapshot CaptureLayout(Text line)

        {

            var snapshot = new RequirementLineLayoutSnapshot();

            if (line == null)

            {

                return snapshot;

            }



            RectTransform rect = line.rectTransform;

            snapshot.captured = true;

            snapshot.fontSize = line.fontSize;

            snapshot.lineSpacing = line.lineSpacing;

            snapshot.alignment = line.alignment;

            snapshot.horizontalOverflow = line.horizontalOverflow;

            snapshot.verticalOverflow = line.verticalOverflow;

            snapshot.resizeTextForBestFit = line.resizeTextForBestFit;

            snapshot.resizeTextMinSize = line.resizeTextMinSize;

            snapshot.resizeTextMaxSize = line.resizeTextMaxSize;

            snapshot.sizeDelta = rect.sizeDelta;

            return snapshot;

        }



        private void RestoreLayout(Text line, int slotIndex)

        {

            if (line == null || requirementLineLayouts == null || slotIndex < 0 || slotIndex >= requirementLineLayouts.Length)

            {

                return;

            }



            RequirementLineLayoutSnapshot snapshot = requirementLineLayouts[slotIndex];

            if (!snapshot.captured)

            {

                return;

            }



            line.fontSize = snapshot.fontSize;

            line.lineSpacing = snapshot.lineSpacing;

            line.alignment = snapshot.alignment;

            line.horizontalOverflow = snapshot.horizontalOverflow;

            line.verticalOverflow = snapshot.verticalOverflow;

            line.resizeTextForBestFit = snapshot.resizeTextForBestFit;

            line.resizeTextMinSize = snapshot.resizeTextMinSize;

            line.resizeTextMaxSize = snapshot.resizeTextMaxSize;

            line.rectTransform.sizeDelta = snapshot.sizeDelta;

        }



        private void ResolveSatisfiedColor()

        {

            if (satisfiedStyleReferenceTmp != null)

            {

                satisfiedTextColor = satisfiedStyleReferenceTmp.color;

                return;

            }



            if (satisfiedStyleReference != null)

            {

                satisfiedTextColor = satisfiedStyleReference.color;

            }

        }



        public void Refresh(MachinePartReceiver receiver)

        {

            if (receiver == null)

            {

                return;

            }



            List<MachinePartReceiver.PartRequirement> requirements = receiver.GetRequirementsInDisplayOrder();

            int requirementCount = requirements.Count;

            if (requirementCount > MaxRequirementLines)

            {

                Debug.LogWarning(

                    $"[MachineRepair] Receiver '{receiver.name}' has {requirementCount} requirements; UI shows first {MaxRequirementLines} only.",

                    receiver);

            }



            int lineIndex = 0;

            for (int r = 0; r < requirements.Count && lineIndex < MaxRequirementLines; r++)

            {

                MachinePartReceiver.PartRequirement req = requirements[r];

                ApplyRequirementLine(requirementLines[lineIndex], lineIndex, req, receiver.IsLineSatisfied(req));

                lineIndex++;

            }



            for (int i = lineIndex; i < MaxRequirementLines; i++)

            {

                ClearRequirementLine(requirementLines[i]);

            }



            string status = receiver.AreAllRequirementsSatisfied() ? StatusSuccess : StatusPending;

            if (statusTextTmp != null)

            {

                statusTextTmp.text = status;

            }

        }



        public void SetVisible(bool visible)

        {

            if (isVisible == visible)

            {

                if (!visible || (uiRoot != null && uiRoot.activeInHierarchy))

                {

                    return;

                }

            }



            isVisible = visible;



            if (visibilityRoutine != null)

            {

                StopVisibilityRoutine();

            }



            MonoBehaviour host = GetCoroutineHost();

            if (host == null)

            {

                ApplyVisibilityImmediate(visible);

                return;

            }



            visibilityRoutine = host.StartCoroutine(ApplyVisibilityRoutine(visible));

        }



        private MonoBehaviour GetCoroutineHost()

        {

            if (coroutineHost != null)

            {

                return coroutineHost;

            }



            coroutineHost = GetComponentInParent<MachinePartReceiver>();

            return coroutineHost;

        }



        private void StopVisibilityRoutine()

        {

            MonoBehaviour host = GetCoroutineHost();

            if (host != null && visibilityRoutine != null)

            {

                host.StopCoroutine(visibilityRoutine);

            }



            visibilityRoutine = null;

        }



        private void ApplyVisibilityImmediate(bool visible)

        {

            if (uiRoot == null)

            {

                return;

            }



            if (visible)

            {

                uiRoot.SetActive(true);

                ShowPanelWithIam();

            }

            else

            {

                HidePanelWithIam();

            }

        }



        private void EnsurePanelIam()

        {

            if (panelIam == null && uiRoot != null)

            {

                panelIam = uiRoot.GetComponent<InterfaceAnimManager>();

                if (panelIam != null)

                {

                    panelIam.autoStart = false;

                }

            }

        }



        private void ShowPanelWithIam()

        {

            EnsurePanelIam();

            if (panelIam == null)

            {

                return;

            }

            panelIam.autoStart = false;

            if (panelIam.currentState == CSFHIAnimableState.disappearing

                || panelIam.currentState == CSFHIAnimableState.appearing)

            {

                panelIam.startDisappear(true);

            }

            MachineRepairInventoryIamSync.ResetAllElementsForAppear(panelIam);

            panelIam.startAppear(true);

        }



        private void HidePanelWithIam()

        {

            EnsurePanelIam();

            if (panelIam != null && uiRoot.activeInHierarchy)

            {

                panelIam.startDisappear(true);

            }

            if (uiRoot != null)

            {

                uiRoot.SetActive(false);

            }

        }



        private IEnumerator ApplyVisibilityRoutine(bool visible)

        {

            if (uiRoot == null)

            {

                yield break;

            }



            if (visible)

            {

                uiRoot.SetActive(true);

                ShowPanelWithIam();

            }

            else

            {

                HidePanelWithIam();

            }



            visibilityRoutine = null;

            yield break;

        }



        private static IEnumerator WaitUntilIamState(InterfaceAnimManager iam, CSFHIAnimableState target, float timeout)

        {

            float elapsed = 0f;

            while (iam != null && iam.currentState != target && elapsed < timeout)

            {

                elapsed += Time.unscaledDeltaTime;

                yield return null;

            }

        }



        private void ApplyRequirementLine(
            Text line,
            int slotIndex,
            MachinePartReceiver.PartRequirement req,
            bool satisfied)

        {

            if (line == null)

            {

                return;

            }



            RestoreLayout(line, slotIndex);

            line.gameObject.SetActive(true);

            string displayName = req.part != null ? req.part.DisplayName : "?";

            string mark = satisfied ? MarkSatisfied : MarkPending;

            string content = $"{mark}{displayName}x{req.countRequired}";

            line.text = content;

            line.color = satisfied ? satisfiedTextColor : pendingTextColor;

        }



        private static void ClearRequirementLine(Text line)

        {

            if (line == null)

            {

                return;

            }



            line.text = string.Empty;

            line.gameObject.SetActive(false);

        }



        private static List<Text> CollectRequirementLineTexts(Transform searchRoot)

        {

            var result = new List<Text>(MaxRequirementLines);

            if (searchRoot == null)

            {

                return result;

            }



            Text[] all = searchRoot.GetComponentsInChildren<Text>(true);

            var ordered = new List<Text>();

            for (int i = 0; i < all.Length; i++)

            {

                Text t = all[i];

                if (t == null)

                {

                    continue;

                }



                string name = t.gameObject.name;

                if (name == "winorLose" || name.StartsWith("customText"))

                {

                    continue;

                }



                if (name == "amount" || name.StartsWith("amount "))

                {

                    ordered.Add(t);

                }

            }



            ordered.Sort(CompareAmountLineTopToBottom);

            for (int i = 0; i < ordered.Count && i < MaxRequirementLines; i++)

            {

                result.Add(ordered[i]);

            }



            return result;

        }



        private static int CompareAmountLineTopToBottom(Text a, Text b)

        {

            if (a == null && b == null)

            {

                return 0;

            }

            if (a == null)

            {

                return 1;

            }

            if (b == null)

            {

                return -1;

            }



            float yA = a.rectTransform.anchoredPosition.y;

            float yB = b.rectTransform.anchoredPosition.y;

            int yCompare = yB.CompareTo(yA);

            if (yCompare != 0)

            {

                return yCompare;

            }



            return string.CompareOrdinal(GetAmountSortKey(a.gameObject.name), GetAmountSortKey(b.gameObject.name));

        }



        private static string GetAmountSortKey(string objectName)

        {

            if (objectName == "amount")

            {

                return "0";

            }



            if (objectName.StartsWith("amount (") && objectName.EndsWith(")"))

            {

                return objectName.Substring("amount (".Length, objectName.Length - "amount (".Length - 1).PadLeft(4, '0');

            }



            return objectName;

        }

    }

}


