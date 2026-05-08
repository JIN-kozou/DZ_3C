namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Posterize")]
    public sealed class PosterizeSettings : VolumeComponent, IPostProcessComponent
    {
        public PosterizeSettings()
        {
            displayName = "Posterize";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("How many red levels are supported.")]
        public ClampedIntParameter redLevels = new ClampedIntParameter(2, 2, 256);

        [Tooltip("How many green levels are supported.")]
        public ClampedIntParameter greenLevels = new ClampedIntParameter(2, 2, 256);

        [Tooltip("How many blue levels are supported.")]
        public ClampedIntParameter blueLevels = new ClampedIntParameter(2, 2, 256);

        [Tooltip("Modify the input colors via a power ramp. 1 = original mapping, " +
            "higher = favors darker output, lower = favors lighter output.")]
        public ClampedFloatParameter powerRamp = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);

        public bool IsActive()
        {
            return enabled.value && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
