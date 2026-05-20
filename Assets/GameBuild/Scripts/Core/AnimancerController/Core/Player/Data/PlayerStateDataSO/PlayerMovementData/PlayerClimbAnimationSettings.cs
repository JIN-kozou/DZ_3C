using UnityEngine;
[System.Serializable]
public class PlayerClimbAnimationSettings
{
    [Tooltip("起始位置匹配区间（动画归一化时间 x~y）。")]
    public Vector2 startMatchTime;
    [Tooltip("目标高度匹配区间（动画归一化时间 x~y），用于 Y 轴上台面/目标点对齐。")]
    public Vector2 targetMatchTime;
    [Tooltip("目标匹配点的高度偏移（米），叠加在 vaultPos 上。")]
    public float targetHeightOffSet;
    [Tooltip("起始匹配时沿墙法线方向的额外偏移（米），叠加在基础 0.35m 上。")]
    public float startMatchDistanceOffset;
    [Tooltip("相对 targetMatchTime.y 再延后多少（归一化时间）恢复 CharacterController。")]
    public float enableCCTimeOffset;
}