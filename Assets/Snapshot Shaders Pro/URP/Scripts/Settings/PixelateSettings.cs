namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Pixelate")]
    public sealed class PixelateSettings : VolumeComponent, IPostProcessComponent
    {
        public PixelateSettings()
        {
            displayName = "Pixelate";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Size of each new 'pixel' in the image.")]
        public ClampedIntParameter pixelSize = new ClampedIntParameter(1, 1, 256);

        public bool IsActive()
        {
            return pixelSize.value > 1 && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
