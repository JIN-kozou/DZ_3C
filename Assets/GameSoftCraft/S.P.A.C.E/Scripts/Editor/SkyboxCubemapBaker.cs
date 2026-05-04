using UnityEditor;
using UnityEngine;

namespace GameSoftCraft.SpaceEditor
{
    /// <summary>
    /// Bakes a Unity skybox material (e.g. Skybox/6 Sided) into a <see cref="Cubemap"/> using
    /// <see cref="Camera.RenderToCubemap(Cubemap)"/> so sampling matches the engine skybox path (no hand-derived UV seams).
    /// </summary>
    public static class SkyboxCubemapBaker
    {
        public const int DefaultResolution = 1024;

        public static bool IsLikelySixSidedSkybox (Material material)
        {
            return material != null && material.HasProperty("_FrontTex") && material.HasProperty("_BackTex");
        }

        /// <summary>Render current skybox material into a new cubemap (caller saves asset). Returns null on failure.</summary>
        public static Cubemap BakeSkyboxMaterialToCubemap (Material skyboxMaterial, int resolution, out string errorMessage)
        {
            errorMessage = null;

            if (skyboxMaterial == null) {
                errorMessage = "Skybox material is null.";
                return null;
            }

            if (!IsLikelySixSidedSkybox(skyboxMaterial)) {
                errorMessage = "Material does not expose six skybox faces (_FrontTex / _BackTex, etc.). Use Skybox/6 Sided or compatible.";
                return null;
            }

            if (resolution < 16 || resolution > 4096 || (resolution & (resolution - 1)) != 0) {
                errorMessage = "Resolution must be a power of two between 16 and 4096.";
                return null;
            }

            var texFormat = TextureFormat.RGBAHalf;
            if (!SystemInfo.SupportsTextureFormat(texFormat)) {
                texFormat = TextureFormat.RGBA32;
            }

            var previousSky = RenderSettings.skybox;
            StarfieldMatManager.SuppressAssignToRenderSettings = true;
            try {
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();

                var go = new GameObject("Space_CubemapBakeCamera") {
                    hideFlags = HideFlags.HideAndDontSave
                };
                try {
                    var cam = go.AddComponent<Camera>();
                    cam.cameraType = CameraType.Game;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.cullingMask = 0;
                    cam.nearClipPlane = 0.01f;
                    cam.farClipPlane = 1000f;
                    cam.fieldOfView = 90f;
                    cam.backgroundColor = Color.black;
                    cam.enabled = false;
                    cam.transform.position = Vector3.zero;
                    cam.transform.rotation = Quaternion.identity;

                    var cube = new Cubemap(resolution, texFormat, false);
                    if (!cam.RenderToCubemap(cube)) {
                        Object.DestroyImmediate(cube);
                        errorMessage = "Camera.RenderToCubemap returned false.";
                        return null;
                    }

                    cube.Apply(false, true);
                    return cube;
                }
                finally {
                    Object.DestroyImmediate(go);
                }
            }
            finally {
                RenderSettings.skybox = previousSky;
                DynamicGI.UpdateEnvironment();
                StarfieldMatManager.SuppressAssignToRenderSettings = false;
            }
        }

        [MenuItem("GameSoftCraft/S.P.A.C.E/Bake selected skybox material to Cubemap…", false, 20)]
        static void MenuBakeSelectedMaterial ()
        {
            if (!(Selection.activeObject is Material mat)) {
                EditorUtility.DisplayDialog(
                    "S.P.A.C.E",
                    "Select a Material asset in the Project window (e.g. Dark Simple Skybox MAT).",
                    "OK");
                return;
            }

            if (!IsLikelySixSidedSkybox(mat)) {
                EditorUtility.DisplayDialog(
                    "S.P.A.C.E",
                    "Selected material does not look like a 6-sided skybox (needs _FrontTex / _BackTex).",
                    "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject(
                "Save baked Cubemap",
                mat.name + " Baked Cubemap",
                "asset",
                "Choose where to save the baked Cubemap.");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            BakeAndSaveAsset(mat, DefaultResolution, path);
        }

        public static void BakeAndSaveAsset (Material skyboxMaterial, int resolution, string assetPath)
        {
            var cube = BakeSkyboxMaterialToCubemap(skyboxMaterial, resolution, out var err);
            if (cube == null) {
                EditorUtility.DisplayDialog("S.P.A.C.E bake failed", err ?? "Unknown error.", "OK");
                return;
            }

            cube.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(cube, assetPath);
            AssetDatabase.SaveAssets();

            // Prefer sharper sampling in-game when Unity exposes a texture importer for this asset.
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (texImporter != null) {
                texImporter.filterMode = FilterMode.Trilinear;
                texImporter.anisoLevel = 9;
                texImporter.mipmapEnabled = true;
                texImporter.SaveAndReimport();
            }

            EditorGUIUtility.PingObject(cube);
            Debug.Log($"S.P.A.C.E: Baked cubemap saved to {assetPath} (re-bake at 1024–2048 if the sky looks soft).");
        }
    }
}
