namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/RadialBlur")]
    public sealed class RadialBlurSettings : VolumeComponent, IPostProcessComponent
    {
        public RadialBlurSettings()
        {
            displayName = "Radial Blur";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Blur Strength. Higher values require more system resources.")]
        public ClampedIntParameter strength = new ClampedIntParameter(1, 1, 500);

        [Range(1, 20), Tooltip("Distance between samples. Larger values may result in artefacts.")]
        public ClampedIntParameter stepSize = new ClampedIntParameter(5, 1, 20);

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
