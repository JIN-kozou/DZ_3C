using System.Collections.Generic;
using DZ_3C.MachineRepair.UI;
using DZ_3C.Reverse;
using DZ_3C.UI.WorldInteraction;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private ItemAudio itemAudio;
        [SerializeField] private MachineRepairPickupBannerQueue pickupBannerQueue;
        [SerializeField] private KeyCode dropKey = KeyCode.G;
        [SerializeField, Min(0f)] private float dropForwardDistance = 1.2f;
        [SerializeField, Min(0f)] private float dropUpOffset = 0.25f;
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
            if (itemAudio == null) itemAudio = GetComponent<ItemAudio>();
            if (itemAudio == null) itemAudio = GetComponentInChildren<ItemAudio>(true);
            if (pickupBannerQueue == null)
            {
                pickupBannerQueue = MachineRepairPickupBannerQueue.FindInScene();
            }

            if (pickupBannerQueue == null)
            {
                pickupBannerQueue = MachineRepairPickupBannerQueue.CreateDefaultUnderCanvas();
            }

            WorldInteractionPromptManager.EnsureOnPlayer(player);
        }

        public MachinePartInventory Inventory => inventory;//只读属性，允许被.add

        public bool HasAnyPartInRange()
        {
            foreach (MachinePart part in partsInRange)
            {
                if (part != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyReceiverInRange()
        {
            foreach (MachinePartReceiver receiver in receiversInRange)
            {
                if (receiver != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void NotifyPickupSuccess(MachinePartDefinition definition)
        {
            pickupBannerQueue?.ShowPickupSuccess(definition);
        }

        public void NotifyPickupFailedInventoryFull()
        {
            pickupBannerQueue?.ShowPickupFailedInventoryFull();
        }

        public bool TrySubmitFocusedReceiver(MachinePartReceiver receiver)
        {
            if (receiver == null || !receiversInRange.Contains(receiver))
            {
                return false;
            }

            return receiver.TrySubmitAllFrom(inventory);
        }

        public bool TryPickupFocusedPart(MachinePart part)
        {
            if (part == null || !partsInRange.Contains(part))
            {
                return false;
            }

            MachinePartDefinition def = part.Definition;
            if (part.TryPickup(inventory, this))
            {
                NotifyPickupSuccess(def);
                return true;
            }

            if (def != null)
            {
                NotifyPickupFailedInventoryFull();
            }

            return false;
        }

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

            if (IsDropPressedThisFrame())
            {
                TryDropFirstInventoryPart();
                return;
            }

            bool inBatteryZone = ReverseBatteryZone.IsPlayerInsideAnyBatteryZone(player);
            bool isHeld = inputService.Interactive;

            if (!inBatteryZone)
            {
                interactiveHoldSeconds = 0f;
                wasInteractiveHeldLastFrame = isHeld;
                if (!inputService.InteractiveWasPressedThisFrame) return;
                TryPerformCrosshairTapInteraction();
                return;
            }

            if (isHeld) interactiveHoldSeconds += Time.deltaTime;

            if (!isHeld && wasInteractiveHeldLastFrame)
            {
                float holdThreshold = reverseConfig != null ? reverseConfig.batteryZoneHoldDuration : 1.2f;
                bool treatedAsBatteryLongPress = inBatteryZone && interactiveHoldSeconds >= holdThreshold;
                if (!treatedAsBatteryLongPress)
                {
                    TryPerformCrosshairTapInteraction();
                }

                interactiveHoldSeconds = 0f;
            }

            if (!isHeld && !wasInteractiveHeldLastFrame)
            {
                interactiveHoldSeconds = 0f;
            }

            wasInteractiveHeldLastFrame = isHeld;
        }

        private void TryPerformCrosshairTapInteraction()
        {
            WorldInteractionPromptManager manager = WorldInteractionPromptManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.TryPerformFocusedTapInteraction(this);
        }

        private bool TryDropFirstInventoryPart()
        {
            IReadOnlyDictionary<MachinePartDefinition, int> snapshot = inventory.Snapshot();
            foreach (KeyValuePair<MachinePartDefinition, int> kv in snapshot)
            {
                MachinePartDefinition definition = kv.Key;
                if (definition == null || kv.Value <= 0)
                {
                    continue;
                }

                int consumed = inventory.TryConsumeAndNotify(definition, 1);
                if (consumed <= 0)
                {
                    return false;
                }

                SpawnDroppedPart(definition);
                PlayDropAudio();
                return true;
            }

            return false;
        }

        private void SpawnDroppedPart(MachinePartDefinition definition)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 position = transform.position + forward * dropForwardDistance + Vector3.up * dropUpOffset;
            GameObject go = new GameObject($"MachinePart_Dropped_{definition.Id}");
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.65f;
            MachinePart part = go.AddComponent<MachinePart>();
            part.Configure(definition);
        }

        private void PlayDropAudio()
        {
            if (itemAudio == null)
            {
                itemAudio = GetComponent<ItemAudio>();
            }

            if (itemAudio == null)
            {
                itemAudio = GetComponentInChildren<ItemAudio>(true);
            }

            itemAudio?.PlayDrop();
        }

        private bool IsDropPressedThisFrame()
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (dropKey)
            {
                case KeyCode.G: return Keyboard.current.gKey.wasPressedThisFrame;
                case KeyCode.Q: return Keyboard.current.qKey.wasPressedThisFrame;
                case KeyCode.F: return Keyboard.current.fKey.wasPressedThisFrame;
                case KeyCode.E: return Keyboard.current.eKey.wasPressedThisFrame;
                default: return false;
            }
        }
    }
}
