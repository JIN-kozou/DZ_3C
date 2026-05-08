namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Greyscale")]
    public sealed class GreyscaleSettings : VolumeComponent, IPostProcessComponent
    {
        public GreyscaleSettings()
        {
            displayName = "Greyscale";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Greyscale effect intensity.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

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
