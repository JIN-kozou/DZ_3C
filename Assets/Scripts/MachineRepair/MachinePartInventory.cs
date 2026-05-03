using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// 玩家身上的零件库存。挂在 Player 上；按 MachinePartDefinition 聚合数量。
    /// </summary>
    [DisallowMultipleComponent]
    public class MachinePartInventory : MonoBehaviour
    {
        [Tooltip("可选：按 carryGroupId 配组内总数量上限。未指定时仅应用 Definition 自身上限。")]
        [SerializeField] private MachinePartCarryRules carryRules;

        private readonly Dictionary<MachinePartDefinition, int> counts = new();

        public int GetCount(MachinePartDefinition definition)
        {
            if (definition == null) return 0;
            return counts.TryGetValue(definition, out int n) ? n : 0;
        }

        public void Add(MachinePartDefinition definition, int amount)
        {
            if (definition == null || amount <= 0) return;
            counts.TryGetValue(definition, out int cur);
            counts[definition] = cur + amount;
        }

        /// <summary>
        /// 拾取前校验：同时检查 Definition 单项上限与可选的携带组总上限。
        /// </summary>
        public bool CanAdd(MachinePartDefinition definition, int amount)
        {
            if (definition == null || amount <= 0) return false;

            int current = GetCount(definition);
            int perDefMax = definition.PerDefinitionMaxStack;
            if (perDefMax >= 0 && current + amount > perDefMax) return false;

            string groupId = definition.CarryGroupId;
            if (string.IsNullOrWhiteSpace(groupId)) return true;
            if (carryRules == null) return true;
            if (!carryRules.TryGetGroupMax(groupId, out int groupMax)) return true;

            int inGroupTotal = 0;
            foreach (KeyValuePair<MachinePartDefinition, int> kv in counts)
            {
                MachinePartDefinition def = kv.Key;
                if (def == null) continue;
                if (!string.Equals(def.CarryGroupId, groupId, StringComparison.Ordinal)) continue;
                inGroupTotal += kv.Value;
            }

            return inGroupTotal + amount <= groupMax;
        }

        /// <summary>尝试扣除最多 amount 个，返回实际扣除数量。</summary>
        public int TryConsume(MachinePartDefinition definition, int amount)
        {
            if (definition == null || amount <= 0) return 0;
            if (!counts.TryGetValue(definition, out int cur) || cur <= 0) return 0;
            int take = Mathf.Min(cur, amount);
            cur -= take;
            if (cur <= 0) counts.Remove(definition);
            else counts[definition] = cur;
            return take;
        }

        public IReadOnlyDictionary<MachinePartDefinition, int> Snapshot()
        {
            return new Dictionary<MachinePartDefinition, int>(counts);
        }

        /// <summary>供 Debug 输出：当前快照里每种零件的 id、显示名与数量。</summary>
        public static string FormatSnapshotForDebug(IReadOnlyDictionary<MachinePartDefinition, int> snapshot)
        {
            if (snapshot == null || snapshot.Count == 0) return "(empty)";
            var sb = new StringBuilder();
            foreach (KeyValuePair<MachinePartDefinition, int> kv in snapshot)
            {
                if (kv.Key == null) continue;
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(kv.Key.Id).Append(" [").Append(kv.Key.DisplayName).Append("] x").Append(kv.Value);
            }

            return sb.Length == 0 ? "(empty)" : sb.ToString();
        }

        public event Action InventoryChanged;

        public void AddAndNotify(MachinePartDefinition definition, int amount)
        {
            Add(definition, amount);
            InventoryChanged?.Invoke();
        }

        public int TryConsumeAndNotify(MachinePartDefinition definition, int amount)
        {
            int n = TryConsume(definition, amount);
            if (n > 0) InventoryChanged?.Invoke();
            return n;
        }
    }
}
