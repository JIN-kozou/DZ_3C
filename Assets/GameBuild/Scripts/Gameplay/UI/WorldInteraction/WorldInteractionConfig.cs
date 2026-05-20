using UnityEngine;

namespace DZ_3C.UI.WorldInteraction
{
  [CreateAssetMenu(fileName = "WorldInteractionConfig", menuName = "DZ_3C/UI/WorldInteraction Config", order = 0)]
  public sealed class WorldInteractionConfig : ScriptableObject
  {
    [Header("Display Distance (m)")]
    [Tooltip("准心指向且距离玩家不超过此值时，显示 E + 文案（及长按进度条）。")]
    [Min(0.1f)]
    public float activePromptMaxDistance = 3f;

    [Tooltip("距离玩家不超过此值时，显示次级圆点标记（非准心选中目标）。")]
    [Min(0.1f)]
    public float markerMaxDistance = 12f;

    [Header("Aim Ray")]
    [Tooltip("屏幕中心准心射线最大距离（米）。")]
    [Min(0.5f)]
    public float aimRayMaxDistance = 40f;

    [Tooltip("准心射线检测的 Layer。")]
    public LayerMask aimLayerMask = ~0;

    [Tooltip("为 true 时，射线未命中会用视角夹角兜底；完整提示建议保持 false，移开准心即显示圆点。")]
    public bool allowAngleFocusFallback;

    [Tooltip("视角夹角兜底的最大角度（度），仅 allowAngleFocusFallback 为 true 时生效。")]
    [Range(1f, 45f)]
    public float focusMaxViewAngle = 12f;
  }
}
