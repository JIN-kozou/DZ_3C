namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(PaintingSettings))]
#else
    [VolumeComponentEditor(typeof(PaintingSettings))]
#endif
    public class PaintingEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter kernelSize;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PaintingSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            kernelSize = Unpack(o.Find(x => x.kernelSize));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Painting>())
            {
                EditorGUILayout.HelpBox("The Painting effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Painting Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Painting>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(kernelSize);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Painting");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Painting";
    }
#endif
    }
}
