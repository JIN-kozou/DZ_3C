using Animancer;
using System;
using UnityEngine;

[Serializable]
public class PlayerClimbData
{
    [Tooltip("按障碍高度分档选择的攀爬动画，与 climbSettings 一一对应。索引：0=low，1=lowMedium，2=medium。")]
    [field: SerializeField] public ClipTransition[] climbs;
    [Tooltip("与 climbs 同索引的攀爬匹配参数。")]
    public PlayerClimbAnimationSettings[] climbSettings;
}