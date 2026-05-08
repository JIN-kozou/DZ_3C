namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(RadialBlurSettings))]
#else
    [VolumeComponentEditor(typeof(RadialBlurSettings))]
#endif
    public class RadialBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;
        SerializedDataParameter stepSize;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<RadialBlurSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
            stepSize = Unpack(o.Find(x => x.stepSize));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<RadialBlur>())
            {
                EditorGUILayout.HelpBox("The Radial Blur effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Radial Blur Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<RadialBlur>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
            PropertyField(stepSize);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Radial Blur");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Radial Blur";
    }
#endif
    }
}
