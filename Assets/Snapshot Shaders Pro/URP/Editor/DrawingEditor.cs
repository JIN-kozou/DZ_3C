namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(DrawingSettings))]
#else
    [VolumeComponentEditor(typeof(DrawingSettings))]
#endif
    public class DrawingEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter drawingTex;
        SerializedDataParameter animCycleTime;
        SerializedDataParameter strength;
        SerializedDataParameter tiling;
        SerializedDataParameter smudge;
        SerializedDataParameter depthThreshold;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DrawingSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            drawingTex = Unpack(o.Find(x => x.drawingTex));
            animCycleTime = Unpack(o.Find(x => x.animCycleTime));
            strength = Unpack(o.Find(x => x.strength));
            tiling = Unpack(o.Find(x => x.tiling));
            smudge = Unpack(o.Find(x => x.smudge));
            depthThreshold = Unpack(o.Find(x => x.depthThreshold));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Drawing>())
            {
                EditorGUILayout.HelpBox("The Drawing effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Drawing Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Drawing>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(drawingTex);
            PropertyField(animCycleTime);
            PropertyField(strength);
            PropertyField(tiling);
            PropertyField(smudge);
            PropertyField(depthThreshold);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Drawing");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Drawing";
    }
#endif
    }
}
