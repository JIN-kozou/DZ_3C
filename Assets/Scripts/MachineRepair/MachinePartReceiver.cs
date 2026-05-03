using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class MachinePartReceiver : MonoBehaviour
    {
        [Serializable]
        public class PartRequirement
        {
            public MachinePartDefinition part;
            [Min(1)] public int countRequired = 1;
            [NonSerialized] public int delivered;
        }

        [SerializeField] private List<PartRequirement> requirements = new();

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Vector3 receiverScale = new Vector3(4f, 0.35f, 2.5f);

        public IReadOnlyList<PartRequirement> Requirements => requirements;

        private void Reset()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter != null && meshRenderer != null)
            {
                RepairMeshUtility.ApplyReceiverBox(transform, meshFilter, meshRenderer, receiverScale);
            }

            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            hub.RegisterReceiver(this, true);
        }

        private void OnTriggerExit(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            hub.RegisterReceiver(this, false);
        }

        private static RepairInteractionHub FindHub(Collider other)
        {
            Player p = other.GetComponent<Player>();
            if (p == null) p = other.GetComponentInParent<Player>();
            return p != null ? p.GetComponent<RepairInteractionHub>() : null;
        }

        /// <summary>按需求从库存扣减，能交多少交多少；有提交则返回 true。</summary>
        public bool TrySubmitAllFrom(MachinePartInventory inventory)
        {
            string receiverName = gameObject.name;
            if (inventory == null)
            {
                Debug.Log($"[MachineRepair] Submit on '{receiverName}': inventory is null, skip.");
                return false;
            }

            if (requirements == null || requirements.Count == 0)
            {
                Debug.Log(
                    $"[MachineRepair] Submit on '{receiverName}': no requirements configured. " +
                    $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inventory.Snapshot())}");
                return false;
            }

            bool any = false;
            foreach (PartRequirement req in requirements)
            {
                if (req.part == null) continue;
                int need = req.countRequired - req.delivered;
                if (need <= 0) continue;
                int have = inventory.GetCount(req.part);
                if (have <= 0) continue;
                int give = Mathf.Min(have, need);
                int taken = inventory.TryConsumeAndNotify(req.part, give);
                if (taken > 0)
                {
                    req.delivered += taken;
                    any = true;
                }
            }

            Debug.Log(
                $"[MachineRepair] Submit on '{receiverName}'. DeliveredThisPress={any}. " +
                $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inventory.Snapshot())}. " +
                $"Receiver still needs: {FormatRemainingNeedsForDebug(requirements)}");

            return any;
        }

        private static string FormatRemainingNeedsForDebug(List<PartRequirement> reqs)
        {
            var sb = new StringBuilder();
            foreach (PartRequirement req in reqs)
            {
                if (req.part == null) continue;
                int remaining = req.countRequired - req.delivered;
                if (remaining <= 0) continue;
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(req.part.Id).Append(" [").Append(req.part.DisplayName).Append("] need ").Append(remaining);
            }

            return sb.Length == 0 ? "(all satisfied)" : sb.ToString();
        }
    }
}
