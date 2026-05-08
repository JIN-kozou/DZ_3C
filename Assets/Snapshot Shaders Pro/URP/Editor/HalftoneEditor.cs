namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(HalftoneSettings))]
#else
    [VolumeComponentEditor(typeof(HalftoneSettings))]
#endif
    public class HalftoneEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter halftoneTexture;
        SerializedDataParameter softness;
        SerializedDataParameter textureSize;
        SerializedDataParameter minMaxLuminance;
        SerializedDataParameter darkColor;
        SerializedDataParameter lightColor;
        SerializedDataParameter useSceneColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HalftoneSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            halftoneTexture = Unpack(o.Find(x => x.halftoneTexture));
            softness = Unpack(o.Find(x => x.softness));
            textureSize = Unpack(o.Find(x => x.textureSize));
            minMaxLuminance = Unpack(o.Find(x => x.minMaxLuminance));
            darkColor = Unpack(o.Find(x => x.darkColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Halftone>())
            {
                EditorGUILayout.HelpBox("The Halftone effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Halftone Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Halftone>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(halftoneTexture);
            PropertyField(softness);
            PropertyField(textureSize);
            PropertyField(minMaxLuminance);
            PropertyField(darkColor);
            PropertyField(lightColor);
            PropertyField(useSceneColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Halftone");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Halftone";
    }
#endif
    }
}
