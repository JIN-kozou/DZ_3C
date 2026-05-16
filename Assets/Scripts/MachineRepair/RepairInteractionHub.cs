using System.Collections.Generic;
using DZ_3C.MachineRepair.UI;
using DZ_3C.Reverse;
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
        [SerializeField] private MachineRepairPartIconPrompt partIconPrompt;
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

            if (partIconPrompt == null)
            {
                partIconPrompt = MachineRepairPartIconPrompt.FindInScene();
            }

            partIconPrompt?.BindHub(this);
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

        internal void RegisterPartIconPrompt(MachineRepairPartIconPrompt prompt)
        {
            if (prompt == null)
            {
                return;
            }

            partIconPrompt = prompt;
            prompt.RefreshFromHub(this);
        }

        internal void UnregisterPartIconPrompt(MachineRepairPartIconPrompt prompt)
        {
            if (prompt == null || partIconPrompt != prompt)
            {
                return;
            }

            partIconPrompt = null;
        }

        internal void RegisterPart(MachinePart part, bool inRange)
        {
            if (part == null) return;
            if (inRange) partsInRange.Add(part);
            else partsInRange.Remove(part);
            NotifyPartIconProximityChanged();
        }

        internal void RegisterReceiver(MachinePartReceiver receiver, bool inRange)
        {
            if (receiver == null) return;
            if (inRange) receiversInRange.Add(receiver);
            else receiversInRange.Remove(receiver);
            NotifyPartIconProximityChanged();
        }

        private void NotifyPartIconProximityChanged()
        {
            partIconPrompt?.RefreshFromHub(this);
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

            if (best == null)
            {
                return;
            }

            MachinePartDefinition def = best.Definition;
            if (best.TryPickup(inventory, this))
            {
                pickupBannerQueue?.ShowPickupSuccess(def);
            }
            else if (def != null)
            {
                pickupBannerQueue?.ShowPickupFailedInventoryFull();
            }
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
