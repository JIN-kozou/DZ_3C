using System;
using System.Collections;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Shared full-screen UI fade (0→1) for death / load checkpoint panels.
    /// </summary>
    public static class ReverseUiFadeUtil
    {
        private static MonoBehaviour fadeCoroutineHost;

        public static bool TryPlay(
            MonoBehaviour host,
            CanvasGroup canvasGroup,
            ref Coroutine runningFade,
            float duration,
            float defaultDuration,
            bool pauseTimeDuringFade,
            Action onComplete)
        {
            if (host == null || canvasGroup == null)
            {
                return false;
            }

            StopRunningFade(host, ref runningFade);

            host.gameObject.SetActive(true);
            EnsureActiveInHierarchy(host.transform);

            float useDuration = duration > 0f ? duration : defaultDuration;
            MonoBehaviour runner = ResolveCoroutineRunner(host);
            runningFade = runner.StartCoroutine(
                FadeRoutine(host.transform, canvasGroup, useDuration, pauseTimeDuringFade, onComplete));
            return true;
        }

        public static void HideImmediate(MonoBehaviour host, CanvasGroup canvasGroup, ref Coroutine runningFade)
        {
            if (host == null || canvasGroup == null)
            {
                return;
            }

            StopRunningFade(host, ref runningFade);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            host.gameObject.SetActive(false);
        }

        public static CanvasGroup EnsureCanvasGroup(MonoBehaviour host)
        {
            CanvasGroup group = host.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = host.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static void StopRunningFade(MonoBehaviour host, ref Coroutine runningFade)
        {
            if (runningFade == null)
            {
                return;
            }

            if (fadeCoroutineHost != null)
            {
                fadeCoroutineHost.StopCoroutine(runningFade);
            }
            else if (host != null && host.isActiveAndEnabled)
            {
                host.StopCoroutine(runningFade);
            }

            runningFade = null;
        }

        /// <summary>
        /// UI 淡入协程始终跑在常驻 Runner 上，避免玩家死亡后 Character 被禁用导致 StartCoroutine 失败。
        /// </summary>
        private static MonoBehaviour ResolveCoroutineRunner(MonoBehaviour host)
        {
            EnsurePersistentRunner();
            return fadeCoroutineHost;
        }

        private static void EnsurePersistentRunner()
        {
            if (fadeCoroutineHost != null)
            {
                return;
            }

            var existing = GameObject.Find("[ReverseUiFadeRunner]");
            if (existing != null)
            {
                fadeCoroutineHost = existing.GetComponent<ReverseUiFadeRunner>();
                if (fadeCoroutineHost != null)
                {
                    return;
                }
            }

            var runnerObject = new GameObject("[ReverseUiFadeRunner]");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            fadeCoroutineHost = runnerObject.AddComponent<ReverseUiFadeRunner>();
        }

        private static void EnsureActiveInHierarchy(Transform panelTransform)
        {
            Transform current = panelTransform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                current = current.parent;
            }
        }

        private static IEnumerator FadeRoutine(
            Transform panelTransform,
            CanvasGroup canvasGroup,
            float duration,
            bool pauseTimeDuringFade,
            Action onComplete)
        {
            if (panelTransform.parent != null)
            {
                panelTransform.SetAsLastSibling();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            float previousTimeScale = Time.timeScale;
            if (pauseTimeDuringFade)
            {
                Time.timeScale = 0f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += pauseTimeDuringFade ? Time.unscaledDeltaTime : Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;

            if (pauseTimeDuringFade)
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }

            onComplete?.Invoke();
        }
    }
}
