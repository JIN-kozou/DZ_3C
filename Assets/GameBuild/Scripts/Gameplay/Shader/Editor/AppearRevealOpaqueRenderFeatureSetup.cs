#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class AppearRevealOpaqueRenderFeatureSetup
{
    const string HighFidelityRendererPath = "Assets/GameBuild/Settings/URP-HighFidelity-Renderer.asset";

    [MenuItem("GameBuild/Appear/Add Opaque Render Feature To URP Renderer")]
    public static void AddFeatureToHighFidelityRenderer()
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(HighFidelityRendererPath);
        if (rendererData == null)
        {
            Debug.LogError($"[Appear] Missing renderer asset at {HighFidelityRendererPath}");
            return;
        }

        var feature = ScriptableObject.CreateInstance<AppearRevealOpaqueRenderFeature>();
        feature.name = "AppearRevealOpaqueRenderFeature";

        Undo.RegisterCreatedObjectUndo(feature, "Create AppearRevealOpaqueRenderFeature");
        AssetDatabase.AddObjectToAsset(feature, rendererData);

        var serializedRenderer = new SerializedObject(rendererData);
        SerializedProperty featureList = serializedRenderer.FindProperty("m_RendererFeatures");
        if (featureList == null)
        {
            Debug.LogError("[Appear] Could not find m_RendererFeatures on URP renderer asset.");
            Object.DestroyImmediate(feature);
            return;
        }

        for (int i = 0; i < featureList.arraySize; i++)
        {
            var existing = featureList.GetArrayElementAtIndex(i).objectReferenceValue as AppearRevealOpaqueRenderFeature;
            if (existing != null)
            {
                Debug.Log("[Appear] AppearRevealOpaqueRenderFeature is already present on URP-HighFidelity-Renderer.");
                Object.DestroyImmediate(feature);
                return;
            }
        }

        featureList.InsertArrayElementAtIndex(featureList.arraySize);
        featureList.GetArrayElementAtIndex(featureList.arraySize - 1).objectReferenceValue = feature;

        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        Debug.Log("[Appear] Added AppearRevealOpaqueRenderFeature to URP-HighFidelity-Renderer.");
    }
}
#endif
