namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(GreyscaleSettings))]
#else
    [VolumeComponentEditor(typeof(GreyscaleSettings))]
#endif
    public class GreyscaleEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GreyscaleSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Greyscale>())
            {
                EditorGUILayout.HelpBox("The Greyscale effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Greyscale Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Greyscale>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Greyscale");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Greyscale";
    }
#endif
    }
}
