namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(FancyNeonSettings))]
#else
    [VolumeComponentEditor(typeof(FancyNeonSettings))]
#endif
    public class FancyNeonEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter colorSensitivity;
        SerializedDataParameter colorStrength;
        SerializedDataParameter depthSensitivity;
        SerializedDataParameter depthStrength;
        SerializedDataParameter normalSensitivity;
        SerializedDataParameter normalStrength;
        SerializedDataParameter depthThreshold;
        SerializedDataParameter saturationFloor;
        SerializedDataParameter lightnessFloor;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter emissiveEdgeColor;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FancyNeonSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            colorSensitivity = Unpack(o.Find(x => x.colorSensitivity));
            colorStrength = Unpack(o.Find(x => x.colorStrength));
            depthSensitivity = Unpack(o.Find(x => x.depthSensitivity));
            depthStrength = Unpack(o.Find(x => x.depthStrength));
            normalSensitivity = Unpack(o.Find(x => x.normalSensitivity));
            normalStrength = Unpack(o.Find(x => x.normalStrength));
            depthThreshold = Unpack(o.Find(x => x.depthThreshold));
            saturationFloor = Unpack(o.Find(x => x.saturationFloor));
            lightnessFloor = Unpack(o.Find(x => x.lightnessFloor));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            emissiveEdgeColor = Unpack(o.Find(x => x.emissiveEdgeColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<FancyNeon>())
            {
                EditorGUILayout.HelpBox("The Fancy Neon effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Fancy Neon Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<FancyNeon>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(colorSensitivity);
            PropertyField(colorStrength);
            PropertyField(depthSensitivity);
            PropertyField(depthStrength);
            PropertyField(normalSensitivity);
            PropertyField(normalStrength);
            PropertyField(depthThreshold);
            PropertyField(saturationFloor);
            PropertyField(lightnessFloor);
            PropertyField(backgroundColor);
            PropertyField(emissiveEdgeColor);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Fancy Neon");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Neon (Fancy)";
    }
#endif
    }
}
