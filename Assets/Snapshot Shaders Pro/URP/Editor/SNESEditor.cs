namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SNESSettings))]
#else
    [VolumeComponentEditor(typeof(SNESSettings))]
#endif
    public class SNESEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter bandingValues;
        SerializedDataParameter powerRamp;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SNESSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            bandingValues = Unpack(o.Find(x => x.bandingValues));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<SNES>())
            {
                EditorGUILayout.HelpBox("The SNES effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add SNES Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<SNES>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(bandingValues);
            PropertyField(powerRamp);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("SNES");
        }
#else
    public override string GetDisplayTitle()
    {
        return "SNES";
    }
#endif
    }
}
