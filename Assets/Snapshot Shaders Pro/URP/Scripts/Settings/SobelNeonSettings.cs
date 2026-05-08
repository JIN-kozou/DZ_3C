namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/SobelNeon")]
    public sealed class SobelNeonSettings : VolumeComponent, IPostProcessComponent
    {
        public SobelNeonSettings()
        {
            displayName = "Sobel Neon";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Saturation values lower than this will be clamped to this.")]
        public ClampedFloatParameter saturationFloor = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        [Range(0.0f, 1.0f), Tooltip("Lightness/value values lower than this will be clamped to this.")]
        public ClampedFloatParameter lightnessFloor = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        [Tooltip("Color of the background if Use Scene Color is turned off.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

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
