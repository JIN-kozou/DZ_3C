namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Fancy Outlines")]
    public sealed class FancyOutlineSettings : VolumeComponent, IPostProcessComponent
    {
        public FancyOutlineSettings()
        {
            displayName = "Fancy Outline";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect enabled?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Color of the outlines.")]
        public ColorParameter outlineColor = new ColorParameter(Color.white, true, true, true);

        [Tooltip("Threshold for colour-based edge detection.")]
        public ClampedFloatParameter colorSensitivity = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        [Tooltip("Strength of colour-based edges.")]
        public ClampedFloatParameter colorStrength = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Threshold for depth-based edge detection.")]
        public ClampedFloatParameter depthSensitivity = new ClampedFloatParameter(0.01f, 0.0f, 1.0f);

        [Tooltip("Strength of depth-based edges.")]
        public ClampedFloatParameter depthStrength = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        [Tooltip("Threshold for normal-based edge detection.")]
        public ClampedFloatParameter normalSensitivity = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        [Tooltip("Strength of normal-based edges.")]
        public ClampedFloatParameter normalStrength = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);

        [Tooltip("Pixels past this depth threshold will not be edge-detected.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.99f, 0.0f, 1.0f);

        [Tooltip("Color of the background if Use Scene Color is turned off.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

        [Tooltip("Use the Scene Color instead of Background Color?")]
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
