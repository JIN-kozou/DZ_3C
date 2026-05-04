using UnityEditor;
using UnityEngine;

namespace GameSoftCraft
{
    [CustomEditor(typeof(StarfieldMatManager))]
    public class StarfieldMatManagerEditor : Editor
    {
        StarfieldMatManager _manager;

        SerializedObject _skyMatSerializedObject;

        void OnSkyMaterialUndo ()
        {
            if (_manager != null && _manager.skyMaterial != null) {
                _manager.skyMaterial.UpdateMaterialProperties();
            }
        }

        private void OnEnable ()
        {
            _manager = (StarfieldMatManager)target;
            Undo.undoRedoPerformed += OnSkyMaterialUndo;
            _skyMatSerializedObject = _manager.skyMaterial != null
                ? new SerializedObject(_manager.skyMaterial)
                : null;
        }

        private void OnDisable ()
        {
            Undo.undoRedoPerformed -= OnSkyMaterialUndo;
        }

        public override void OnInspectorGUI ()
        {
            EditorGUILayout.HelpBox(
                "When Assign To Render Settings is on, this component overwrites Lighting > Environment > Skybox Material whenever it refreshes. Turn it off to switch skyboxes manually or while using GameSoftCraft/S.P.A.C.E > Bake selected skybox material to Cubemap.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            DrawDefaultInspector();

            if (_manager.skyMaterial == null) {
                EditorGUILayout.HelpBox("Assign Sky Material to edit underlay and stars below.", MessageType.Warning);
                return;
            }

            if (_skyMatSerializedObject == null) {
                _skyMatSerializedObject = new SerializedObject(_manager.skyMaterial);
            }

            _skyMatSerializedObject.Update();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            SkyMaterialEditor.DrawInspector(_skyMatSerializedObject, _manager.skyMaterial);
        }
    }
}
