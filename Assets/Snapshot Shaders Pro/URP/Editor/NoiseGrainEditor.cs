namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NoiseGrainSettings))]
#else
    [VolumeComponentEditor(typeof(NoiseGrainSettings))]
#endif
    public class NoiseGrainEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter strength;
        SerializedDataParameter speed;
        SerializedDataParameter noiseSize;
        SerializedDataParameter noiseInterpolation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<NoiseGrainSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            strength = Unpack(o.Find(x => x.strength));
            speed = Unpack(o.Find(x => x.speed));
            noiseSize = Unpack(o.Find(x => x.noiseSize));
            noiseInterpolation = Unpack(o.Find(x => x.noiseInterpolation));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<NoiseGrain>())
            {
                EditorGUILayout.HelpBox("The Noise Grain effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Noise Grain Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<NoiseGrain>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(strength);
            PropertyField(speed);
            PropertyField(noiseSize);
            PropertyField(noiseInterpolation);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Noise Grain");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Noise Grain";
    }
#endif
    }
}
