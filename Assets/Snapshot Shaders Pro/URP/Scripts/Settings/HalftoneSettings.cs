namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Halftone")]
    public sealed class HalftoneSettings : VolumeComponent, IPostProcessComponent
    {
        public HalftoneSettings()
        {
            displayName = "Halftone";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("The texture used to determine the shape of the halftone 'dots'.")]
        public TextureParameter halftoneTexture = new TextureParameter(null);

        [Tooltip("How soft the transition between light and dark is.")]
        public ClampedFloatParameter softness = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Size of the halftone 'dots' on the screen.")]
        public ClampedFloatParameter textureSize = new ClampedFloatParameter(4.0f, 1.0f, 20.0f);

        [Tooltip("Use this vector to remap the minimum and maximum luminance values used in calculations. Default is (0, 1).")]
        public Vector2Parameter minMaxLuminance = new Vector2Parameter(new Vector2(0.0f, 1.0f));

        [Tooltip("Color of the darkened sections.")]
        public ColorParameter darkColor = new ColorParameter(Color.black);

        [Tooltip("Color of the lighter sections.")]
        public ColorParameter lightColor = new ColorParameter(Color.white);

        [Tooltip("Use the Scene Color instead of Light Color?")]
        public BoolParameter useSceneColor = new BoolParameter(false);

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
