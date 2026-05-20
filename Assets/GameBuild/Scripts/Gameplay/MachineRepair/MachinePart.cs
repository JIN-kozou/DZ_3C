using DZ_3C.UI.WorldInteraction;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class MachinePart : MonoBehaviour
    {
        private const string DefaultPickupPrompt = "拾取零件";

        [SerializeField] private MachinePartDefinition definition;
        [SerializeField] private WorldInteractionPromptAnchor promptAnchor;
        [SerializeField] private string pickupPromptText = DefaultPickupPrompt;
        [Tooltip("开启时：不在运行时按 Definition 的 VisualShape 重建 mesh/材质，完全使用 prefab 上的 MeshFilter/缩放/材质。")]
        [SerializeField] private bool usePrefabMesh = true;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private ItemAudio itemAudio;

        private Collider col;
        private RepairInteractionHub cachedHub;

        private void Reset()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (itemAudio == null) itemAudio = GetComponent<ItemAudio>();
            col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            EnsurePromptAnchor();
        }

        private void Start()
        {
            if (usePrefabMesh) return;
            if (definition != null)
            {
                ApplyVisual();
            }
        }

        public MachinePartDefinition Definition => definition;

        public void Configure(MachinePartDefinition def)
        {
            definition = def;
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (definition == null || meshFilter == null || meshRenderer == null) return;
            RepairMeshUtility.ApplyPartVisual(transform, definition.VisualShape, meshFilter, meshRenderer);
        }

        private void OnTriggerEnter(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            cachedHub = hub;
            hub.RegisterPart(this, true);
            WorldInteractionPromptManager.EnsureOnPlayer(hub.GetComponent<Player>());
            promptAnchor?.SetPlayerInRange(true);
            promptAnchor?.SetAvailable(true);
        }

        private void OnTriggerExit(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            hub.RegisterPart(this, false);
            if (cachedHub == hub) cachedHub = null;
            promptAnchor?.SetPlayerInRange(false);
        }

        private static RepairInteractionHub FindHub(Collider other)
        {
            Player p = other.GetComponent<Player>();
            if (p == null) p = other.GetComponentInParent<Player>();
            return p != null ? p.GetComponent<RepairInteractionHub>() : null;
        }

        internal bool TryPickup(MachinePartInventory inv, RepairInteractionHub hub)
        {
            if (definition == null || inv == null) return false;
            if (!inv.CanAdd(definition, 1))
            {
                Debug.Log(
                    $"[MachineRepair] Pickup blocked by carry limit: id={definition.Id}, displayName={definition.DisplayName}. " +
                    $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inv.Snapshot())}");
                return false;
            }

            inv.AddAndNotify(definition, 1);
            Debug.Log(
                $"[MachineRepair] Picked up part: id={definition.Id}, displayName={definition.DisplayName}. " +
                $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inv.Snapshot())}");
            hub.RegisterPart(this, false);
            PlayPickupAudio();
            Destroy(gameObject);
            return true;
        }

        private void PlayPickupAudio()
        {
            if (itemAudio == null)
            {
                itemAudio = GetComponentInChildren<ItemAudio>(true);
            }

            if (itemAudio == null)
            {
                return;
            }

            itemAudio.PlayPickup();
        }

        private void OnDestroy()
        {
            if (cachedHub != null)
            {
                cachedHub.RegisterPart(this, false);
            }

            if (promptAnchor != null)
            {
                promptAnchor.SetPlayerInRange(false);
                promptAnchor.SetAvailable(false);
            }
        }

        private void EnsurePromptAnchor()
        {
            if (promptAnchor == null)
            {
                promptAnchor = GetComponent<WorldInteractionPromptAnchor>();
            }

            if (promptAnchor == null)
            {
                promptAnchor = gameObject.AddComponent<WorldInteractionPromptAnchor>();
            }

            promptAnchor.Configure(pickupPromptText, WorldInteractionMode.Tap, "E");
        }
    }
}
