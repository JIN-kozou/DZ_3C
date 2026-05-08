namespace SnapshotShaders.URP
{
    using UnityEditor.Rendering;
    using UnityEngine.Rendering.Universal;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(UnderwaterSettings))]
#else
    [VolumeComponentEditor(typeof(UnderwaterSettings))]
#endif
    public class UnderwaterEditor : VolumeComponentEditor
    {
        SerializedDataParameter renderPassEvent;
        SerializedDataParameter bumpMap;
        SerializedDataParameter strength;
        SerializedDataParameter waterFogColor;
        SerializedDataParameter fogStrength;

        SerializedDataParameter useCaustics;
        SerializedDataParameter causticsTexture;
        SerializedDataParameter causticsNoiseSpeed;
        SerializedDataParameter causticsNoiseScale;
        SerializedDataParameter causticsNoiseStrength;
        SerializedDataParameter causticsScrollVelocity1;
        SerializedDataParameter causticsScrollVelocity2;
        SerializedDataParameter causticsTiling;
        SerializedDataParameter causticsTint;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<UnderwaterSettings>(serializedObject);
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            bumpMap = Unpack(o.Find(x => x.bumpMap));
            strength = Unpack(o.Find(x => x.strength));
            waterFogColor = Unpack(o.Find(x => x.waterFogColor));
            fogStrength = Unpack(o.Find(x => x.fogStrength));

            useCaustics = Unpack(o.Find(x => x.useCaustics));
            causticsTexture = Unpack(o.Find(x => x.causticsTexture));
            causticsNoiseSpeed = Unpack(o.Find(x => x.causticsNoiseSpeed));
            causticsNoiseScale = Unpack(o.Find(x => x.causticsNoiseScale));
            causticsNoiseStrength = Unpack(o.Find(x => x.causticsNoiseStrength));
            causticsScrollVelocity1 = Unpack(o.Find(x => x.causticsScrollVelocity1));
            causticsScrollVelocity2 = Unpack(o.Find(x => x.causticsScrollVelocity2));
            causticsTiling = Unpack(o.Find(x => x.causticsTiling));
            causticsTint = Unpack(o.Find(x => x.causticsTint));
        }

        public override void OnInspectorGUI()
        {
            if (!SnapshotUtility.CheckEffectEnabled<Underwater>())
            {
                EditorGUILayout.HelpBox("The Underwater effect must be added to your renderer's Renderer Features list.", MessageType.Error);
                if (GUILayout.Button("Add Underwater Renderer Feature"))
                {
                    SnapshotUtility.AddEffectToPipelineAsset<Underwater>();
                }
            }

            PropertyField(renderPassEvent);
            PropertyField(bumpMap);
            PropertyField(strength);
            PropertyField(waterFogColor);
            PropertyField(fogStrength);

            PropertyField(useCaustics);
            PropertyField(causticsTexture);
            PropertyField(causticsNoiseSpeed);
            PropertyField(causticsNoiseScale);
            PropertyField(causticsNoiseStrength);
            PropertyField(causticsScrollVelocity1);
            PropertyField(causticsScrollVelocity2);
            PropertyField(causticsTiling);
            PropertyField(causticsTint);
        }

#if UNITY_2021_2_OR_NEWER
        public override GUIContent GetDisplayTitle()
        {
            return new GUIContent("Underwater");
        }
#else
    public override string GetDisplayTitle()
    {
        return "Underwater";
    }
#endif
    }
}
