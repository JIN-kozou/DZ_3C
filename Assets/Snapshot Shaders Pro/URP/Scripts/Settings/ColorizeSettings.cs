namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Colorize")]
    public sealed class ColorizeSettings : VolumeComponent, IPostProcessComponent
    {
        public ColorizeSettings()
        {
            displayName = "Colorize";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [ColorUsage(true, true), Tooltip("Tint colour to use.")]
        public ColorParameter tintColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 0.0f));

        public bool IsActive()
        {
            return tintColor.value.a > 0.0f && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
