namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Vortex")]
    public sealed class VortexSettings : VolumeComponent, IPostProcessComponent
    {
        public VortexSettings()
        {
            displayName = "Vortex";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("How strongly the effect will twirl pixels around the center.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);

        [Tooltip("The vortex will swirl around this normalized position.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("How far the image is offset before twirling.")]
        public Vector2Parameter offset = new Vector2Parameter(Vector2.zero);

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
