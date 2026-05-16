using DZ_3C.MachineRepair;
using TMPro;
using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// HUD PartIcon：靠近可拾取零件或接收器时显示交互提示（E 键框 + 标题文案）。
    /// </summary>
    [DisallowMultipleComponent]
    public class MachineRepairPartIconPrompt : MonoBehaviour
    {
        private const string PickupText = "拾取零件";
        private const string SubmitText = "提交零件";

        private enum PromptMode
        {
            Hidden,
            Pickup,
            Submit
        }

        [SerializeField] private GameObject uiRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private RepairInteractionHub interactionHub;

        private InterfaceAnimManager panelIam;
        private PromptMode currentMode = PromptMode.Hidden;
        private bool isVisible;

        public static MachineRepairPartIconPrompt FindInScene()
        {
            MachineRepairPartIconPrompt[] prompts = FindObjectsOfType<MachineRepairPartIconPrompt>(true);
            return prompts.Length > 0 ? prompts[0] : null;
        }

        private void Awake()
        {
            if (uiRoot == null)
            {
                uiRoot = gameObject;
            }

            if (titleText == null)
            {
                titleText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            EnsurePanelIam();
        }

        private void OnDestroy()
        {
            interactionHub?.UnregisterPartIconPrompt(this);
        }

        public void BindHub(RepairInteractionHub hub)
        {
            if (interactionHub == hub)
            {
                return;
            }

            interactionHub?.UnregisterPartIconPrompt(this);
            interactionHub = hub;
            interactionHub?.RegisterPartIconPrompt(this);
            RefreshFromHub(interactionHub);
        }

        public void RefreshFromHub(RepairInteractionHub hub)
        {
            PromptMode desired = PromptMode.Hidden;
            if (hub != null)
            {
                if (hub.HasAnyReceiverInRange())
                {
                    desired = PromptMode.Submit;
                }
                else if (hub.HasAnyPartInRange())
                {
                    desired = PromptMode.Pickup;
                }
            }

            ApplyMode(desired);
        }

        private void ApplyMode(PromptMode desired)
        {
            if (currentMode == desired && (desired == PromptMode.Hidden || isVisible))
            {
                return;
            }

            currentMode = desired;

            if (desired == PromptMode.Hidden)
            {
                SetVisible(false);
                return;
            }

            if (titleText != null)
            {
                titleText.text = desired == PromptMode.Submit ? SubmitText : PickupText;
            }

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (isVisible == visible)
            {
                if (!visible || (uiRoot != null && uiRoot.activeInHierarchy))
                {
                    return;
                }
            }

            isVisible = visible;
            ApplyVisibilityImmediate(visible);
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
                ShowWithIam();
            }
            else
            {
                HideWithIam();
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

        private void ShowWithIam()
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

        private void HideWithIam()
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
    }
}
