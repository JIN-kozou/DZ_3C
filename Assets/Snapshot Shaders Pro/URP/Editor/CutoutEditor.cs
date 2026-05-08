namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(CutoutSettings))]
#else
    [VolumeComponentEditor(typeof(CutoutSettings))]
#endif
    public class CutoutEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter cutoutTexture;
        SerializedDataParameter borderColor;
        SerializedDataParameter stretch;
        SerializedDataParameter zoom;
        SerializedDataParameter offset;
        SerializedDataParameter rotation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CutoutSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            cutoutTexture = Unpack(o.Find(x => x.cutoutTexture));
            borderColor = Unpack(o.Find(x => x.borderColor));
            stretch = Unpack(o.Find(x => x.stretch));
            zoom = Unpack(o.Find(x => x.zoom));
            offset = Unpack(o.Find(x => x.offset));
            rotation = Unpack(o.Find(x => x.rotation));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Cutout>())
            {
                EditorGUILayout.HelpBox("The Cutout effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Cutout Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Cutout>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(cutoutTexture);
            PropertyField(borderColor);
            PropertyField(stretch);
            PropertyField(zoom);
            PropertyField(offset);
            PropertyField(rotation);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Cutout");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Cutout";
    }
#endif
    }
}
