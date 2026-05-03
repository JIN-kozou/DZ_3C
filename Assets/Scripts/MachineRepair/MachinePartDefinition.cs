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

        public string Id => id;
        public string DisplayName => displayName;
        public MachinePartCategory Category => category;
        public PartVisualShape VisualShape => visualShape;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name.Replace(" ", "").ToLowerInvariant();
            }
        }
    }
}
