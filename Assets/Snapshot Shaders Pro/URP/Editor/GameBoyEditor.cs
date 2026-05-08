namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(GameBoySettings))]
#else
    [VolumeComponentEditor(typeof(GameBoySettings))]
#endif
    public class GameBoyEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter enabled;
        SerializedDataParameter darkestColor;
        SerializedDataParameter darkColor;
        SerializedDataParameter lightColor;
        SerializedDataParameter lightestColor;
        SerializedDataParameter powerRamp;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GameBoySettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            enabled = Unpack(o.Find(x => x.enabled));
            darkestColor = Unpack(o.Find(x => x.darkestColor));
            darkColor = Unpack(o.Find(x => x.darkColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
            lightestColor = Unpack(o.Find(x => x.lightestColor));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<GameBoy>())
            {
                EditorGUILayout.HelpBox("The Game Boy effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Game Boy Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<GameBoy>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(enabled);
            PropertyField(darkestColor);
            PropertyField(darkColor);
            PropertyField(lightColor);
            PropertyField(lightestColor);
            PropertyField(powerRamp);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Game Boy");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Game Boy";
    }
#endif
    }
}
