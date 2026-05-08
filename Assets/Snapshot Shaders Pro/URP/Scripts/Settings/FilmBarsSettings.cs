namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/FilmBars")]
    public sealed class FilmBarsSettings : VolumeComponent, IPostProcessComponent
    {
        public FilmBarsSettings()
        {
            displayName = "Film Bars";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Range(0.1f, 5.0f), Tooltip("Desired aspect ratio (16:9 = 1.777 approx).")]
        public ClampedFloatParameter aspect = new ClampedFloatParameter(1.777f, 0.1f, 5.0f);

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
