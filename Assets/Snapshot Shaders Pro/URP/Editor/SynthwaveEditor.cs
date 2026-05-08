namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(SynthwaveSettings))]
#else
    [VolumeComponentEditor(typeof(SynthwaveSettings))]
#endif
    public class SynthwaveEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter lineColor1;
        SerializedDataParameter lineColor2;
        SerializedDataParameter lineColorMix;
        SerializedDataParameter lineWidth;
        SerializedDataParameter lineFalloff;
        SerializedDataParameter gapWidth;
        SerializedDataParameter offset;
        SerializedDataParameter axisMask;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SynthwaveSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            lineColor1 = Unpack(o.Find(x => x.lineColor1));
            lineColor2 = Unpack(o.Find(x => x.lineColor2));
            lineColorMix = Unpack(o.Find(x => x.lineColorMix));
            lineWidth = Unpack(o.Find(x => x.lineWidth));
            lineFalloff = Unpack(o.Find(x => x.lineFalloff));
            gapWidth = Unpack(o.Find(x => x.gapWidth));
            offset = Unpack(o.Find(x => x.offset));
            axisMask = Unpack(o.Find(x => x.axisMask));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Synthwave>())
            {
                EditorGUILayout.HelpBox("The Synthwave effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if(GUILayout.Button("Add Synthwave Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Synthwave>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(backgroundColor);
            PropertyField(lineColor1);
            PropertyField(lineColor2);
            PropertyField(lineColorMix);
            PropertyField(lineWidth);
            PropertyField(lineFalloff);
            PropertyField(gapWidth);
            PropertyField(offset);
            PropertyField(axisMask);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Synthwave");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Synthwave";
    }
#endif
    }
}
