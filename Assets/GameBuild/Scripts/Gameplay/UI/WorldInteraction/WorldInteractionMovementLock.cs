namespace DZ_3C.UI.WorldInteraction
{
  /// <summary>
  /// 长按交互期间由 <see cref="WorldInteractionPromptManager"/> 置位，<see cref="InputService"/> 读取并屏蔽移动输入。
  /// </summary>
  public static class WorldInteractionMovementLock
  {
    public static bool IsLocked { get; private set; }

    internal static void SetLocked(bool locked)
    {
      IsLocked = locked;
    }
  }
}
