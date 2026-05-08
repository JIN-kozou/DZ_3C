namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SharpenSettings))]
#else
    [VolumeComponentEditor(typeof(SharpenSettings))]
#endif
    public class SharpenEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SharpenSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            intensity = Unpack(o.Find(x => x.intensity));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Sharpen>())
            {
                EditorGUILayout.HelpBox("The Sharpen effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Sharpen Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Sharpen>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(intensity);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Sharpen");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Sharpen";
    }
#endif
    }
}
