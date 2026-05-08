namespace SnapshotShaders
{
    using SnapshotShaders.URP;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Light Streaks")]
    public sealed class LightStreaksSettings : VolumeComponent, IPostProcessComponent
    {
        public LightStreaksSettings()
        {
            displayName = "Light Streaks";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Light Streaks blur strength.")]
        public ClampedIntParameter strength = new ClampedIntParameter(1, 1, 1000);

        [Tooltip("Luminance Threshold - pixels above this luminance will glow.")]
        public ClampedFloatParameter luminanceThreshold = new ClampedFloatParameter(1.2f, 0.0f, 25.0f);

        [Tooltip("Divisor to apply to the screen resolution in the x-direction for the blur pass.")]
        public NoInterpClampedIntParameter downsampleAmount = new NoInterpClampedIntParameter(24, 1, 128);

        public bool IsActive()
        {
            return strength.value > 1 && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
