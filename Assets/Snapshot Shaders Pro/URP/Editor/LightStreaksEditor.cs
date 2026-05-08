namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(LightStreaksSettings))]
#else
    [VolumeComponentEditor(typeof(LightStreaksSettings))]
#endif
    public class LightStreaksEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;
        SerializedDataParameter luminanceThreshold;
        SerializedDataParameter downsampleAmount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<LightStreaksSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
            luminanceThreshold = Unpack(o.Find(x => x.luminanceThreshold));
            downsampleAmount = Unpack(o.Find(x => x.downsampleAmount));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<LightStreaks>())
            {
                EditorGUILayout.HelpBox("The Light Streaks effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Light Streaks Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<LightStreaks>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
            PropertyField(luminanceThreshold);
            PropertyField(downsampleAmount);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Light Streaks");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Light Streaks";
    }
#endif
    }
}
