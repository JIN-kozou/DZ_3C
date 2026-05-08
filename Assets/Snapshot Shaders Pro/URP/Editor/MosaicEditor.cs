namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(MosaicSettings))]
#else
    [VolumeComponentEditor(typeof(MosaicSettings))]
#endif
    public class MosaicEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter overlayTexture;
        SerializedDataParameter overlayColor;
        SerializedDataParameter xTileCount;
        SerializedDataParameter usePointFiltering;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MosaicSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            overlayTexture = Unpack(o.Find(x => x.overlayTexture));
            overlayColor = Unpack(o.Find(x => x.overlayColor));
            xTileCount = Unpack(o.Find(x => x.xTileCount));
            usePointFiltering = Unpack(o.Find(x => x.usePointFiltering));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Mosaic>())
            {
                EditorGUILayout.HelpBox("The Mosaic effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Mosaic Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Mosaic>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(overlayTexture);
            PropertyField(overlayColor);
            PropertyField(xTileCount);
            PropertyField(usePointFiltering);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Mosaic");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Mosaic";
    }
#endif
    }
}
