using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    [CreateAssetMenu(menuName = "DZ_3C/Machine Repair/Machine Part Carry Rules", fileName = "MachinePartCarryRules")]
    public class MachinePartCarryRules : ScriptableObject
    {
        [Serializable]
        public class GroupLimit
        {
            public string groupId = "generic_bulk";
            [Min(1)] public int maxTotalCountInGroup = 1;
        }

        [SerializeField] private List<GroupLimit> groupLimits = new();

        public bool TryGetGroupMax(string groupId, out int maxTotalCount)
        {
            maxTotalCount = -1;
            if (string.IsNullOrWhiteSpace(groupId) || groupLimits == null) return false;

            foreach (GroupLimit g in groupLimits)
            {
                if (g == null || string.IsNullOrWhiteSpace(g.groupId)) continue;
                if (!string.Equals(g.groupId, groupId, StringComparison.Ordinal)) continue;
                maxTotalCount = Mathf.Max(1, g.maxTotalCountInGroup);
                return true;
            }

            return false;
        }
    }
}
