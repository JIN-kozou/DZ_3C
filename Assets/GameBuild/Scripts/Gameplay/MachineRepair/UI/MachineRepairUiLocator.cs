using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Resolves machine-repair HUD widgets. UITest keeps Inventory/TAB under HUD/Canvas, not the first Canvas in the scene.
    /// </summary>
    public static class MachineRepairUiLocator
    {
        public static bool TryResolveHudWidgets(
            out Transform inventoryRoot,
            out Transform tabRoot,
            out RectTransform bannerStackParent)
        {
            inventoryRoot = null;
            tabRoot = null;
            bannerStackParent = null;

            if (TryUnderHudCanvas(out inventoryRoot, out tabRoot, out bannerStackParent))
            {
                return inventoryRoot != null && tabRoot != null;
            }

            inventoryRoot = FindTransformByName("Inventory", includeInactive: true);
            tabRoot = FindTransformByName("TAB", includeInactive: true);
            if (inventoryRoot != null)
            {
                Transform canvasParent = inventoryRoot.parent;
                bannerStackParent = canvasParent as RectTransform;
            }

            return inventoryRoot != null && tabRoot != null;
        }

        public static RectTransform GetBannerStackParent()
        {
            if (TryResolveHudWidgets(out _, out _, out RectTransform parent) && parent != null)
            {
                return parent;
            }

            Canvas any = Object.FindObjectOfType<Canvas>();
            return any != null ? any.transform as RectTransform : null;
        }

        private static bool TryUnderHudCanvas(
            out Transform inventoryRoot,
            out Transform tabRoot,
            out RectTransform bannerStackParent)
        {
            inventoryRoot = null;
            tabRoot = null;
            bannerStackParent = null;

            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                return false;
            }

            Transform hudCanvas = hud.transform.Find("Canvas");
            if (hudCanvas == null)
            {
                return false;
            }

            bannerStackParent = hudCanvas as RectTransform;
            inventoryRoot = hudCanvas.Find("Inventory");
            tabRoot = hudCanvas.Find("TAB");
            return true;
        }

        private static Transform FindTransformByName(string objectName, bool includeInactive)
        {
            if (!includeInactive)
            {
                GameObject go = GameObject.Find(objectName);
                return go != null ? go.transform : null;
            }

            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t.name != objectName)
                {
                    continue;
                }

                if (!t.gameObject.scene.IsValid())
                {
                    continue;
                }

                return t;
            }

            return null;
        }
    }
}
