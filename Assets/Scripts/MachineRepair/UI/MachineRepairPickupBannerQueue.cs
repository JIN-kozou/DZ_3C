using System;
using System.Collections;
using System.Collections.Generic;
using DZ_3C.MachineRepair;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.MachineRepair.UI
{
    public class MachineRepairPickupBannerQueue : MonoBehaviour
    {
        private static readonly string[] StackObjectNames = { "StackRoot", "stackRoot", "PickupBannerStack", "BannerStack" };

        private const string SuccessPrefix = "成功拾取：";
        private const string FailPrefix = "拾取失败：";
        private const string FailInventoryFullDetail = "库存已满";
        private const string SuccessPrefabPath = "Assets/Prefab/UI/getGearBanner.prefab";
        private const string FailPrefabPath = "Assets/Prefab/UI/failgetGearBanner.prefab";
        private const string ExamplesFolderName = "BannerExamples";

        [SerializeField] private RectTransform layoutReference;
        [SerializeField] private RectTransform failLayoutReference;
        [SerializeField] private RectTransform stackRoot;
        [SerializeField] private GameObject successBannerPrefab;
        [SerializeField] private GameObject failBannerPrefab;
        [SerializeField] private Vector2 stackAnchorPosition = new Vector2(-851.5f, 369.57764f);
        [SerializeField] private float stackSpacing = 65.57764f;
        [SerializeField, Min(0f)] private float extraStackPadding = 12f;

        [Header("布局调节（叠在 BannerExamples 示例之上）")]
        [Tooltip("若指定，则使用资源中的缩放/偏移；否则使用下方组件字段。")]
        [SerializeField] private MachineRepairPickupBannerLayoutSettings layoutSettings;

        [SerializeField] private Vector3 bannerScaleMultiplier = Vector3.one;
        [SerializeField] private Vector2 bannerPositionOffset;
        [SerializeField, Min(0.1f)] private float bannerStackSpacingMultiplier = 1f;

        [SerializeField, Min(0.5f)] private float holdSecondsAfterAppear = 2.2f;
        [SerializeField, Min(1)] private int maxConcurrent = 5;

        private readonly List<ActiveBanner> activeBanners = new();
        private Vector2 stackStep = new Vector2(0f, 65.57764f);

        private sealed class ActiveBanner
        {
            public RectTransform Rect;
            public InterfaceAnimManager Anim;
            public Coroutine Routine;
            public bool IsFail;
            public Vector3 BaseLocalScale;
            public Vector2 BaseAnchoredPosition;
        }

        public static MachineRepairPickupBannerQueue FindInScene()
        {
            MachineRepairPickupBannerQueue[] queues = FindObjectsOfType<MachineRepairPickupBannerQueue>(true);
            for (int i = 0; i < queues.Length; i++)
            {
                if (queues[i] != null && queues[i].isActiveAndEnabled)
                {
                    return queues[i];
                }
            }

            return queues.Length > 0 ? queues[0] : null;
        }

        public static MachineRepairPickupBannerQueue CreateDefaultUnderCanvas()
        {
            RectTransform parent = MachineRepairUiLocator.GetBannerStackParent();
            if (parent == null)
            {
                return null;
            }

            Transform existing = FindBannerStackTransform(parent);
            if (existing != null && existing.TryGetComponent(out MachineRepairPickupBannerQueue queue))
            {
                queue.EnsureStackRootLayout();
                queue.RefreshLayoutFromExamples();
                return queue;
            }

            GameObject stackGo = new GameObject("StackRoot", typeof(RectTransform));
            stackGo.transform.SetParent(parent, false);
            MachineRepairPickupBannerQueue created = stackGo.AddComponent<MachineRepairPickupBannerQueue>();
            created.stackRoot = stackGo.GetComponent<RectTransform>();
            created.EnsureStackRootLayout();
            created.RefreshLayoutFromExamples();
            return created;
        }

        public static Transform FindBannerStackTransform(Transform hudCanvas)
        {
            if (hudCanvas == null)
            {
                return null;
            }

            for (int i = 0; i < StackObjectNames.Length; i++)
            {
                Transform child = hudCanvas.Find(StackObjectNames[i]);
                if (child != null)
                {
                    return child;
                }
            }

            for (int i = 0; i < hudCanvas.childCount; i++)
            {
                Transform child = hudCanvas.GetChild(i);
                for (int n = 0; n < StackObjectNames.Length; n++)
                {
                    if (string.Equals(child.name, StackObjectNames[n], StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void Awake()
        {
            if (stackRoot == null)
            {
                stackRoot = transform as RectTransform;
            }

            EnsureStackRootLayout();
            RefreshLayoutFromExamples();
            EnsurePrefabsLoaded();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || activeBanners.Count == 0)
            {
                return;
            }

            RefreshLayoutFromExamples();
            ReflowStack();
        }

        private void LateUpdate()
        {
            if (activeBanners.Count == 0)
            {
                return;
            }

            for (int i = 0; i < activeBanners.Count; i++)
            {
                ApplyUserLayoutTuning(activeBanners[i]);
            }
        }

        [ContextMenu("Refresh Active Banner Layout")]
        public void RefreshActiveBannerLayout()
        {
            RefreshLayoutFromExamples();
            ReflowStack();
        }

        public void EnsureStackRootLayout()
        {
            if (stackRoot == null)
            {
                stackRoot = transform as RectTransform;
            }

            MachineRepairRectLayoutMirror.ApplyFullCanvasStretch(stackRoot);
        }

        public void RefreshLayoutFromExamples()
        {
            ResolveLayoutExamples();

            if (layoutReference != null)
            {
                stackAnchorPosition = layoutReference.anchoredPosition;
            }

            stackStep = MachineRepairRectLayoutMirror.ResolveVerticalStackStep(layoutReference, failLayoutReference);
            stackSpacing = stackStep.y;
        }

        private void ResolveLayoutExamples()
        {
            RectTransform canvasRect = MachineRepairUiLocator.GetBannerStackParent();
            if (canvasRect != null)
            {
                if (TryFindExamplesOnCanvas(canvasRect, out RectTransform success, out RectTransform fail))
                {
                    if (layoutReference == null)
                    {
                        layoutReference = success;
                    }

                    if (failLayoutReference == null)
                    {
                        failLayoutReference = fail;
                    }
                }
            }

            if (layoutReference == null)
            {
                GameObject example = GameObject.Find("getGearBanner");
                if (example != null)
                {
                    layoutReference = example.GetComponent<RectTransform>();
                }
            }

            if (failLayoutReference == null)
            {
                GameObject failExample = GameObject.Find("getGearBanner_fail_example");
                if (failExample != null)
                {
                    failLayoutReference = failExample.GetComponent<RectTransform>();
                }
            }
        }

        private static bool TryFindExamplesOnCanvas(
            Transform hudCanvas,
            out RectTransform success,
            out RectTransform fail)
        {
            success = null;
            fail = null;

            Transform examples = hudCanvas.Find(ExamplesFolderName);
            if (examples != null)
            {
                success = examples.Find("getGearBanner") as RectTransform;
                fail = examples.Find("getGearBanner_fail_example") as RectTransform;
                if (fail == null)
                {
                    fail = examples.Find("getGearBanner (1)") as RectTransform;
                }
            }

            if (success == null)
            {
                Transform direct = hudCanvas.Find("getGearBanner");
                if (direct != null)
                {
                    success = direct as RectTransform;
                }
            }

            if (fail == null)
            {
                Transform directFail = hudCanvas.Find("getGearBanner_fail_example");
                if (directFail != null)
                {
                    fail = directFail as RectTransform;
                }
            }

            return success != null || fail != null;
        }

        private void EnsurePrefabsLoaded()
        {
            if (successBannerPrefab == null)
            {
                successBannerPrefab = LoadPrefabAtPath(SuccessPrefabPath);
            }

            if (failBannerPrefab == null)
            {
                failBannerPrefab = LoadPrefabAtPath(FailPrefabPath);
            }
        }

        private static GameObject LoadPrefabAtPath(string assetPath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            return null;
#endif
        }

        public void ShowPickupSuccess(MachinePartDefinition definition)
        {
            EnsurePrefabsLoaded();
            if (successBannerPrefab == null)
            {
                return;
            }

            string detail = definition != null ? $"{definition.DisplayName} x 1" : "零件 x 1";
            Enqueue(successBannerPrefab, SuccessPrefix, detail, false);
        }

        public void ShowPickupFailedInventoryFull()
        {
            EnsurePrefabsLoaded();
            if (failBannerPrefab == null)
            {
                return;
            }

            Enqueue(failBannerPrefab, FailPrefix, FailInventoryFullDetail, true);
        }

        private void Enqueue(GameObject prefab, string prefix, string detail, bool isFail)
        {
            if (stackRoot == null)
            {
                return;
            }

            EnsureStackRootLayout();
            RefreshLayoutFromExamples();

            while (activeBanners.Count >= maxConcurrent)
            {
                ForceFinishOldest();
            }

            GameObject instance = Instantiate(prefab, stackRoot);
            instance.SetActive(true);

            GearPickupBannerView view = instance.GetComponent<GearPickupBannerView>();
            if (view == null)
            {
                view = instance.AddComponent<GearPickupBannerView>();
            }

            view.SetContent(prefix, detail);

            Button button = instance.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }

            CanvasGroup rootGroup = instance.GetComponent<CanvasGroup>();
            if (rootGroup != null)
            {
                rootGroup.alpha = 1f;
            }

            DisableBannerAnimator(instance);

            InterfaceAnimManager iam = ResolveAnimManager(instance);

            var entry = new ActiveBanner
            {
                Rect = instance.GetComponent<RectTransform>(),
                Anim = iam,
                IsFail = isFail,
            };
            activeBanners.Add(entry);
            ApplyBannerLayout(entry, activeBanners.Count - 1);

            entry.Routine = StartCoroutine(RunBannerLifecycle(entry, instance, iam));
        }

        private static void DisableBannerAnimator(GameObject instance)
        {
            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].enabled = false;
            }
        }

        private void ApplyBannerLayout(ActiveBanner entry, int stackIndex)
        {
            RectTransform rect = entry.Rect;
            if (rect == null)
            {
                return;
            }

            RefreshLayoutFromExamples();

            RectTransform stackBase = layoutReference != null ? layoutReference : failLayoutReference;
            if (stackBase == null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = stackAnchorPosition - GetStackStepVector() * stackIndex;
                CacheBannerBaseLayout(entry);
                ApplyUserLayoutTuning(entry);
                return;
            }

            MachineRepairRectLayoutMirror.CopyFromExampleWithStackIndex(
                stackBase,
                rect,
                stackIndex,
                GetStackStepVector());

            CacheBannerBaseLayout(entry);
            ApplyUserLayoutTuning(entry);
        }

        private static void CacheBannerBaseLayout(ActiveBanner entry)
        {
            if (entry.Rect == null)
            {
                return;
            }

            entry.BaseLocalScale = entry.Rect.localScale;
            entry.BaseAnchoredPosition = entry.Rect.anchoredPosition;
        }

        private Vector2 GetStackStepVector()
        {
            float stepY = stackSpacing > 0f ? stackSpacing : stackStep.y;
            stepY *= GetStackSpacingMultiplier();

            RectTransform sizeReference = layoutReference != null ? layoutReference : failLayoutReference;
            if (sizeReference != null)
            {
                Vector3 scaleMultiplier = GetScaleMultiplier();
                float scaledHeight = sizeReference.sizeDelta.y
                    * Mathf.Abs(sizeReference.localScale.y)
                    * scaleMultiplier.y;
                stepY = Mathf.Max(stepY, scaledHeight + extraStackPadding);
            }

            return new Vector2(0f, stepY);
        }

        private Vector3 GetScaleMultiplier()
        {
            return layoutSettings != null ? layoutSettings.scaleMultiplier : bannerScaleMultiplier;
        }

        private Vector2 GetPositionOffset()
        {
            return layoutSettings != null ? layoutSettings.positionOffset : bannerPositionOffset;
        }

        private float GetStackSpacingMultiplier()
        {
            return layoutSettings != null ? layoutSettings.stackSpacingMultiplier : bannerStackSpacingMultiplier;
        }

        private void ApplyUserLayoutTuning(ActiveBanner entry)
        {
            if (entry?.Rect == null)
            {
                return;
            }

            Vector3 multiplier = GetScaleMultiplier();
            Vector3 baseScale = entry.BaseLocalScale;
            if (baseScale == Vector3.zero)
            {
                baseScale = entry.Rect.localScale;
            }

            entry.Rect.localScale = new Vector3(
                baseScale.x * multiplier.x,
                baseScale.y * multiplier.y,
                baseScale.z * multiplier.z);

            entry.Rect.anchoredPosition = entry.BaseAnchoredPosition + GetPositionOffset();
        }

        private static InterfaceAnimManager ResolveAnimManager(GameObject instance)
        {
            InterfaceAnimManager iam = instance.GetComponent<InterfaceAnimManager>();
            if (iam != null)
            {
                iam.autoStart = false;
                return iam;
            }

            iam = instance.GetComponentInChildren<InterfaceAnimManager>(true);
            if (iam != null)
            {
                iam.autoStart = false;
            }

            return iam;
        }

        private IEnumerator RunBannerLifecycle(ActiveBanner entry, GameObject instance, InterfaceAnimManager iam)
        {
            if (iam != null)
            {
                iam.startAppear();
                yield return WaitUntilState(iam, CSFHIAnimableState.appeared, 4f);
                ApplyUserLayoutTuning(entry);
                yield return new WaitForSeconds(holdSecondsAfterAppear);
                iam.startDisappear();
                yield return WaitUntilState(iam, CSFHIAnimableState.disappeared, 4f);
            }
            else
            {
                ApplyUserLayoutTuning(entry);
                yield return new WaitForSeconds(holdSecondsAfterAppear);
            }

            RemoveEntry(entry);
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private static IEnumerator WaitUntilState(InterfaceAnimManager iam, CSFHIAnimableState target, float timeout)
        {
            float elapsed = 0f;
            while (iam != null && iam.currentState != target && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ForceFinishOldest()
        {
            if (activeBanners.Count == 0)
            {
                return;
            }

            ActiveBanner oldest = activeBanners[0];
            if (oldest.Routine != null)
            {
                StopCoroutine(oldest.Routine);
            }

            if (oldest.Anim != null)
            {
                oldest.Anim.startDisappear(true);
            }

            if (oldest.Rect != null)
            {
                Destroy(oldest.Rect.gameObject);
            }

            activeBanners.RemoveAt(0);
            ReflowStack();
        }

        private void RemoveEntry(ActiveBanner entry)
        {
            if (entry.Routine != null)
            {
                StopCoroutine(entry.Routine);
            }

            activeBanners.Remove(entry);
            ReflowStack();
        }

        private void ReflowStack()
        {
            for (int i = 0; i < activeBanners.Count; i++)
            {
                ApplyBannerLayout(activeBanners[i], i);
            }
        }
    }
}
