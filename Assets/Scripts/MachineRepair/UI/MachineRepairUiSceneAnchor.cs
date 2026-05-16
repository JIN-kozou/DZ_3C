using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Ensures inventory panel + banner queue exist when the scene loads (no manual wiring required in Editor Play).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MachineRepairUiSceneAnchor : MonoBehaviour
    {
        private void Awake()
        {
            if (FindObjectOfType<MachineRepairInventoryPanel>() == null)
            {
                gameObject.AddComponent<MachineRepairInventoryPanel>();
            }

            if (FindObjectOfType<MachineRepairPickupBannerQueue>() == null)
            {
                MachineRepairPickupBannerQueue.CreateDefaultUnderCanvas();
            }
        }
    }
}
