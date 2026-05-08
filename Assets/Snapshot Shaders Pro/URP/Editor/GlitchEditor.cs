namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(GlitchSettings))]
#else
    [VolumeComponentEditor(typeof(GlitchSettings))]
#endif
    public class GlitchEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter offsetTexture;
        SerializedDataParameter offsetStrength;
        SerializedDataParameter verticalTiling;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlitchSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            offsetTexture = Unpack(o.Find(x => x.offsetTexture));
            offsetStrength = Unpack(o.Find(x => x.offsetStrength));
            verticalTiling = Unpack(o.Find(x => x.verticalTiling));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Glitch>())
            {
                EditorGUILayout.HelpBox("The Glitch effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Glitch Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Glitch>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(offsetTexture);
            PropertyField(offsetStrength);
            PropertyField(verticalTiling);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Glitch");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Glitch";
    }
#endif
    }
}
