using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// 接收器需求预设：名称 + 各行零件种类与数量。若挂在 MachinePartReceiver 上，运行时 Awake 会从该 SO 填充需求列表。
    /// </summary>
    [CreateAssetMenu(menuName = "DZ_3C/Machine Repair/Machine Part Receiver Preset", fileName = "MachinePartReceiverPreset")]
    public class MachinePartReceiverPreset : ScriptableObject
    {
        [Serializable]
        public class PresetLine
        {
            public MachinePartDefinition part;
            [Min(1)] public int countRequired = 1;
        }

        [SerializeField] private string presetName = "Receiver";

        [SerializeField] private List<PresetLine> lines = new();

        public string PresetName => presetName;

        public IReadOnlyList<PresetLine> Lines => lines ?? (IReadOnlyList<PresetLine>)Array.Empty<PresetLine>();
    }
}
