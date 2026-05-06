using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Battery Zone 交互提示与长按进度 UI。
    /// </summary>
    [DisallowMultipleComponent]
    public class BatteryZonePromptUI : MonoBehaviour
    {
        [SerializeField] private ReverseBatteryZone batteryZone;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private Slider holdProgressBar;

        private void Awake()
        {
            if (batteryZone == null) batteryZone = FindObjectOfType<ReverseBatteryZone>();
            if (promptRoot == null) promptRoot = gameObject;
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (batteryZone == null)
            {
                if (promptRoot != null) promptRoot.SetActive(false);
                return;
            }

            bool shouldShow = batteryZone.IsPlayerInside && !batteryZone.IsActivatedInCurrentStay;
            if (promptRoot != null) promptRoot.SetActive(shouldShow);
            if (!shouldShow) return;

            if (promptLabel != null) promptLabel.text = batteryZone.PromptText;
            if (holdProgressBar != null)
            {
                holdProgressBar.minValue = 0f;
                holdProgressBar.maxValue = 1f;
                holdProgressBar.value = batteryZone.ChargeProgressNormalized;
            }
        }
    }
}
