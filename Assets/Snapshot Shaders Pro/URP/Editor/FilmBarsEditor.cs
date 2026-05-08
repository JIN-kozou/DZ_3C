namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(FilmBarsSettings))]
#else
    [VolumeComponentEditor(typeof(FilmBarsSettings))]
#endif
    public class FilmBarsEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter aspect;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FilmBarsSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            aspect = Unpack(o.Find(x => x.aspect));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<FilmBars>())
            {
                EditorGUILayout.HelpBox("The Film Bars effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Film Bars Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<FilmBars>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(aspect);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Film Bars");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Film Bars";
    }
#endif
    }
}
