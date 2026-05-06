using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// 记录最近一次成功激活的 Battery Zone 重生点。
    /// </summary>
    public static class ReverseBatteryRespawnStore
    {
        private static bool hasRespawnPoint;
        private static Vector3 respawnWorldPosition;

        public static void SaveBatteryRespawnPoint(Transform zoneTransform)
        {
            if (zoneTransform == null) return;
            SaveBatteryRespawnPoint(zoneTransform.position);
        }

        public static void SaveBatteryRespawnPoint(Vector3 worldPosition)
        {
            respawnWorldPosition = worldPosition;
            hasRespawnPoint = true;
        }

        public static bool TryGetBatteryRespawnPoint(out Vector3 worldPosition)
        {
            worldPosition = respawnWorldPosition;
            return hasRespawnPoint;
        }

        public static void Clear()
        {
            hasRespawnPoint = false;
            respawnWorldPosition = default;
        }
    }
}
