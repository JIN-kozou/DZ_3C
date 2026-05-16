#if UNITY_EDITOR
using DZ_3C.MachineRepair.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DZ_3C.MachineRepair.Editor
{
    public static class MachineRepairReceiverUiSceneSetup
    {
        private const string LevelTestScenePath = "Assets/AnimancerController/Scenes/Level test.unity";
        private const string ReceiverUiPrefabPath = "Assets/Prefab/UI/ReceiverUI.prefab";

        [MenuItem("DZ_3C/Machine Repair/Wire All Receiver UI In Open Scene")]
        public static void WireAllReceiversInOpenScene()
        {
            WireAllReceivers(SceneManager.GetActiveScene());
        }

        [MenuItem("DZ_3C/Machine Repair/Wire Level Test Receiver UI")]
        public static void WireLevelTestReceivers()
        {
            var scene = EditorSceneManager.OpenScene(LevelTestScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[MachineRepair] Could not open: {LevelTestScenePath}");
                return;
            }

            WireAllReceivers(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void WireAllReceivers(Scene scene)
        {
            GameObject receiverUiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReceiverUiPrefabPath);
            if (receiverUiPrefab == null)
            {
                Debug.LogError($"[MachineRepair] Missing prefab: {ReceiverUiPrefabPath}");
                return;
            }

            MachinePartReceiver[] receivers = Object.FindObjectsOfType<MachinePartReceiver>(true);
            if (receivers.Length == 0)
            {
                Debug.LogWarning($"[MachineRepair] No MachinePartReceiver in scene '{scene.name}'.");
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                worldCamera = Object.FindObjectOfType<Camera>();
            }

            int wired = 0;
            for (int i = 0; i < receivers.Length; i++)
            {
                if (WireSingleReceiver(receivers[i], receiverUiPrefab, worldCamera))
                {
                    wired++;
                }
            }

            Debug.Log(
                $"[MachineRepair] Wired ReceiverUI on {wired}/{receivers.Length} receiver(s) in '{scene.name}'. " +
                "Requirements were not modified.");
        }

        private static bool WireSingleReceiver(
            MachinePartReceiver receiver,
            GameObject receiverUiPrefab,
            Camera worldCamera)
        {
            if (receiver == null)
            {
                return false;
            }

            MachinePartReceiverUIView view = receiver.GetComponentInChildren<MachinePartReceiverUIView>(true);
            if (view == null)
            {
                Transform existing = receiver.transform.Find("ReceiverUI");
                GameObject instanceRoot;
                if (existing != null)
                {
                    instanceRoot = existing.gameObject;
                    view = instanceRoot.GetComponent<MachinePartReceiverUIView>();
                    if (view == null)
                    {
                        view = instanceRoot.AddComponent<MachinePartReceiverUIView>();
                    }
                }
                else
                {
                    instanceRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                        receiverUiPrefab,
                        receiver.transform);
                    if (instanceRoot == null)
                    {
                        Debug.LogWarning($"[MachineRepair] Failed to instantiate ReceiverUI on {receiver.name}.", receiver);
                        return false;
                    }

                    instanceRoot.name = "ReceiverUI";
                    instanceRoot.transform.SetSiblingIndex(0);
                    view = instanceRoot.GetComponent<MachinePartReceiverUIView>();
                }
            }

            Canvas canvas = view.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && worldCamera != null)
            {
                canvas.worldCamera = worldCamera;
            }

            view.TryAutoBindReferences();

            SerializedObject viewSo = new SerializedObject(view);
            Transform panel = view.transform.Find("Panel") ?? view.transform;
            Transform winorLose = panel.Find("winorLose");
            if (winorLose != null)
            {
                viewSo.FindProperty("statusTextTmp").objectReferenceValue =
                    winorLose.GetComponent<TextMeshProUGUI>();
            }

            viewSo.ApplyModifiedPropertiesWithoutUndo();

            InterfaceAnimManager iam = view.GetComponent<InterfaceAnimManager>();
            if (iam == null)
            {
                iam = view.GetComponentInChildren<InterfaceAnimManager>(true);
            }

            if (iam != null)
            {
                iam.autoStart = false;
                EditorUtility.SetDirty(iam);
            }

            SerializedObject receiverSo = new SerializedObject(receiver);
            receiverSo.FindProperty("proximityUi").objectReferenceValue = view;
            receiverSo.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(false);
            EditorUtility.SetDirty(receiver);
            EditorUtility.SetDirty(view);

            return true;
        }
    }
}
#endif
