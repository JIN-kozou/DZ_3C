namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Scanlines")]
    public sealed class ScanlinesSettings : VolumeComponent, IPostProcessComponent
    {
        public ScanlinesSettings()
        {
            displayName = "Scanlines";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Scanlines texture.")]
        public TextureParameter scanlineTex = new TextureParameter(null);

        [Tooltip("Strength of the effect.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("Pixel size of the scanlines.")]
        public ClampedIntParameter size = new ClampedIntParameter(8, 1, 64);

        [Tooltip("Scroll speed of scanlines vertically.")]
        public ClampedFloatParameter scrollSpeed = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);

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
