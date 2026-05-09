#if UNITY_EDITOR
using DZ_3C.Reverse;
using UnityEditor;
using UnityEngine;

namespace DZ_3C.EditorTools
{
    /// <summary>
    /// Configures Physics collision matrix: GravityInfluence layer only interacts with GravityProps.
    /// Run once after adding layers in Tags &amp; Layers.
    /// </summary>
    public static class GravityIlluminationPhysicsSetup
    {
        private const string MenuPathTop = "DZ_3C/Physics/Apply Gravity Illumination Layer Matrix";

        /// <summary>Same command under Window — visible if the custom top bar menu does not appear.</summary>
        private const string MenuPathWindow = "Window/DZ_3C/Apply Gravity Illumination Layer Matrix";

        [MenuItem(MenuPathTop, priority = 0)]
        [MenuItem(MenuPathWindow, priority = 100)]
        public static void ApplyLayerMatrix()
        {
            int inf = LayerMask.NameToLayer(GravityIlluminationLayers.Influence);
            int props = LayerMask.NameToLayer(GravityIlluminationLayers.Props);
            if (inf < 0 || props < 0)
            {
                Debug.LogError(
                    "[GravityIllumination] Missing layers. Add user layers named '" +
                    GravityIlluminationLayers.Influence + "' and '" +
                    GravityIlluminationLayers.Props + "' in Edit → Project Settings → Tags and Layers.");
                return;
            }

            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(inf, i, i != props);
            }

            Debug.Log(
                $"[GravityIllumination] Layer {inf} ({GravityIlluminationLayers.Influence}) now only interacts with layer {props} ({GravityIlluminationLayers.Props}). Save the project to persist.",
                null);
        }
    }
}
#endif
