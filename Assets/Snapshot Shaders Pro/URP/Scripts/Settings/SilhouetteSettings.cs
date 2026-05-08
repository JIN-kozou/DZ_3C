namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Silhouette")]
    public sealed class SilhouetteSettings : VolumeComponent, IPostProcessComponent
    {
        public SilhouetteSettings()
        {
            displayName = "Silhouette";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Color at the camera's near clip plane.")]
        public ColorParameter nearColor = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        [Tooltip("Color at the camera's far clip plane.")]
        public ColorParameter farColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 1.0f));

        [Tooltip("Modify the input colors via a power ramp. 1 = original mapping, " +
            "higher = favors near color, lower = favors far color.")]
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
