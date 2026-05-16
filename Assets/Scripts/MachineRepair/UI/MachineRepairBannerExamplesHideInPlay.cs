using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Hides editor-only banner layout examples during Play Mode; does not delete them from the scene.
    /// </summary>
    public class MachineRepairBannerExamplesHideInPlay : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
