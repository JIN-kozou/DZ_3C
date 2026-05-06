using System.Collections.Generic;
using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.MachineRepair
{
    /// <summary>
    /// 应挂在Player上
    /// 该组件为交互中枢：把「按哪个键」「当前在范围内的零件 / 接收器」「玩家身上的库存」三件事串起来，在玩家按下交互键时决定先交接收器还是先捡零件。
    /// </summary>
    [DisallowMultipleComponent]
    public class RepairInteractionHub : MonoBehaviour
    {
        private readonly HashSet<MachinePart> partsInRange = new();
        private readonly HashSet<MachinePartReceiver> receiversInRange = new();

        private MachinePartInventory inventory;
        private Player player;
        private ReverseConfig reverseConfig;
        private bool wasInteractiveHeldLastFrame;
        private float interactiveHoldSeconds;

        private void Awake()
        {
            inventory = GetComponent<MachinePartInventory>();
            if (inventory == null)
            {
                inventory = gameObject.AddComponent<MachinePartInventory>();
            }
            player = GetComponent<Player>();
            reverseConfig = Resources.Load<ReverseConfig>("Config/Reverse/ReverseConfig");
        }

        public MachinePartInventory Inventory => inventory;//只读属性，允许被.add

        internal void RegisterPart(MachinePart part, bool inRange)
        {
            if (part == null) return;
            if (inRange) partsInRange.Add(part);
            else partsInRange.Remove(part);
        }

        internal void RegisterReceiver(MachinePartReceiver receiver, bool inRange)
        {
            if (receiver == null) return;
            if (inRange) receiversInRange.Add(receiver);
            else receiversInRange.Remove(receiver);
        }

        private void Update()
        {
            var inputService = InputService.Instance;
            if (inputService == null) return;
            if (inventory == null) return;

            bool inBatteryZone = ReverseBatteryZone.IsPlayerInsideAnyBatteryZone(player);
            bool isHeld = inputService.Interactive;

            if (!inBatteryZone)
            {
                interactiveHoldSeconds = 0f;
                wasInteractiveHeldLastFrame = isHeld;
                if (!inputService.InteractiveWasPressedThisFrame) return;
                if (TrySubmitNearestReceiver()) return;
                TryPickupNearestPart();
                return;
            }

            if (isHeld) interactiveHoldSeconds += Time.deltaTime;

            if (!isHeld && wasInteractiveHeldLastFrame)
            {
                float holdThreshold = reverseConfig != null ? reverseConfig.batteryZoneHoldDuration : 1.2f;
                bool treatedAsBatteryLongPress = inBatteryZone && interactiveHoldSeconds >= holdThreshold;
                if (!treatedAsBatteryLongPress)
                {
                    if (TrySubmitNearestReceiver()) return;
                    TryPickupNearestPart();
                }

                interactiveHoldSeconds = 0f;
            }

            if (!isHeld && !wasInteractiveHeldLastFrame)
            {
                interactiveHoldSeconds = 0f;
            }

            wasInteractiveHeldLastFrame = isHeld;
        }

        private bool TrySubmitNearestReceiver()
        {
            MachinePartReceiver best = null;
            float bestSqr = float.MaxValue;
            Vector3 p = transform.position;
            foreach (var r in receiversInRange)
            {
                if (r == null) continue;
                float s = (r.transform.position - p).sqrMagnitude;
                if (s < bestSqr)
                {
                    bestSqr = s;
                    best = r;
                }
            }

            if (best == null) return false;
            return best.TrySubmitAllFrom(inventory);
        }

        private void TryPickupNearestPart()
        {
            MachinePart best = null;
            float bestSqr = float.MaxValue;
            Vector3 p = transform.position;
            foreach (var part in partsInRange)
            {
                if (part == null) continue;
                float s = (part.transform.position - p).sqrMagnitude;
                if (s < bestSqr)
                {
                    bestSqr = s;
                    best = part;
                }
            }

            best?.TryPickup(inventory, this);
        }
    }
}
