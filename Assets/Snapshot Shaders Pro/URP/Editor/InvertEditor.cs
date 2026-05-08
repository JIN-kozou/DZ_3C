namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(InvertSettings))]
#else
    [VolumeComponentEditor(typeof(InvertSettings))]
#endif
    public class InvertEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<InvertSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Invert>())
            {
                EditorGUILayout.HelpBox("The Invert effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Invert Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Invert>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Invert");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Invert";
    }
#endif
    }
}
