using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Binds prefix/detail lines on getGearBanner / failgetGearBanner prefabs.
    /// </summary>
    public class GearPickupBannerView : MonoBehaviour
    {
        [SerializeField] private Text prefixText;
        [SerializeField] private Text detailText;

        private void Awake()
        {
            EnsureBound();
        }

        public void EnsureBound()
        {
            if (prefixText != null && detailText != null)
            {
                return;
            }

            AutoBind();
        }

        public void SetContent(string prefix, string detail)
        {
            EnsureBound();

            if (prefixText != null)
            {
                prefixText.text = prefix;
            }

            if (detailText != null)
            {
                detailText.text = detail;
            }
        }

        private void AutoBind()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if (t.gameObject.name.Contains("(1)"))
                {
                    prefixText = t;
                }
                else if (t.gameObject.name == "customText")
                {
                    detailText = t;
                }
            }
        }
    }
}
