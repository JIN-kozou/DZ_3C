namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SobelNeonSettings))]
#else
    [VolumeComponentEditor(typeof(SobelNeonSettings))]
#endif
    public class SobelNeonEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter saturationFloor;
        SerializedDataParameter lightnessFloor;
        SerializedDataParameter backgroundColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SobelNeonSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            saturationFloor = Unpack(o.Find(x => x.saturationFloor));
            lightnessFloor = Unpack(o.Find(x => x.lightnessFloor));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<SobelNeon>())
            {
                EditorGUILayout.HelpBox("The Sobel Neon effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Sobel Neon Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<SobelNeon>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(saturationFloor);
            PropertyField(lightnessFloor);
            PropertyField(backgroundColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Neon (Sobel)");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Neon (Sobel)";
    }
#endif
    }
}
