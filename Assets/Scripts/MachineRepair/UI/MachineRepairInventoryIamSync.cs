using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Coordinates InterfaceAnimManager with inventory conditional widgets (gear / tier button / empty hint).
    /// </summary>
    public static class MachineRepairInventoryIamSync
    {
        public static void ResetAllElementsForAppear(InterfaceAnimManager iam)
        {
            if (iam == null)
            {
                return;
            }

            foreach (InterfaceAnmElement element in iam.elementsList)
            {
                if (element == null || element.gameObjectRef == null)
                {
                    continue;
                }

                element.currentState = CSFHIAnimableState.disappeared;
                element.gameObjectRef.SetActive(false);
            }
        }

        public static void PrepareConditionalBeforeAppear(
            InterfaceAnimManager iam,
            bool inventoryEmpty,
            bool hasTier1,
            GameObject gearUiRoot,
            GameObject tierButtonRoot,
            GameObject emptyInventoryRoot)
        {
            if (iam == null)
            {
                return;
            }

            iam.autoStart = false;

            foreach (InterfaceAnmElement element in iam.elementsList)
            {
                if (element == null || element.gameObjectRef == null)
                {
                    continue;
                }

                GameObject target = element.gameObjectRef;
                if (IsGearOrTier(target, gearUiRoot, tierButtonRoot))
                {
                    ConfigureForIamReveal(element, allowIamToReveal: hasTier1);
                }
                else if (IsEmptyHint(target, emptyInventoryRoot))
                {
                    ConfigureForIamReveal(element, allowIamToReveal: inventoryEmpty);
                }
            }
        }

        public static void ApplyConditionalVisibility(
            bool inventoryEmpty,
            bool hasTier1,
            GameObject gearUiRoot,
            GameObject tierButtonRoot,
            GameObject emptyInventoryRoot)
        {
            SetVisible(gearUiRoot, hasTier1);
            SetVisible(tierButtonRoot, hasTier1);
            SetVisible(emptyInventoryRoot, inventoryEmpty);
        }

        public static void ApplyChromeVisibility(Transform inventoryRoot, bool visible)
        {
            if (inventoryRoot == null)
            {
                return;
            }

            SetChildActive(inventoryRoot, "frameAndBorder", visible);
            SetChildActive(inventoryRoot, "frame", visible);
            SetChildActive(inventoryRoot, "amount", visible);

            for (int i = 0; i < inventoryRoot.childCount; i++)
            {
                Transform child = inventoryRoot.GetChild(i);
                if (child.name.StartsWith("customText"))
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }

        private static void ConfigureForIamReveal(InterfaceAnmElement element, bool allowIamToReveal)
        {
            if (allowIamToReveal)
            {
                element.currentState = CSFHIAnimableState.disappeared;
                element.gameObjectRef.SetActive(false);
                return;
            }

            element.currentState = CSFHIAnimableState.appeared;
            element.gameObjectRef.SetActive(false);
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        private static void SetChildActive(Transform parent, string childName, bool visible)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(visible);
            }
        }

        private static bool IsGearOrTier(GameObject target, GameObject gearUiRoot, GameObject tierButtonRoot)
        {
            if (referenceMatches(target, gearUiRoot) || referenceMatches(target, tierButtonRoot))
            {
                return true;
            }

            return target.name == "GearUI" || target.name == "TierButton" || target.name == "customButton";
        }

        private static bool IsEmptyHint(GameObject target, GameObject emptyInventoryRoot)
        {
            return referenceMatches(target, emptyInventoryRoot) || target.name == "emptyInventoryHint";
        }

        private static bool referenceMatches(GameObject target, GameObject reference)
        {
            return reference != null && target == reference;
        }
    }
}
