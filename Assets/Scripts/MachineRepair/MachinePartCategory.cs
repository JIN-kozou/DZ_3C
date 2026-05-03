namespace DZ_3C.MachineRepair
{
    /// <summary>零件分类，用于按「类型 + 数量」配置需求（与具体 Definition 正交）。</summary>
    public enum MachinePartCategory
    {
        GenericTier1 = 0,
        GenericTier2 = 1,
        StoryUnique = 2
    }

    /// <summary>演示用外形，用于生成简单网格。</summary>
    public enum PartVisualShape
    {
        Triangle = 0,
        Ellipse = 1,
        Circle = 2
    }
}
