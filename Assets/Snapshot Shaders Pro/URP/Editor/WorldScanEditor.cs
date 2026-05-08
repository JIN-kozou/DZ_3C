namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(WorldScanSettings))]
#else
    [VolumeComponentEditor(typeof(WorldScanSettings))]
#endif
    public class WorldScanEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter scanOrigin;
        SerializedDataParameter scanDistance;
        SerializedDataParameter scanWidth;
        SerializedDataParameter overlayRampTex;
        SerializedDataParameter overlayColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<WorldScanSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            scanOrigin = Unpack(o.Find(x => x.scanOrigin));
            scanDistance = Unpack(o.Find(x => x.scanDistance));
            scanWidth = Unpack(o.Find(x => x.scanWidth));
            overlayRampTex = Unpack(o.Find(x => x.overlayRampTex));
            overlayColor = Unpack(o.Find(x => x.overlayColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<WorldScan>())
            {
                EditorGUILayout.HelpBox("The World Scan effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add World Scan Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<WorldScan>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(scanOrigin);
            PropertyField(scanDistance);
            PropertyField(scanWidth);
            PropertyField(overlayRampTex);
            PropertyField(overlayColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("World Scan");
        }
#else
        public override string GetDisplayTitle()
    {
        return "World Scan";
    }
#endif
    }
}
