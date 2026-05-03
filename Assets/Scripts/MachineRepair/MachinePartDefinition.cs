using UnityEngine;

namespace DZ_3C.MachineRepair
{
    [CreateAssetMenu(menuName = "DZ_3C/Machine Repair/Machine Part Definition", fileName = "MachinePartDefinition")]
    public class MachinePartDefinition : ScriptableObject
    {
        [SerializeField] private string id = "part.unknown";
        [SerializeField] private string displayName = "Part";
        [SerializeField] private MachinePartCategory category = MachinePartCategory.GenericTier1;
        [SerializeField] private PartVisualShape visualShape = PartVisualShape.Triangle;
        [Tooltip("同组内总数量共享上限。为空表示不参与组上限（如 key）。")]
        [SerializeField] private string carryGroupId = string.Empty;
        [Tooltip("该 Definition 的单独堆叠上限。-1 表示无限。")]
        [SerializeField] private int perDefinitionMaxStack = -1;

        public string Id => id;
        public string DisplayName => displayName;
        public MachinePartCategory Category => category;
        public PartVisualShape VisualShape => visualShape;
        public string CarryGroupId => carryGroupId;
        public int PerDefinitionMaxStack => perDefinitionMaxStack;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name.Replace(" ", "").ToLowerInvariant();
            }

            if (perDefinitionMaxStack < -1)
            {
                perDefinitionMaxStack = -1;
            }
        }
    }
}
