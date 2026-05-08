namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Kaleidoscope")]
    public sealed class KaleidoscopeSettings : VolumeComponent, IPostProcessComponent
    {
        public KaleidoscopeSettings()
        {
            displayName = "Kaleidoscope";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("The number of radial segments.")]
        public ClampedFloatParameter segmentCount = new ClampedFloatParameter(1.0f, 1.0f, 20.0f);

        public bool IsActive()
        {
            return segmentCount.value > 1.0f && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
