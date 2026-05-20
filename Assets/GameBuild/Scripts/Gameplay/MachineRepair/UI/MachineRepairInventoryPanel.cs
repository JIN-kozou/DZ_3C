using System.Collections;
using DZ_3C.MachineRepair;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.MachineRepair.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class MachineRepairInventoryPanel : MonoBehaviour
    {
        private static MachineRepairInventoryPanel activeInstance;
        [Header("文案")]
        [SerializeField] private string tabHintClosed = "库存";
        [SerializeField] private string tabHintOpen = "关闭";
        [SerializeField] private string remainCapacityFull = "剩余载荷：1/1";
        [SerializeField] private string remainCapacityEmpty = "剩余载荷：0/1";
        [SerializeField] private string emptyInventoryLabel = "库存为空";
        [Tooltip("Tier1 按钮文案，{0} 为零件显示名")]
        [SerializeField] private string tierButtonLabelFormat = "{0} x 1";

        [Header("引用")]
        [SerializeField] private MachinePartInventory inventory;
        [SerializeField] private MachinePartDefinition tier1Definition;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI tabHintText;
        [SerializeField] private Text amountText;
        [SerializeField] private GameObject gearUiRoot;
        [SerializeField] private GameObject customButtonRoot;
        [SerializeField] private Text customButtonLabel;
        [SerializeField] private GameObject emptyInventoryRoot;

        [Header("行为")]
        [SerializeField] private bool startPanelClosed = true;
        [SerializeField] private bool autoBindSceneReferences = true;

        private bool panelOpen;
        private bool panelIamPrimed;
        private Coroutine iamRefreshRoutine;
        private InterfaceAnimManager panelIam;

        private void Awake()
        {
            if (!TryBecomeActiveInstance())
            {
                return;
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<MachinePartInventory>();
            }

            if (tier1Definition == null)
            {
                tier1Definition = Resources.Load<MachinePartDefinition>("Config/MachineRepair/Tier1Material");
            }

            if (autoBindSceneReferences)
            {
                TryAutoBindSceneReferences();
            }

            if (panelRoot != null)
            {
                panelIam = panelRoot.GetComponent<InterfaceAnimManager>();
                if (panelIam != null)
                {
                    panelIam.autoStart = false;
                    panelIamPrimed = panelRoot.activeInHierarchy;
                }
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
            }

            if (iamRefreshRoutine != null)
            {
                StopCoroutine(iamRefreshRoutine);
                iamRefreshRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private bool TryBecomeActiveInstance()
        {
            MachineRepairInventoryPanel[] panels = FindObjectsOfType<MachineRepairInventoryPanel>(true);
            if (panels.Length == 1)
            {
                activeInstance = this;
                return true;
            }

            MachineRepairInventoryPanel primary = SelectPrimary(panels);
            if (this != primary)
            {
                enabled = false;
                return false;
            }

            activeInstance = this;
            for (int i = 0; i < panels.Length; i++)
            {
                MachineRepairInventoryPanel duplicate = panels[i];
                if (duplicate == this || !duplicate.enabled)
                {
                    continue;
                }

                Debug.LogWarning(
                    $"[MachineRepair] Disabled duplicate MachineRepairInventoryPanel on \"{duplicate.name}\". " +
                    $"Edit copy on \"{name}\" (Machine Repair Inventory Panel).",
                    duplicate);
                duplicate.enabled = false;
            }

            return true;
        }

        private static MachineRepairInventoryPanel SelectPrimary(MachineRepairInventoryPanel[] panels)
        {
            MachineRepairInventoryPanel best = panels[0];
            int bestScore = best.GetReferenceScore();
            for (int i = 1; i < panels.Length; i++)
            {
                int score = panels[i].GetReferenceScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = panels[i];
                }
            }

            return best;
        }

        private int GetReferenceScore()
        {
            int score = 0;
            if (panelRoot != null)
            {
                score += 100;
            }

            if (tabHintText != null)
            {
                score += 20;
            }

            if (amountText != null)
            {
                score += 10;
            }

            if (gearUiRoot != null)
            {
                score += 5;
            }

            if (customButtonRoot != null)
            {
                score += 5;
            }

            if (emptyInventoryRoot != null)
            {
                score += 5;
            }

            if (gameObject.name == "MachineRepairUI")
            {
                score += 1;
            }

            return score;
        }

        private void Start()
        {
            panelOpen = !startPanelClosed;
            ApplyPanelVisibility();
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                panelOpen = !panelOpen;
                ApplyPanelVisibility();
            }
        }

        private void ApplyPanelVisibility()
        {
            if (tabHintText != null)
            {
                tabHintText.text = panelOpen ? tabHintOpen : tabHintClosed;
            }

            if (!panelOpen || panelRoot == null)
            {
                if (iamRefreshRoutine != null)
                {
                    StopCoroutine(iamRefreshRoutine);
                    iamRefreshRoutine = null;
                }

                HideInventoryPanel();
                return;
            }

            if (!ShowInventoryPanel())
            {
                Refresh();
            }
        }

        private void EnsurePanelIam()
        {
            if (panelIam == null && panelRoot != null)
            {
                panelIam = panelRoot.GetComponent<InterfaceAnimManager>();
                if (panelIam != null)
                {
                    panelIam.autoStart = false;
                }
            }
        }

        private void HideInventoryPanel()
        {
            EnsurePanelIam();

            if (panelIam != null)
            {
                panelIam.startDisappear(true);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        /// <returns>True when appear animation is deferred (caller should skip Refresh).</returns>
        private bool ShowInventoryPanel()
        {
            if (panelRoot == null)
            {
                return false;
            }

            panelRoot.SetActive(true);
            EnsurePanelIam();

            if (panelIam == null)
            {
                return false;
            }

            if (!TryGetInventoryState(out bool empty, out bool hasTier1))
            {
                return false;
            }

            panelIam.autoStart = false;

            if (!panelIamPrimed)
            {
                if (iamRefreshRoutine != null)
                {
                    StopCoroutine(iamRefreshRoutine);
                }

                iamRefreshRoutine = StartCoroutine(ShowInventoryPanelAfterIamStart(empty, hasTier1));
                return true;
            }

            PlayInventoryAppearAnimation(empty, hasTier1);
            return false;
        }

        private IEnumerator ShowInventoryPanelAfterIamStart(bool empty, bool hasTier1)
        {
            // Inventory was inactive, so IAM.Start() had not run yet. It calls startDisappear(true)
            // on the first active frame and would cancel our appear if we started in the same frame.
            yield return null;

            panelIamPrimed = true;
            iamRefreshRoutine = null;

            if (!panelOpen || panelRoot == null || panelIam == null)
            {
                yield break;
            }

            PlayInventoryAppearAnimation(empty, hasTier1);
            Refresh();

            iamRefreshRoutine = StartCoroutine(WaitForPanelAppearThenRefresh(panelIam));
        }

        private void PlayInventoryAppearAnimation(bool empty, bool hasTier1)
        {
            if (panelIam == null)
            {
                return;
            }

            // Closing only deactivated the root; IAM can stay "appeared" while children are hidden.
            // startAppear() is a no-op in that state — force a clean disappear before re-appearing.
            if (panelIam.currentState == CSFHIAnimableState.appeared
                || panelIam.currentState == CSFHIAnimableState.appearing
                || panelIam.currentState == CSFHIAnimableState.disappearing)
            {
                panelIam.startDisappear(true);
            }

            MachineRepairInventoryIamSync.ResetAllElementsForAppear(panelIam);
            MachineRepairInventoryIamSync.PrepareConditionalBeforeAppear(
                panelIam,
                empty,
                hasTier1,
                gearUiRoot,
                customButtonRoot,
                emptyInventoryRoot);

            panelIam.gameObject.SetActive(true);
            panelIam.startAppear();

            if (iamRefreshRoutine != null)
            {
                StopCoroutine(iamRefreshRoutine);
            }

            iamRefreshRoutine = StartCoroutine(WaitForPanelAppearThenRefresh(panelIam));
        }

        private IEnumerator WaitForPanelAppearThenRefresh(InterfaceAnimManager iam)
        {
            while (iam != null && iam.currentState == CSFHIAnimableState.appearing)
            {
                yield return null;
            }

            Refresh();
            iamRefreshRoutine = null;
        }

        private void TryAutoBindSceneReferences()
        {
            if (panelRoot != null && tabHintText != null && amountText != null && gearUiRoot != null
                && customButtonRoot != null)
            {
                return;
            }

            if (!MachineRepairUiLocator.TryResolveHudWidgets(out Transform inventoryTransform, out Transform tab, out _))
            {
                return;
            }

            if (panelRoot == null && inventoryTransform != null)
            {
                panelRoot = inventoryTransform.gameObject;
            }

            if (tabHintText == null && tab != null)
            {
                tabHintText = tab.Find("title (1)")?.GetComponent<TextMeshProUGUI>();
            }

            if (amountText == null && inventoryTransform != null)
            {
                amountText = inventoryTransform.Find("amount")?.GetComponent<Text>();
            }

            if (gearUiRoot == null && inventoryTransform != null)
            {
                Transform gear = inventoryTransform.Find("GearUI");
                if (gear != null)
                {
                    gearUiRoot = gear.gameObject;
                }
            }

            if (customButtonRoot == null && inventoryTransform != null)
            {
                Transform tierButton = inventoryTransform.Find("TierButton");
                if (tierButton == null)
                {
                    tierButton = inventoryTransform.Find("customButton");
                }

                if (tierButton != null)
                {
                    customButtonRoot = tierButton.gameObject;
                }
            }

            if (emptyInventoryRoot == null && inventoryTransform != null)
            {
                Transform empty = inventoryTransform.Find("emptyInventoryHint");
                if (empty != null)
                {
                    emptyInventoryRoot = empty.gameObject;
                }
            }
        }

        private bool TryGetInventoryState(out bool empty, out bool hasTier1)
        {
            empty = true;
            hasTier1 = false;
            if (inventory == null)
            {
                return false;
            }

            empty = inventory.Snapshot().Count == 0;
            hasTier1 = tier1Definition != null && inventory.GetCount(tier1Definition) > 0;
            return true;
        }

        private void Refresh()
        {
            if (!TryGetInventoryState(out bool empty, out bool hasTier1))
            {
                return;
            }

            if (panelOpen && panelRoot != null)
            {
                MachineRepairInventoryIamSync.ApplyChromeVisibility(panelRoot.transform, true);
            }

            if (amountText != null)
            {
                amountText.text = empty || !hasTier1 ? remainCapacityFull : remainCapacityEmpty;
            }

            MachineRepairInventoryIamSync.ApplyConditionalVisibility(
                empty,
                hasTier1,
                gearUiRoot,
                customButtonRoot,
                emptyInventoryRoot);

            if (customButtonLabel == null && customButtonRoot != null)
            {
                customButtonLabel = customButtonRoot.GetComponentInChildren<Text>(true);
            }

            if (customButtonLabel != null && hasTier1 && tier1Definition != null)
            {
                customButtonLabel.text = string.Format(tierButtonLabelFormat, tier1Definition.DisplayName);
            }

            if (emptyInventoryRoot != null && empty)
            {
                Text emptyLabel = emptyInventoryRoot.GetComponentInChildren<Text>(true);
                if (emptyLabel != null)
                {
                    emptyLabel.text = emptyInventoryLabel;
                }
            }
        }
    }
}
