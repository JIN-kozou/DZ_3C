namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(TextAdventureSettings))]
#else
    [VolumeComponentEditor(typeof(TextAdventureSettings))]
#endif
    public class TextAdventureEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter characterSize;
        SerializedDataParameter characterAtlas;
        SerializedDataParameter characterCount;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter characterColor;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TextAdventureSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            characterSize = Unpack(o.Find(x => x.characterSize));
            characterAtlas = Unpack(o.Find(x => x.characterAtlas));
            characterCount = Unpack(o.Find(x => x.characterCount));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            characterColor = Unpack(o.Find(x => x.characterColor));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<TextAdventure>())
            {
                EditorGUILayout.HelpBox("The Text Adventure effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Text Adventure Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<TextAdventure>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(characterSize);
            PropertyField(characterAtlas);
            PropertyField(characterCount);
            PropertyField(backgroundColor);
            PropertyField(characterColor);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Text Adventure");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Text Adventure";
    }
#endif
    }
}
