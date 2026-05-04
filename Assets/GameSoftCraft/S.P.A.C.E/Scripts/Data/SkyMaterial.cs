using UnityEngine;
using UnityEngine.Serialization;

namespace GameSoftCraft
{
    [CreateAssetMenu(fileName = "Sky Material", menuName = "S.P.A.C.E/Sky Material")]
    public class SkyMaterial : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField, Tooltip("Seed for procedural generation. Controls randomness in stars, nebula, and other features.")]
        int _seed = 0;

        [SerializeField, Tooltip("Controls the overall brightness adjustment of the sky. Lower values darken the sky, higher values brighten it.")]
        float _gamma = 1;

        [SerializeField, Tooltip("Enable or disable the sun in the sky material.")]
        bool _isSunOn;

        [SerializeField, Tooltip("Enable or disable the planet in the sky material.")]
        bool _isPlanetOn;

        [SerializeField, Tooltip("Enable or disable space debris effects in the sky material.")]
        bool _isDebrisOn;

        [SerializeField, Tooltip("Orientation of the sky material. This vector determines rotation applied to stars, planets, and other elements.")]
        Vector3 _orientation = Vector3.zero;

        [Header("Stars Settings")]

        [SerializeField, Tooltip("Density of far stars. Higher values create more distant stars.")]
        float _farStarDensity = .7f;

        [SerializeField, Tooltip("Twinkling effect for far stars. Higher values increase twinkle intensity.")]
        float _farStarTwinkle = .7f;

        [SerializeField, Tooltip("Density of mid-range stars. Higher values increase the number of stars closer than far stars but farther than near stars.")]
        float _midStarDensity = .7f;

        [SerializeField, Tooltip("Twinkling effect for mid-range stars. Adjusts twinkle intensity for mid-distance stars.")]
        float _midStarTwinkle = .7f;

        [SerializeField, Tooltip("Density of near stars. Controls how many stars appear close to the camera.")]
        float _nearStarDensity = 1.0f;

        [SerializeField, Tooltip("Color offset applied to nebula regions in the sky material.")]
        Color _nebulaColorOffset = Color.white;

        [Header("Underlay - background under stars (底图)")]
        [SerializeField, Tooltip("Use a Unity Skybox/6 Sided material as the background (e.g. Assets/Nebula Skyboxes/.../Dark Simple Skybox MAT.mat). Takes priority over the cubemap below.")]
        Material _sixSidedSkyboxMaterial;

        [SerializeField, Tooltip("Single cubemap underlay (ignored if a valid six-sided material is set above).")]
        Cubemap _backgroundCubemap;

        [SerializeField, Tooltip("Multiply sampled cubemap colors (cubemap mode only).")]
        Color _backgroundCubemapTint = Color.white;

        [SerializeField, Tooltip("Brightness multiplier for the cubemap sample (cubemap mode only).")]
        float _backgroundCubemapExposure = 1f;

        [SerializeField, FormerlySerializedAs("_backgroundCubemapBlend"), Tooltip("0 = only S.P.A.C.E stars. Raise to show the six-sided or cubemap background underneath. Edit here only, not the skybox material in Lighting.")]
        float _underlayBlend;

        [Header("Sun Settings")]

        [SerializeField, Tooltip("Size of the sun object in the sky.")]
        float _sunSize = .7f;

        [SerializeField, Tooltip("Speed of the sun's corona effect, adding a dynamic glow around the sun.")]
        float _sunCoronaSpeed = .7f;

        [SerializeField, Tooltip("Direction vector for the sun's position in the sky.")]
        Vector3 _sunDirection = Vector3.forward;

        [SerializeField, Tooltip("Color tint for the sun. Use to adjust the sun's hue.")]
        Color _sunTint = Color.yellow;

        [Header("Planet Settings")]
        [SerializeField, Tooltip("Size of the planet in the sky.")]
        float _planetSize = .7f;

        [SerializeField, Tooltip("Direction vector for the planet's position in the sky.")]
        Vector3 _planetDirection = Vector3.back;

        [SerializeField, Tooltip("Color tint for the planet. Controls the main color of the planet.")]
        Color _planetTint = new Color(1f, .3f, .25f);

        [SerializeField, Tooltip("Color tint for the atmosphere surrounding the planet.")]
        Color _planetAtmosphereTint = new Color(0, .8f, .35f);

        [SerializeField, Tooltip("Thickness of the planet's atmosphere layer. Higher values make the atmosphere more prominent.")]
        float _planetAtmosphereThickness = .7f;

        [SerializeField, Tooltip("Brightness level of the planet. Higher values make the planet appear brighter.")]
        float _planetBrightness = .7f;

        [SerializeField, Tooltip("Angle of the planet's axis. Adjust to tilt the planet.")]
        float _planetAngle = .5f;

        [SerializeField, Tooltip("Rotation speed of the planet. Higher values make the planet rotate faster.")]
        float _planetSpeed = .5f;

        [SerializeField, Tooltip("Angle of the shadow cast on the planet.")]
        float _shadowAngle = .7f;

        [SerializeField, Tooltip("Depth of the shadow on the planet, adjusting darkness.")]
        float _shadowDepth = .2f;

        [Header("Space Debris Settings")]
        [SerializeField, Tooltip("Color of the space debris")]
        Color _meteorsTint = new Color(0.7f, 0.5f, 0.6f);

        [SerializeField, Tooltip("Brightness of space debris elements. Higher values make debris more visible.")]
        float _meteorsBrightness = .7f;

        [SerializeField, Tooltip("Speed of the space debris movement.")]
        float _meteorsSpeed = .7f;

        // Do not serialize the runtime Material on this ScriptableObject: Unity will persist shader
        // defaults (e.g. Underlay Blend = 0) on the nested material and overwrite what you set in Play mode.
        [System.NonSerialized]
        Material _runtimeMaterial;

        const int SkyboxBgKindNone = 0;
        const int SkyboxBgKindCubemap = 1;
        const int SkyboxBgKindSixSided = 2;

        static readonly string[] SixSidedTexturePropertyNames = {
            "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
        };

        void EnsureRuntimeMaterial ()
        {
            if (_runtimeMaterial != null) {
                return;
            }

            var shader = Shader.Find("GameSoftCraft/S.P.A.C.E");
            if (shader == null) {
                Debug.LogError("Shader not found!");
                return;
            }

            _runtimeMaterial = new Material(shader);
        }

        private void EnableKeyword (string keyword, bool isEnabled)
        {
            if (_runtimeMaterial == null) {
                return;
            }

            if (isEnabled) {
                _runtimeMaterial.EnableKeyword(keyword);
                return;
            }

            _runtimeMaterial.DisableKeyword(keyword);
        }

        /// <summary>Copies face textures and tint/exposure/rotation from a Skybox/6 Sided material onto the S.P.A.C.E runtime material.</summary>
        bool CopySixSidedSkyboxProperties (Material source)
        {
            if (source == null || source.shader == null) {
                return false;
            }

            if (!source.HasProperty("_FrontTex") || !source.HasProperty("_BackTex")) {
                return false;
            }

            foreach (var prop in SixSidedTexturePropertyNames) {
                _runtimeMaterial.SetTexture(prop, source.GetTexture(prop));
                var hdrProp = prop + "_HDR";
                if (source.HasProperty(hdrProp)) {
                    _runtimeMaterial.SetVector(hdrProp, source.GetVector(hdrProp));
                }
            }

            if (source.HasProperty("_Tint")) {
                _runtimeMaterial.SetColor("_SixSidedTint", source.GetColor("_Tint"));
            }

            if (source.HasProperty("_Exposure")) {
                _runtimeMaterial.SetFloat("_SixSidedExposure", source.GetFloat("_Exposure"));
            }

            if (source.HasProperty("_Rotation")) {
                _runtimeMaterial.SetFloat("_SixSidedRotation", source.GetFloat("_Rotation"));
            }

            return true;
        }

        public Material GetMaterial ()
        {
            EnsureRuntimeMaterial();
            return _runtimeMaterial;
        }

        public void UpdateMaterialProperties ()
        {
            EnsureRuntimeMaterial();
            if (_runtimeMaterial == null) {
                return;
            }

            EnableKeyword("SUN_ON", _isSunOn);
            EnableKeyword("PLANET_ON", _isPlanetOn);
            EnableKeyword("DEBRIS_ON", _isDebrisOn);

            /// Update general settings
            _runtimeMaterial.SetInt("_Seed", _seed);
            _runtimeMaterial.SetFloat("_Gamma", _gamma);
            _runtimeMaterial.SetVector("_Orientation", _orientation);

            /// Update stars settings
            _runtimeMaterial.SetFloat("_FarStarDens", _farStarDensity);
            _runtimeMaterial.SetFloat("_FarStarTwinkle", _farStarTwinkle);
            _runtimeMaterial.SetFloat("_MidStarDens", _midStarDensity);
            _runtimeMaterial.SetFloat("_MidStarTwinkle", _midStarTwinkle);
            _runtimeMaterial.SetFloat("_NearStarDens", _nearStarDensity);
            _runtimeMaterial.SetColor("_NebulaColOffset", _nebulaColorOffset);

            // Baked cubemap (official RenderToCubemap) wins over live six-sided copy to avoid double underlay and use texCUBE path.
            if (_backgroundCubemap != null) {
                _runtimeMaterial.SetTexture("_SkyboxCube", _backgroundCubemap);
                _runtimeMaterial.SetColor("_SkyboxTint", _backgroundCubemapTint);
                _runtimeMaterial.SetFloat("_SkyboxExposure", _backgroundCubemapExposure);
                _runtimeMaterial.SetInt("_SkyboxBgKind", SkyboxBgKindCubemap);
                _runtimeMaterial.SetFloat("_SkyboxBlend", _underlayBlend);
            }
            else if (_sixSidedSkyboxMaterial != null && CopySixSidedSkyboxProperties(_sixSidedSkyboxMaterial)) {
                _runtimeMaterial.SetInt("_SkyboxBgKind", SkyboxBgKindSixSided);
                _runtimeMaterial.SetFloat("_SkyboxBlend", _underlayBlend);
            }
            else {
                _runtimeMaterial.SetInt("_SkyboxBgKind", SkyboxBgKindNone);
                _runtimeMaterial.SetFloat("_SkyboxBlend", 0f);
            }

            /// Update sun settings if applicable
            if (_isSunOn) {
                _runtimeMaterial.SetFloat("_SunSize", _sunSize);
                _runtimeMaterial.SetFloat("_SunCoronaSpeed", _sunCoronaSpeed);
                _runtimeMaterial.SetVector("_SunDir", _sunDirection);
                _runtimeMaterial.SetColor("_SunTint", _sunTint);
            }

            /// Update planet settings if applicable
            if (_isPlanetOn) {
                _runtimeMaterial.SetVector("_PlanetDir", _planetDirection);
                _runtimeMaterial.SetColor("_PlanetTint", _planetTint);
                _runtimeMaterial.SetColor("_PlanetAtmoTint", _planetAtmosphereTint);
                _runtimeMaterial.SetFloat("_PlanetAtmoThick", _planetAtmosphereThickness);
                _runtimeMaterial.SetFloat("_PlanetBrightness", _planetBrightness);
                _runtimeMaterial.SetFloat("_PlanetAngle", _planetAngle);
                _runtimeMaterial.SetFloat("_PlanetSpeed", _planetSpeed);
                _runtimeMaterial.SetFloat("_ShadowAngle", _shadowAngle);
                _runtimeMaterial.SetFloat("_ShadowDepth", _shadowDepth);
                _runtimeMaterial.SetFloat("_PlanetSize", _planetSize);
            }

            /// Update space debris settings if applicable
            if (_isDebrisOn) {
                _runtimeMaterial.SetColor("_MetTint", _meteorsTint);
                _runtimeMaterial.SetFloat("_MetBrightness", _meteorsBrightness);
                _runtimeMaterial.SetFloat("_MetSpeed", _meteorsSpeed);
            }
        }
    }
}