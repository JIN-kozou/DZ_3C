using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Optional asset for pickup-banner scale/position tuning (assign on PickupBannerStack).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PickupBannerLayoutSettings",
        menuName = "DZ_3C/Machine Repair/Pickup Banner Layout Settings")]
    public class MachineRepairPickupBannerLayoutSettings : ScriptableObject
    {
        [Tooltip("在场景示例布局复制之后叠加的 localScale 倍率（相对 BannerExamples 示例）。3 = 示例尺寸的 3 倍。")]
        public Vector3 scaleMultiplier = Vector3.one;

        [Tooltip("在示例 anchoredPosition 上叠加的偏移（Canvas 坐标）。")]
        public Vector2 positionOffset;

        [Tooltip("多条 Banner 垂直堆叠间距倍率（与示例间距相乘）。")]
        [Min(0.1f)]
        public float stackSpacingMultiplier = 1f;
    }
}
