namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(FancyOutlineSettings))]
#else
    [VolumeComponentEditor(typeof(FancyOutlineSettings))]
#endif
    public class FancyOutlineEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter outlineColor;
        SerializedDataParameter colorSensitivity;
        SerializedDataParameter colorStrength;
        SerializedDataParameter depthSensitivity;
        SerializedDataParameter depthStrength;
        SerializedDataParameter normalSensitivity;
        SerializedDataParameter normalStrength;
        SerializedDataParameter depthThreshold;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FancyOutlineSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            outlineColor = Unpack(o.Find(x => x.outlineColor));
            colorSensitivity = Unpack(o.Find(x => x.colorSensitivity));
            colorStrength = Unpack(o.Find(x => x.colorStrength));
            depthSensitivity = Unpack(o.Find(x => x.depthSensitivity));
            depthStrength = Unpack(o.Find(x => x.depthStrength));
            normalSensitivity = Unpack(o.Find(x => x.normalSensitivity));
            normalStrength = Unpack(o.Find(x => x.normalStrength));
            depthThreshold = Unpack(o.Find(x => x.depthThreshold));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<FancyOutline>())
            {
                EditorGUILayout.HelpBox("The Fancy Outlines effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Fancy Outlines Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<FancyOutline>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(outlineColor);
            PropertyField(colorSensitivity);
            PropertyField(colorStrength);
            PropertyField(depthSensitivity);
            PropertyField(depthStrength);
            PropertyField(normalSensitivity);
            PropertyField(normalStrength);
            PropertyField(depthThreshold);
            PropertyField(backgroundColor);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Fancy Outlines");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Fancy Outlines";
    }
#endif
    }
}
