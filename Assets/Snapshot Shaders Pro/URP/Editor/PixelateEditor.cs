namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(PixelateSettings))]
#else
    [VolumeComponentEditor(typeof(PixelateSettings))]
#endif
    public class PixelateEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter pixelSize;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PixelateSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            pixelSize = Unpack(o.Find(x => x.pixelSize));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Pixelate>())
            {
                EditorGUILayout.HelpBox("The Pixelate effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Pixelate Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Pixelate>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(pixelSize);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Pixelate");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Pixelate";
    }
#endif
    }
}
