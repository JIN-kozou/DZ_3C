#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DZ_3C.MachineRepair;

namespace DZ_3C.MachineRepair.Editor
{
    public static class MachineRepairSetupMenu
    {
        private const string ResourcesDir = "Assets/Resources/Config/MachineRepair";
        private const string PrefabPath = "Assets/Prefab/MachineRepair/MachineRepair_DemoRoot.prefab";
        private const string TriangleMeshPath = "Assets/Prefab/MachineRepair/Meshes/MachinePart_Triangle.asset";
        private const string PrefabTier1 = "Assets/Prefab/MachineRepair/MachinePart_Tier1.prefab";
        private const string PrefabKey = "Assets/Prefab/MachineRepair/MachinePart_Key.prefab";
        private const string PrefabTier2 = "Assets/Prefab/MachineRepair/MachinePart_Tier2.prefab";

        [MenuItem("DZ_3C/Machine Repair/Bake Triangle Mesh + Wire Part Prefabs")]
        public static void BakeTriangleMeshAndWirePartPrefabs()
        {
            EnsureTriangleMeshAsset();
            WireMachinePartPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MachineRepair] Triangle mesh baked and part prefabs wired (prefab mesh, no runtime VisualShape swap).");
        }

        [MenuItem("DZ_3C/Machine Repair/Create Definition Assets + Demo Prefab")]
        public static void CreateDefinitionsAndDemoPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Config"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Config");
            }

            if (!AssetDatabase.IsValidFolder(ResourcesDir))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Config", "MachineRepair");
            }

            CreateOrUpdateDefinition(
                $"{ResourcesDir}/Tier1Material.asset",
                "part.tier1",
                "\u4E00\u7EA7\u96F6\u4EF6",
                MachinePartCategory.GenericTier1,
                PartVisualShape.Triangle);

            CreateOrUpdateDefinition(
                $"{ResourcesDir}/KeySpecial.asset",
                "part.key",
                "\u94A5\u5319",
                MachinePartCategory.StoryUnique,
                PartVisualShape.Ellipse);

            CreateOrUpdateDefinition(
                $"{ResourcesDir}/Tier2Material.asset",
                "part.tier2",
                "\u4E8C\u7EA7\u96F6\u4EF6",
                MachinePartCategory.GenericTier2,
                PartVisualShape.Circle);

            if (!AssetDatabase.IsValidFolder("Assets/Prefab/MachineRepair"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefab"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefab");
                }

                AssetDatabase.CreateFolder("Assets/Prefab", "MachineRepair");
            }

            GameObject root = new GameObject("MachineRepair_DemoRoot");
            root.AddComponent<MachineRepairBootstrap>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            EnsureTriangleMeshAsset();
            WireMachinePartPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MachineRepair] Created definition assets and demo prefab. Drag MachineRepair_DemoRoot into your scene.");
        }

        private static void EnsureTriangleMeshAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefab/MachineRepair/Meshes"))
            {
                AssetDatabase.CreateFolder("Assets/Prefab/MachineRepair", "Meshes");
            }

            Mesh mesh = RepairMeshUtility.CreateTriangleMesh(0.65f);
            mesh.name = "MachinePart_Triangle";

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(TriangleMeshPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(TriangleMeshPath);
            }

            AssetDatabase.CreateAsset(mesh, TriangleMeshPath);
            EditorUtility.SetDirty(mesh);
        }

        private static void WireMachinePartPrefabs()
        {
            Mesh tri = AssetDatabase.LoadAssetAtPath<Mesh>(TriangleMeshPath);
            if (tri == null)
            {
                Debug.LogError("[MachineRepair] Missing triangle mesh asset. Run Bake Triangle Mesh first.");
                return;
            }

            WirePrefabMeshAndScale(PrefabTier1, tri, Vector3.one);
            WirePrefabMeshAndScale(PrefabKey, null, new Vector3(0.55f, 0.38f, 0.78f));
            WirePrefabMeshAndScale(PrefabTier2, null, new Vector3(0.48f, 0.48f, 0.48f));
        }

        /// <summary>mesh 为 null 时保留 prefab 上原有 mesh（如内置球体）。</summary>
        private static void WirePrefabMeshAndScale(string prefabPath, Mesh meshOrNull, Vector3 localScale)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[MachineRepair] Prefab not found, skip: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var mf = root.GetComponent<MeshFilter>();
                if (mf != null && meshOrNull != null)
                {
                    mf.sharedMesh = meshOrNull;
                }

                root.transform.localScale = localScale;

                MachinePart part = root.GetComponent<MachinePart>();
                if (part != null)
                {
                    SerializedObject so = new SerializedObject(part);
                    SerializedProperty p = so.FindProperty("usePrefabMesh");
                    if (p != null)
                    {
                        p.boolValue = true;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateOrUpdateDefinition(
            string path,
            string id,
            string displayName,
            MachinePartCategory category,
            PartVisualShape shape)
        {
            MachinePartDefinition def = AssetDatabase.LoadAssetAtPath<MachinePartDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<MachinePartDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            SerializedObject so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("visualShape").enumValueIndex = (int)shape;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }
    }
}
#endif
