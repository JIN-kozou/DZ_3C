namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SobelOutlineSettings))]
#else
    [VolumeComponentEditor(typeof(SobelOutlineSettings))]
#endif
    public class SobelOutlineEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter threshold;
        SerializedDataParameter outlineColor;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SobelOutlineSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            threshold = Unpack(o.Find(x => x.threshold));
            outlineColor = Unpack(o.Find(x => x.outlineColor));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<SobelOutline>())
            {
                EditorGUILayout.HelpBox("The Outline (Sobel) effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Outline (Sobel) Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<SobelOutline>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(threshold);
            PropertyField(outlineColor);
            PropertyField(backgroundColor);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Outline (Sobel)");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Outline (Sobel)";
    }
#endif
    }
}
