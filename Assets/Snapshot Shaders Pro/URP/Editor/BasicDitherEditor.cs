namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(BasicDitherSettings))]
#else
    [VolumeComponentEditor(typeof(BasicDitherSettings))]
#endif
    public class BasicDitherEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter noiseTex;
        SerializedDataParameter noiseSize;
        SerializedDataParameter thresholdOffset;
        SerializedDataParameter darkColor;
        SerializedDataParameter lightColor;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<BasicDitherSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            noiseTex = Unpack(o.Find(x => x.noiseTex));
            noiseSize = Unpack(o.Find(x => x.noiseSize));
            thresholdOffset = Unpack(o.Find(x => x.thresholdOffset));
            darkColor = Unpack(o.Find(x => x.darkColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<BasicDither>())
            {
                EditorGUILayout.HelpBox("The Basic Dither effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Basic Dither Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<BasicDither>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(noiseTex);
            PropertyField(noiseSize);
            PropertyField(thresholdOffset);
            PropertyField(darkColor);
            PropertyField(lightColor);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Basic Dither");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Basic Dither";
    }
#endif
    }
}
