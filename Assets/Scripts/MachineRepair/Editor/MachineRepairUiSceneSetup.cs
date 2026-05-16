#if UNITY_EDITOR
using DZ_3C.MachineRepair.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.MachineRepair.Editor
{
    public static class MachineRepairUiSceneSetup
    {
        private const string UiTestScenePath = "Assets/AnimancerController/Scenes/UITest.unity";
        private const string SuccessBannerPath = "Assets/Prefab/UI/getGearBanner.prefab";
        private const string FailBannerPath = "Assets/Prefab/UI/failgetGearBanner.prefab";
        private const string ExamplesFolderName = "BannerExamples";

        [MenuItem("DZ_3C/Machine Repair/Wire UITest Machine Repair UI")]
        public static void WireUiTestScene()
        {
            var scene = EditorSceneManager.OpenScene(UiTestScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[MachineRepair] Could not open scene: {UiTestScenePath}");
                return;
            }

            if (!MachineRepairUiLocator.TryResolveHudWidgets(
                    out Transform inventoryRoot,
                    out Transform tabRoot,
                    out RectTransform bannerStackParent))
            {
                Debug.LogError("[MachineRepair] Inventory or TAB not found under HUD/Canvas.");
                return;
            }

            TextMeshProUGUI tabHint = tabRoot.Find("title (1)")?.GetComponent<TextMeshProUGUI>();
            Text amount = inventoryRoot.Find("amount")?.GetComponent<Text>();
            Transform gearUi = inventoryRoot.Find("GearUI");
            Transform customButton = inventoryRoot.Find("TierButton");
            if (customButton == null)
            {
                customButton = inventoryRoot.Find("customButton");
            }

            Transform emptyRoot = inventoryRoot.Find("emptyInventoryHint");
            if (emptyRoot == null)
            {
                GameObject emptyGo = new GameObject("emptyInventoryHint", typeof(RectTransform), typeof(Text));
                emptyGo.transform.SetParent(inventoryRoot, false);
                RectTransform emptyRect = emptyGo.GetComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.pivot = new Vector2(0.5f, 0.5f);
                emptyRect.anchoredPosition = new Vector2(-80f, -8f);
                emptyRect.sizeDelta = new Vector2(120f, 24f);
                Text emptyText = emptyGo.GetComponent<Text>();
                emptyText.text = "库存为空";
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.fontSize = 12;
                emptyText.color = Color.white;
                emptyGo.SetActive(false);
                emptyRoot = emptyGo.transform;
            }

            Transform stackTransform = MachineRepairPickupBannerQueue.FindBannerStackTransform(bannerStackParent);
            if (stackTransform == null)
            {
                GameObject stackGo = new GameObject("StackRoot", typeof(RectTransform));
                stackGo.transform.SetParent(bannerStackParent, false);
                stackTransform = stackGo.transform;
            }

            MachineRepairPickupBannerQueue queue = stackTransform.GetComponent<MachineRepairPickupBannerQueue>();
            if (queue == null)
            {
                queue = stackTransform.gameObject.AddComponent<MachineRepairPickupBannerQueue>();
            }

            queue.EnsureStackRootLayout();

            FindBannerLayoutExamples(bannerStackParent, out RectTransform layoutExample, out RectTransform failExample);
            Vector2 anchorPos = layoutExample != null ? layoutExample.anchoredPosition : new Vector2(-851.5f, 369.58f);
            float spacing = 65f;
            if (layoutExample != null && failExample != null)
            {
                spacing = Mathf.Abs(layoutExample.anchoredPosition.y - failExample.anchoredPosition.y);
                if (spacing < 40f)
                {
                    spacing = 65f;
                }
            }

            SerializedObject queueSo = new SerializedObject(queue);
            queueSo.FindProperty("stackRoot").objectReferenceValue = stackTransform as RectTransform;
            queueSo.FindProperty("layoutReference").objectReferenceValue = layoutExample;
            queueSo.FindProperty("failLayoutReference").objectReferenceValue = failExample;
            queueSo.FindProperty("stackAnchorPosition").vector2Value = anchorPos;
            queueSo.FindProperty("stackSpacing").floatValue = spacing;
            queueSo.FindProperty("successBannerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(SuccessBannerPath);
            queueSo.FindProperty("failBannerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(FailBannerPath);
            queueSo.ApplyModifiedPropertiesWithoutUndo();

            OrganizeBannerExamples(bannerStackParent, layoutExample, failExample);

            if (stackTransform != layoutExample?.parent && layoutExample != null)
            {
                int stackSiblingIndex = layoutExample.GetSiblingIndex();
                stackTransform.SetSiblingIndex(stackSiblingIndex);
            }

            GameObject uiHost = GameObject.Find("MachineRepairUI");
            if (uiHost == null)
            {
                uiHost = new GameObject("MachineRepairUI");
            }

            if (uiHost.GetComponent<MachineRepairUiSceneAnchor>() == null)
            {
                uiHost.AddComponent<MachineRepairUiSceneAnchor>();
            }

            MachineRepairInventoryPanel panel = uiHost.GetComponent<MachineRepairInventoryPanel>();
            if (panel == null)
            {
                panel = uiHost.AddComponent<MachineRepairInventoryPanel>();
            }

            MachinePartInventory inventory = Object.FindObjectOfType<MachinePartInventory>();
            SerializedObject panelSo = new SerializedObject(panel);
            panelSo.FindProperty("inventory").objectReferenceValue = inventory;
            panelSo.FindProperty("tier1Definition").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MachinePartDefinition>(
                    "Assets/Resources/Config/MachineRepair/Tier1Material.asset");
            panelSo.FindProperty("panelRoot").objectReferenceValue = inventoryRoot.gameObject;
            panelSo.FindProperty("tabHintText").objectReferenceValue = tabHint;
            panelSo.FindProperty("amountText").objectReferenceValue = amount;
            panelSo.FindProperty("gearUiRoot").objectReferenceValue = gearUi != null ? gearUi.gameObject : null;
            panelSo.FindProperty("customButtonRoot").objectReferenceValue =
                customButton != null ? customButton.gameObject : null;
            panelSo.FindProperty("customButtonLabel").objectReferenceValue =
                customButton != null ? customButton.GetComponentInChildren<Text>(true) : null;
            panelSo.FindProperty("emptyInventoryRoot").objectReferenceValue = emptyRoot.gameObject;
            panelSo.FindProperty("startPanelClosed").boolValue = true;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            InterfaceAnimManager inventoryIam = inventoryRoot.GetComponent<InterfaceAnimManager>();
            if (inventoryIam != null)
            {
                inventoryIam.autoStart = false;
                EditorUtility.SetDirty(inventoryIam);
            }

            inventoryRoot.gameObject.SetActive(false);

            if (layoutExample != null)
            {
                layoutExample.gameObject.SetActive(true);
            }

            if (failExample != null)
            {
                failExample.gameObject.SetActive(true);
            }

            AddBannerViewToPrefab(SuccessBannerPath);
            AddBannerViewToPrefab(FailBannerPath);
            SyncBannerPrefabLayoutFromExamples(layoutExample, failExample);

            RepairInteractionHub[] hubs = Object.FindObjectsOfType<RepairInteractionHub>();
            for (int i = 0; i < hubs.Length; i++)
            {
                SerializedObject hubSo = new SerializedObject(hubs[i]);
                hubSo.FindProperty("pickupBannerQueue").objectReferenceValue = queue;
                hubSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                "[MachineRepair] UITest wired. Banner stack: stackRoot (under HUD/Canvas). " +
                "Scene examples moved to BannerExamples (kept for layout reference).");
        }

        private static void FindBannerLayoutExamples(
            Transform hudCanvas,
            out RectTransform success,
            out RectTransform fail)
        {
            success = null;
            fail = null;

            Transform examples = hudCanvas.Find(ExamplesFolderName);
            if (examples != null)
            {
                success = examples.Find("getGearBanner") as RectTransform;
                fail = examples.Find("getGearBanner_fail_example") as RectTransform;
                if (fail == null)
                {
                    fail = examples.Find("getGearBanner (1)") as RectTransform;
                }
            }

            if (success == null)
            {
                success = hudCanvas.Find("getGearBanner") as RectTransform;
            }

            if (fail == null)
            {
                fail = hudCanvas.Find("getGearBanner_fail_example") as RectTransform;
                if (fail == null)
                {
                    fail = hudCanvas.Find("getGearBanner (1)") as RectTransform;
                }
            }
        }

        private static void SyncBannerPrefabLayoutFromExamples(
            RectTransform successExample,
            RectTransform failExample)
        {
            if (successExample != null)
            {
                ApplyExampleLayoutToPrefab(SuccessBannerPath, successExample);
            }

            if (failExample != null)
            {
                ApplyExampleLayoutToPrefab(FailBannerPath, failExample);
            }
        }

        private static void ApplyExampleLayoutToPrefab(string prefabPath, RectTransform example)
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null || example == null)
            {
                return;
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            RectTransform target = instance.GetComponent<RectTransform>();
            if (target != null)
            {
                MachineRepairRectLayoutMirror.CopyFromExample(example, target);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
        }

        private static void OrganizeBannerExamples(
            Transform hudCanvas,
            RectTransform layoutExample,
            RectTransform failExample)
        {
            Transform examplesRoot = hudCanvas.Find(ExamplesFolderName);
            if (examplesRoot == null)
            {
                GameObject folder = new GameObject(ExamplesFolderName, typeof(RectTransform));
                folder.transform.SetParent(hudCanvas, false);
                RectTransform folderRect = folder.GetComponent<RectTransform>();
                folderRect.anchorMin = Vector2.zero;
                folderRect.anchorMax = Vector2.one;
                folderRect.offsetMin = Vector2.zero;
                folderRect.offsetMax = Vector2.zero;
                examplesRoot = folder.transform;
            }

            if (layoutExample != null && layoutExample.parent != examplesRoot)
            {
                layoutExample.SetParent(examplesRoot, true);
                layoutExample.name = "getGearBanner";
            }

            if (failExample != null && failExample.parent != examplesRoot)
            {
                failExample.SetParent(examplesRoot, true);
                failExample.name = "getGearBanner_fail_example";
            }

            if (examplesRoot.GetComponent<MachineRepairBannerExamplesHideInPlay>() == null)
            {
                examplesRoot.gameObject.AddComponent<MachineRepairBannerExamplesHideInPlay>();
            }

            examplesRoot.gameObject.SetActive(true);
        }

        private static void AddBannerViewToPrefab(string path)
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
            {
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefabRoot);
            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (instance.GetComponent<GearPickupBannerView>() == null)
            {
                instance.AddComponent<GearPickupBannerView>();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }
}
#endif
