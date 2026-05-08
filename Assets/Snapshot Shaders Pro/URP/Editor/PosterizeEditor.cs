namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(PosterizeSettings))]
#else
    [VolumeComponentEditor(typeof(PosterizeSettings))]
#endif
    public class PosterizeEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter redLevels;
        SerializedDataParameter greenLevels;
        SerializedDataParameter blueLevels;
        SerializedDataParameter powerRamp;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PosterizeSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            redLevels = Unpack(o.Find(x => x.redLevels));
            greenLevels = Unpack(o.Find(x => x.greenLevels));
            blueLevels = Unpack(o.Find(x => x.blueLevels));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Posterize>())
            {
                EditorGUILayout.HelpBox("The Posterize effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Posterize Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Posterize>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(redLevels);
            PropertyField(greenLevels);
            PropertyField(blueLevels);
            PropertyField(powerRamp);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Posterize");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Posterize";
    }
#endif
    }
}
