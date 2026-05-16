using System.Collections;
using DZ_3C.MachineRepair;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.MachineRepair.UI
{
    public class MachineRepairInventoryPanel : MonoBehaviour
    {
        private const string TabHintClosed = "库存";
        private const string TabHintOpen = "关闭";
        private const string RemainFull = "剩余载荷：1/1";
        private const string RemainEmpty = "剩余载荷：0/1";
        private const string EmptyInventoryLabel = "库存为空";

        [SerializeField] private MachinePartInventory inventory;
        [SerializeField] private MachinePartDefinition tier1Definition;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI tabHintText;
        [SerializeField] private Text amountText;
        [SerializeField] private GameObject gearUiRoot;
        [SerializeField] private GameObject customButtonRoot;
        [SerializeField] private Text customButtonLabel;
        [SerializeField] private GameObject emptyInventoryRoot;
        [SerializeField] private bool startPanelClosed = true;
        [SerializeField] private bool autoBindSceneReferences = true;

        private bool panelOpen;
        private Coroutine iamRefreshRoutine;
        private InterfaceAnimManager panelIam;

        private void Awake()
        {
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
            if (panelRoot != null)
            {
                panelRoot.SetActive(panelOpen);
            }

            if (tabHintText != null)
            {
                tabHintText.text = panelOpen ? TabHintOpen : TabHintClosed;
            }

            if (!panelOpen || panelRoot == null)
            {
                if (iamRefreshRoutine != null)
                {
                    StopCoroutine(iamRefreshRoutine);
                    iamRefreshRoutine = null;
                }

                return;
            }

            if (panelIam == null)
            {
                panelIam = panelRoot.GetComponent<InterfaceAnimManager>();
                if (panelIam != null)
                {
                    panelIam.autoStart = false;
                }
            }

            TryGetInventoryState(out bool empty, out bool hasTier1);

            if (panelIam != null)
            {
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

            Refresh();
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
                amountText.text = empty || !hasTier1 ? RemainFull : RemainEmpty;
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
                customButtonLabel.text = $"{tier1Definition.DisplayName} x 1";
            }

            if (emptyInventoryRoot != null)
            {
                Text emptyLabel = emptyInventoryRoot.GetComponentInChildren<Text>(true);
                if (emptyLabel != null && string.IsNullOrEmpty(emptyLabel.text))
                {
                    emptyLabel.text = EmptyInventoryLabel;
                }
            }
        }
    }
}
