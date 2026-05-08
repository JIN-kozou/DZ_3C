using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class BuffFullscreenVfxBinding
{
    [Tooltip("与 PlayerBuffConfigSO.BuffId 一致。")]
    public string buffId;

    [Tooltip("命中该 buff 时需要开启的 Full Screen Pass Features。")]
    public ScriptableRendererFeature[] features;

    [Min(0f)]
    [Tooltip("Buff 生效后延迟多少秒再开对应 Feature。")]
    public float delayAfterApplySec;

    [Min(0f)]
    [Tooltip("Buff 移除后延迟多少秒再关对应 Feature。")]
    public float delayAfterRemoveSec;

    [Min(0f)]
    [Tooltip("开启后最短保持时长，避免频繁闪烁。")]
    public float minHoldSec;

    [Tooltip("是否允许 buff 刷新时再次触发该特效。")]
    public bool triggerOnRefresh = true;
}

[CreateAssetMenu(menuName = "Asset/VFX/Buff Fullscreen VFX Bindings")]
public class BuffFullscreenVfxBindingsSO : ScriptableObject
{
    public List<BuffFullscreenVfxBinding> bindings = new List<BuffFullscreenVfxBinding>();
}
