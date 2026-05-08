namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SilhouetteSettings))]
#else
    [VolumeComponentEditor(typeof(SilhouetteSettings))]
#endif
    public class SilhouetteEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter nearColor;
        SerializedDataParameter farColor;
        SerializedDataParameter powerRamp;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SilhouetteSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            nearColor = Unpack(o.Find(x => x.nearColor));
            farColor = Unpack(o.Find(x => x.farColor));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Silhouette>())
            {
                EditorGUILayout.HelpBox("The Silhouette effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Silhouette Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Silhouette>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(nearColor);
            PropertyField(farColor);
            PropertyField(powerRamp);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Silhouette");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Silhouette";
    }
#endif
    }
}
