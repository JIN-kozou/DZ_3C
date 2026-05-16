using UnityEngine;

namespace DZ_3C.MachineRepair.UI
{
    /// <summary>
    /// Copies RectTransform layout from scene examples so runtime UI matches authored positions.
    /// </summary>
    public static class MachineRepairRectLayoutMirror
    {
        public static void ApplyFullCanvasStretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        public static void CopyFromExample(RectTransform example, RectTransform target)
        {
            if (example == null || target == null)
            {
                return;
            }

            target.anchorMin = example.anchorMin;
            target.anchorMax = example.anchorMax;
            target.pivot = example.pivot;
            target.sizeDelta = example.sizeDelta;
            target.anchoredPosition = example.anchoredPosition;
            target.localRotation = example.localRotation;
            target.localScale = example.localScale;
            target.localPosition = example.localPosition;
        }

        public static void CopyFromExampleWithStackIndex(
            RectTransform example,
            RectTransform target,
            int stackIndex,
            Vector2 stackStep)
        {
            CopyFromExample(example, target);
            if (stackIndex > 0)
            {
                target.anchoredPosition = example.anchoredPosition - stackStep * stackIndex;
            }
        }

        public static Vector2 ResolveVerticalStackStep(RectTransform topExample, RectTransform lowerExample)
        {
            if (topExample == null || lowerExample == null)
            {
                return new Vector2(0f, 65.57764f);
            }

            Vector2 delta = topExample.anchoredPosition - lowerExample.anchoredPosition;
            if (Mathf.Abs(delta.y) < 1f)
            {
                return new Vector2(0f, 65.57764f);
            }

            return new Vector2(0f, Mathf.Abs(delta.y));
        }
    }
}
