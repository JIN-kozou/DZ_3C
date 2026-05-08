namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(BarrelDistortionSettings))]
#else
    [VolumeComponentEditor(typeof(BarrelDistortionSettings))]
#endif
    public class BarrelDistortionEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;
        SerializedDataParameter backgroundColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<BarrelDistortionSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<BarrelDistortion>())
            {
                EditorGUILayout.HelpBox("The Barrel Distortion effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Barrel Distortion Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<BarrelDistortion>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
            PropertyField(backgroundColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Barrel Distortion");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Barrel Distortion";
    }
#endif
    }
}
