namespace DZ_3C.UI.WorldInteraction
{
  public interface IWorldInteractionHoldProgress
  {
    float NormalizedProgress { get; }

    /// <summary>长按进行中时为 true，用于锁定角色移动。</summary>
    bool BlocksMovement { get; }
  }
}
