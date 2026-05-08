namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SepiaToneSettings))]
#else
    [VolumeComponentEditor(typeof(SepiaToneSettings))]
#endif
    public class SepiaToneEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SepiaToneSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<SepiaTone>())
            {
                EditorGUILayout.HelpBox("The Sepia Tone effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Sepia Tone Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<SepiaTone>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Sepia Tone");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Sepia Tone";
    }
#endif
    }
}
