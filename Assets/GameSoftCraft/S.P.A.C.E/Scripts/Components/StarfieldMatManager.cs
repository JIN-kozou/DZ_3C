using UnityEngine;

namespace GameSoftCraft
{
    [ExecuteAlways]
    public class StarfieldMatManager : MonoBehaviour
    {
        public SkyMaterial skyMaterial;

        [Tooltip("When enabled, this component assigns the S.P.A.C.E runtime material to RenderSettings.skybox (Lighting window). Turn off to edit the Lighting skybox manually or while baking a cubemap.")]
        public bool assignToRenderSettings = true;

        /// <summary>When true, <see cref="Refresh"/> updates material properties but does not write <see cref="RenderSettings.skybox"/> (used by editor cubemap baker).</summary>
        public static bool SuppressAssignToRenderSettings { get; set; }

        private void OnEnable ()
        {
            Refresh();
        }

        private void OnValidate ()
        {
            Refresh();
        }

        private void Refresh ()
        {
            if (skyMaterial == null) {
                Debug.LogWarning("Sky Materialis not assigned!");
                return;
            }

            skyMaterial.UpdateMaterialProperties();
            var skyboxMaterial = skyMaterial.GetMaterial();
            if (skyboxMaterial == null) {
                Debug.LogError("Failed to get material from Sky Material!");
                return;
            }

            if (assignToRenderSettings && !SuppressAssignToRenderSettings) {
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }
    }
}
