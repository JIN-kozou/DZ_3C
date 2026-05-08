namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/Underwater")]
    public sealed class UnderwaterSettings : VolumeComponent, IPostProcessComponent
    {
        public UnderwaterSettings()
        {
            displayName = "Underwater";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Displacement texture for surface waves.")]
        public TextureParameter bumpMap = new TextureParameter(null);

        [Range(0.0f, 10.0f), Tooltip("Strength/size of the waves.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);

        [Tooltip("Tint of the underwater fog.")]
        public ColorParameter waterFogColor = new ColorParameter(Color.white);

        [Range(0.0f, 1.0f), Tooltip("Strength of the underwater fog.")]
        public ClampedFloatParameter fogStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("")]
        public BoolParameter useCaustics = new BoolParameter(false);

        [Tooltip("")]
        public TextureParameter causticsTexture = new TextureParameter(null);

        public ClampedFloatParameter causticsNoiseSpeed = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        public ClampedFloatParameter causticsNoiseScale = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        public ClampedFloatParameter causticsNoiseStrength = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public Vector3Parameter causticsScrollVelocity1 = new Vector3Parameter(new Vector3(0.75f, 0.25f, 0.0f));

        public Vector3Parameter causticsScrollVelocity2 = new Vector3Parameter(new Vector3(0.75f, 0.25f, 0.0f));

        public Vector2Parameter causticsTiling = new Vector2Parameter(Vector2.one);

        public ColorParameter causticsTint = new ColorParameter(Color.white, true, true, true);

        public bool IsActive()
        {
            return (strength.value > 0.0f || fogStrength.value > 0.0f) && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
