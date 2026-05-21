using System;
using System.Collections.Generic;
using System.Text;
using DZ_3C.MachineRepair.UI;
using DZ_3C.UI.WorldInteraction;
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

        [Header("Requirements")]
        [Tooltip("若指定：运行时 Awake 从该预设填充下方需求列表（进入 Play 后以预设为准）；留空则完全使用 Inspector 中手动配置的需求。")]
        [SerializeField] private MachinePartReceiverPreset requirementPreset;

        [SerializeField] private List<PartRequirement> requirements = new();

        [Tooltip("勾选时：Awake 不调用 ApplyReceiverBox，不自动换 mesh/材质与根 Transform 缩放，完全使用 prefab 上的 MeshFilter / MeshRenderer / Transform。")]
        [SerializeField] private bool usePrefabMesh = false;

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Vector3 receiverScale = new Vector3(4f, 0.35f, 2.5f);
        [SerializeField] private ItemAudio itemAudio;

        [Header("Proximity UI")]
        [SerializeField] private MachinePartReceiverUIView proximityUi;
        [SerializeField] private WorldInteractionPromptAnchor promptAnchor;
        [SerializeField] private string submitPromptText = "提交零件";

        [SerializeField] private MachinePartReceiverRevealDriver revealDriver;

        public IReadOnlyList<PartRequirement> Requirements => requirements;

        /// <summary>挂接了预设 SO 时返回预设里的名称；否则为 null。</summary>
        public string ReceiverPresetDisplayName =>
            requirementPreset != null ? requirementPreset.PresetName : null;

        private void Reset()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void Awake()
        {
            ApplyPresetIfAssigned();

            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (itemAudio == null) itemAudio = GetComponent<ItemAudio>();
            if (meshFilter != null && meshRenderer != null && !usePrefabMesh)
            {
                RepairMeshUtility.ApplyReceiverBox(transform, meshFilter, meshRenderer, receiverScale);
            }

            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;

            if (proximityUi == null)
            {
                proximityUi = GetComponentInChildren<MachinePartReceiverUIView>(true);
            }

            EnsurePromptAnchor();

            if (revealDriver == null)
            {
                revealDriver = GetComponent<MachinePartReceiverRevealDriver>();
            }
        }

        /// <summary>按零件数量加权：sum(delivered) / sum(countRequired)，无有效需求时为 0。</summary>
        public float GetItemWeightedCompletionRatio()
        {
            if (requirements == null || requirements.Count == 0)
            {
                return 0f;
            }

            int deliveredTotal = 0;
            int requiredTotal = 0;
            for (int i = 0; i < requirements.Count; i++)
            {
                PartRequirement req = requirements[i];
                if (req == null || req.part == null)
                {
                    continue;
                }

                int count = req.countRequired < 1 ? 1 : req.countRequired;
                requiredTotal += count;
                deliveredTotal += Mathf.Min(req.delivered, count);
            }

            return requiredTotal > 0 ? deliveredTotal / (float)requiredTotal : 0f;
        }

        public bool IsLineSatisfied(PartRequirement req)
        {
            if (req == null || req.part == null)
            {
                return false;
            }

            return req.delivered >= req.countRequired;
        }

        public bool AreAllRequirementsSatisfied()
        {
            if (requirements == null || requirements.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                PartRequirement req = requirements[i];
                if (req == null || req.part == null)
                {
                    continue;
                }

                if (!IsLineSatisfied(req))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>UI 从上到下：一级 → 二级 → 三级 → 四级，再按 Category / 名称。</summary>
        public List<PartRequirement> GetRequirementsInDisplayOrder()
        {
            var ordered = new List<PartRequirement>();
            if (requirements == null)
            {
                return ordered;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                PartRequirement req = requirements[i];
                if (req != null && req.part != null)
                {
                    ordered.Add(req);
                }
            }

            ordered.Sort(CompareRequirementsForDisplay);
            return ordered;
        }

        private static int CompareRequirementsForDisplay(PartRequirement a, PartRequirement b)
        {
            int rankA = GetDisplayTierRank(a.part);
            int rankB = GetDisplayTierRank(b.part);
            int rankCompare = rankA.CompareTo(rankB);
            if (rankCompare != 0)
            {
                return rankCompare;
            }

            int categoryCompare = a.part.Category.CompareTo(b.part.Category);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            return string.Compare(a.part.DisplayName, b.part.DisplayName, StringComparison.Ordinal);
        }

        private static int GetDisplayTierRank(MachinePartDefinition part)
        {
            if (part == null)
            {
                return 999;
            }

            string name = part.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                return (int)part.Category * 10;
            }

            if (name.Contains("一级", StringComparison.Ordinal))
            {
                return 1;
            }

            if (name.Contains("二级", StringComparison.Ordinal))
            {
                return 2;
            }

            if (name.Contains("三级", StringComparison.Ordinal))
            {
                return 3;
            }

            if (name.Contains("四级", StringComparison.Ordinal))
            {
                return 4;
            }

            return 100 + (int)part.Category;
        }

        private void ApplyPresetIfAssigned()
        {
            if (requirementPreset == null) return;

            requirements.Clear();
            IReadOnlyList<MachinePartReceiverPreset.PresetLine> src = requirementPreset.Lines;
            if (src == null) return;

            foreach (MachinePartReceiverPreset.PresetLine line in src)
            {
                if (line == null || line.part == null) continue;
                int count = line.countRequired < 1 ? 1 : line.countRequired;
                requirements.Add(new PartRequirement { part = line.part, countRequired = count });
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            hub.RegisterReceiver(this, true);
            WorldInteractionPromptManager.EnsureOnPlayer(hub.GetComponent<Player>());
            promptAnchor?.SetPlayerInRange(true);
            promptAnchor?.SetAvailable(true);
            if (proximityUi != null)
            {
                proximityUi.SetVisible(true);
                proximityUi.Refresh(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            RepairInteractionHub hub = FindHub(other);
            if (hub == null) return;
            hub.RegisterReceiver(this, false);
            promptAnchor?.SetPlayerInRange(false);
            proximityUi?.SetVisible(false);
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
            string presetLabel = ReceiverPresetDisplayName;
            string logContext = string.IsNullOrEmpty(presetLabel)
                ? receiverName
                : $"{receiverName} (preset: {presetLabel})";

            if (inventory == null)
            {
                Debug.Log($"[MachineRepair] Submit on '{logContext}': inventory is null, skip.");
                return false;
            }

            if (requirements == null || requirements.Count == 0)
            {
                Debug.Log(
                    $"[MachineRepair] Submit on '{logContext}': no requirements configured. " +
                    $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inventory.Snapshot())}");
                return false;
            }

            bool any = false;
            List<PartRequirement> submitOrder = GetRequirementsInDisplayOrder();
            for (int i = 0; i < submitOrder.Count; i++)
            {
                PartRequirement req = submitOrder[i];
                if (req.part == null)
                {
                    continue;
                }

                if (IsLineSatisfied(req))
                {
                    continue;
                }

                int need = req.countRequired - req.delivered;
                if (need <= 0)
                {
                    continue;
                }

                int have = inventory.GetCount(req.part);
                if (have <= 0)
                {
                    break;
                }

                int give = Mathf.Min(have, need);
                int taken = inventory.TryConsumeAndNotify(req.part, give);
                if (taken > 0)
                {
                    req.delivered += taken;
                    any = true;
                }

                break;
            }

            Debug.Log(
                $"[MachineRepair] Submit on '{logContext}'. DeliveredThisPress={any}. " +
                $"Inventory: {MachinePartInventory.FormatSnapshotForDebug(inventory.Snapshot())}. " +
                $"Receiver still needs: {FormatRemainingNeedsForDebug(requirements)}");

            if (any)
            {
                PlaySubmitAudio();
            }

            proximityUi?.Refresh(this);

            if (any)
            {
                MachinePartReceiversSceneGate.NotifyReceiverProgressChanged();
                revealDriver?.RefreshFromRequirements();
            }

            return any;
        }

        private void PlaySubmitAudio()
        {
            if (itemAudio == null)
            {
                itemAudio = GetComponentInChildren<ItemAudio>(true);
            }

            if (itemAudio == null)
            {
                return;
            }

            itemAudio.PlaySubmit();
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

            promptAnchor.Configure(submitPromptText, WorldInteractionMode.Tap, "E");
        }
    }
}
