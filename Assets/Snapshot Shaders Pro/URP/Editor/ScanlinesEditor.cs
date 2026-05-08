namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(ScanlinesSettings))]
#else
    [VolumeComponentEditor(typeof(ScanlinesSettings))]
#endif
    public class ScanlinesEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter scanlineTex;
        SerializedDataParameter strength;
        SerializedDataParameter size;
        SerializedDataParameter scrollSpeed;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScanlinesSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            scanlineTex = Unpack(o.Find(x => x.scanlineTex));
            strength = Unpack(o.Find(x => x.strength));
            size = Unpack(o.Find(x => x.size));
            scrollSpeed = Unpack(o.Find(x => x.scrollSpeed));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Scanlines>())
            {
                EditorGUILayout.HelpBox("The Scanlines effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Scanlines Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Scanlines>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(scanlineTex);
            PropertyField(strength);
            PropertyField(size);
            PropertyField(scrollSpeed);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Scanlines");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Scanlines";
    }
#endif
    }
}
