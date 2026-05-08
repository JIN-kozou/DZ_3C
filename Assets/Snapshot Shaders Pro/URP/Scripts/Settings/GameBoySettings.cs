namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/GameBoy")]
    public sealed class GameBoySettings : VolumeComponent, IPostProcessComponent
    {
        public GameBoySettings()
        {
            displayName = "Game Boy";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Darkest colour.")]
        public ColorParameter darkestColor = new ColorParameter(new Color(0.11f, 0.21f, 0.08f));

        [Tooltip("Second darkest colour.")]
        public ColorParameter darkColor = new ColorParameter(new Color(0.24f, 0.38f, 0.21f));

        [Tooltip("Second lightest colour.")]
        public ColorParameter lightColor = new ColorParameter(new Color(0.57f, 0.67f, 0.21f));

        [Tooltip("Lightest colour.")]
        public ColorParameter lightestColor = new ColorParameter(new Color(0.75f, 0.82f, 0.46f));

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
