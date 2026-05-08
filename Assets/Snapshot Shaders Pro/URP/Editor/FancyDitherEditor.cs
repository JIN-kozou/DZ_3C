namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(FancyDitherSettings))]
#else
    [VolumeComponentEditor(typeof(FancyDitherSettings))]
#endif
    public class FancyDitherEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter noiseTex;
        SerializedDataParameter noiseSize;
        SerializedDataParameter thresholdOffset;
        SerializedDataParameter blendAmount;
        SerializedDataParameter darkColor;
        SerializedDataParameter lightColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FancyDitherSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            noiseTex = Unpack(o.Find(x => x.noiseTex));
            noiseSize = Unpack(o.Find(x => x.noiseSize));
            thresholdOffset = Unpack(o.Find(x => x.thresholdOffset));
            blendAmount = Unpack(o.Find(x => x.blendAmount));
            darkColor = Unpack(o.Find(x => x.darkColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<FancyDither>())
            {
                EditorGUILayout.HelpBox("The Fancy Dither effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Fancy Dither Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<FancyDither>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(noiseTex);
            PropertyField(noiseSize);
            PropertyField(thresholdOffset);
            PropertyField(blendAmount);
            PropertyField(darkColor);
            PropertyField(lightColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Fancy Dither");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Fancy Dither";
    }
#endif
    }
}
