using UnityEngine;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// 演示：确保 Player 上有库存与交互中心，并在玩家旁生成 3 个零件 + 1 个空接收器。
    /// 将本组件挂在场景任意物体上（或空物体 MachineRepairDemo）。
    /// Definition 可在 Inspector 指定；留空则从 Resources/Config/MachineRepair/ 加载。
    /// </summary>
    public class MachineRepairBootstrap : MonoBehaviour
    {
        [SerializeField] private bool spawnOnAwake = true;

        [Header("Definitions (optional — fallback Resources)")]
        [SerializeField] private MachinePartDefinition tier1Definition;
        [SerializeField] private MachinePartDefinition keyDefinition;
        [SerializeField] private MachinePartDefinition tier2Definition;

        [Header("Layout (world offsets from player position)")]
        [SerializeField] private Vector3 tier1Offset = new Vector3(2f, 0.4f, 0f);
        [SerializeField] private Vector3 keyOffset = new Vector3(2.3f, 0.4f, 1f);
        [SerializeField] private Vector3 tier2Offset = new Vector3(2.3f, 0.4f, -1f);
        [SerializeField] private Vector3 receiverOffset = new Vector3(5.5f, 0.25f, 0f);

        private void Awake()
        {
            if (!spawnOnAwake) return;

            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogWarning("[MachineRepairBootstrap] No Player in scene.");
                return;
            }

            EnsurePlayerComponents(player.gameObject);
            LoadDefinitionsIfNeeded();

            Vector3 basePos = player.transform.position;

            SpawnPart(basePos + tier1Offset, tier1Definition, "MachinePart_Tier1");
            SpawnPart(basePos + keyOffset, keyDefinition, "MachinePart_Key");
            SpawnPart(basePos + tier2Offset, tier2Definition, "MachinePart_Tier2");
            SpawnReceiver(basePos + receiverOffset);
        }

        private static void EnsurePlayerComponents(GameObject playerGo)
        {
            if (playerGo.GetComponent<MachinePartInventory>() == null)
            {
                playerGo.AddComponent<MachinePartInventory>();
            }

            if (playerGo.GetComponent<RepairInteractionHub>() == null)
            {
                playerGo.AddComponent<RepairInteractionHub>();
            }
        }

        private void LoadDefinitionsIfNeeded()
        {
            if (tier1Definition == null)
            {
                tier1Definition = Resources.Load<MachinePartDefinition>("Config/MachineRepair/Tier1Material");
            }

            if (keyDefinition == null)
            {
                keyDefinition = Resources.Load<MachinePartDefinition>("Config/MachineRepair/KeySpecial");
            }

            if (tier2Definition == null)
            {
                tier2Definition = Resources.Load<MachinePartDefinition>("Config/MachineRepair/Tier2Material");
            }

            if (tier1Definition == null || keyDefinition == null || tier2Definition == null)
            {
                Debug.LogWarning(
                    "[MachineRepairBootstrap] Missing MachinePartDefinition assets. Create them under Resources/Config/MachineRepair/ or assign in Inspector.");
            }
        }

        private static void SpawnPart(Vector3 worldPos, MachinePartDefinition def, string objectName)
        {
            if (def == null) return;

            GameObject go = new GameObject(objectName);
            go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.65f;
            MachinePart mp = go.AddComponent<MachinePart>();
            mp.Configure(def);
        }

        private void SpawnReceiver(Vector3 worldPos)
        {
            GameObject go = new GameObject("MachinePartReceiver_Empty");
            go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 0.4f, 2.5f);
            box.center = Vector3.zero;
            go.AddComponent<MachinePartReceiver>();
        }
    }
}
