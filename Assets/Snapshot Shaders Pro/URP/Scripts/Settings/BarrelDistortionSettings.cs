namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Barrel Distortion")]
    public class BarrelDistortionSettings : VolumeComponent, IPostProcessComponent
    {
        public BarrelDistortionSettings()
        {
            displayName = "Barrel Distortion";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Strength of the distortion. Values above zero cause CRT screen-like distortion; values below zero bulge outwards.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("Color of the background around the 'screen'.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

        public bool IsActive()
        {
            return strength.value > 0.0f && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
