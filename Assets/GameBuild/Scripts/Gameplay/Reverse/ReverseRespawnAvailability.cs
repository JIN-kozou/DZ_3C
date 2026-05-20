using System;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 是否具备可用复活点（已部署逆重阵列，或已激活 Battery 存档点）。
    /// </summary>
    public static class ReverseRespawnAvailability
    {
        public static event Action Changed;

        public static bool HasRespawnPoint(ReverseCoreStack stack)
        {
            if (stack != null && stack.Registry != null && stack.Registry.DeployedCount > 0)
            {
                return true;
            }

            return ReverseBatteryRespawnStore.TryGetBatteryRespawnPoint(out _);
        }

        public static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
