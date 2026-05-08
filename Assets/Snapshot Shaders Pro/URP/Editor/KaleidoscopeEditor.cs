namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(KaleidoscopeSettings))]
#else
    [VolumeComponentEditor(typeof(KaleidoscopeSettings))]
#endif
    public class KaleidoscopeEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter segmentCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<KaleidoscopeSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            segmentCount = Unpack(o.Find(x => x.segmentCount));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Kaleidoscope>())
            {
                EditorGUILayout.HelpBox("The Kaleidoscope effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Kaleidoscope Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Kaleidoscope>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(segmentCount);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Kaleidoscope");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Kaleidoscope";
    }
#endif
    }
}
