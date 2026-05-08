namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(VortexSettings))]
#else
    [VolumeComponentEditor(typeof(VortexSettings))]
#endif
    public class VortexEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;
        SerializedDataParameter center;
        SerializedDataParameter offset;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VortexSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
            center = Unpack(o.Find(x => x.center));
            offset = Unpack(o.Find(x => x.offset));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Vortex>())
            {
                EditorGUILayout.HelpBox("The Vortex effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Vortex Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Vortex>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
            PropertyField(center);
            PropertyField(offset);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Vortex");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Vortex";
    }
#endif
    }
}
