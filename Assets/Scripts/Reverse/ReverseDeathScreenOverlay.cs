using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Fatal death overlay: YOU DIE + tip, background fades to black, then reloads scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseDeathScreenOverlay : MonoBehaviour
    {
        private static ReverseDeathScreenOverlay instance;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;

        [SerializeField] private GameObject titleRoot;
        [SerializeField] private GameObject tipRoot;

        [Min(0.1f)]
        [SerializeField] private float fadeDuration = 5f;

        [SerializeField] private bool pauseTimeDuringFade = true;

        private Coroutine runningFade;

        public static bool TryPlayThenReload(float duration, Action reloadScene)
        {
            ReverseDeathScreenOverlay overlay = instance != null
                ? instance
                : FindObjectOfType<ReverseDeathScreenOverlay>(true);

            if (overlay == null)
            {
                return false;
            }

            return overlay.TryPlayFadeThen(duration, () => reloadScene?.Invoke());
        }

        private void Awake()
        {
            instance = this;
            EnsureInitialized();
            ApplyHiddenVisuals();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void HideImmediate()
        {
            if (runningFade != null)
            {
                StopCoroutine(runningFade);
                runningFade = null;
            }

            ApplyHiddenVisuals();
            gameObject.SetActive(false);
        }

        private void ApplyHiddenVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = 0f;
                backgroundImage.color = c;
            }

            SetLabelRootsActive(false);
        }

        private bool TryPlayFadeThen(float duration, Action onComplete)
        {
            EnsureInitialized();
            EnsureActiveInHierarchy(transform);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            EnsureHudScale();

            float useDuration = duration > 0f ? duration : fadeDuration;
            runningFade = StartCoroutineOnRunner(DeathFadeRoutine(useDuration, onComplete));
            return runningFade != null;
        }

        private Coroutine StartCoroutineOnRunner(IEnumerator routine)
        {
            MonoBehaviour runner = ResolveFadeRunner();
            return runner != null ? runner.StartCoroutine(routine) : null;
        }

        private static MonoBehaviour ResolveFadeRunner()
        {
            var existing = GameObject.Find("[ReverseUiFadeRunner]");
            if (existing != null)
            {
                var r = existing.GetComponent<ReverseUiFadeRunner>();
                if (r != null)
                {
                    return r;
                }
            }

            var runnerObject = new GameObject("[ReverseUiFadeRunner]");
            DontDestroyOnLoad(runnerObject);
            return runnerObject.AddComponent<ReverseUiFadeRunner>();
        }

        private IEnumerator DeathFadeRoutine(float duration, Action onComplete)
        {
            LayoutDeathLabels();
            SetLabelRootsActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            Color bgColor = backgroundImage != null ? backgroundImage.color : Color.black;
            bgColor.a = 0f;
            if (backgroundImage != null)
            {
                backgroundImage.color = bgColor;
            }

            float previousTimeScale = Time.timeScale;
            if (pauseTimeDuringFade)
            {
                Time.timeScale = 0f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += pauseTimeDuringFade ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (backgroundImage != null)
                {
                    bgColor.a = t;
                    backgroundImage.color = bgColor;
                }

                yield return null;
            }

            if (backgroundImage != null)
            {
                bgColor.a = 1f;
                backgroundImage.color = bgColor;
            }

            if (pauseTimeDuringFade)
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }

            onComplete?.Invoke();
        }

        private void LayoutDeathLabels()
        {
            CenterRect(titleRoot, 48f);
            CenterRect(tipRoot, -56f);
        }

        private void SetLabelRootsActive(bool visible)
        {
            if (titleRoot != null)
            {
                titleRoot.SetActive(visible);
            }

            if (tipRoot != null)
            {
                tipRoot.SetActive(visible);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child == titleRoot || child == tipRoot)
                {
                    continue;
                }

                child.SetActive(false);
            }
        }

        private static void CenterRect(GameObject root, float y)
        {
            if (root == null)
            {
                return;
            }

            var rect = root.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.localScale = Vector3.one;
        }

        private void EnsureInitialized()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (titleRoot == null && transform.childCount > 0)
            {
                titleRoot = transform.GetChild(0).gameObject;
            }
        }

        private void EnsureHudScale()
        {
            Transform hud = transform.parent;
            if (hud != null && hud.localScale.sqrMagnitude < 0.0001f)
            {
                hud.localScale = Vector3.one;
            }
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

        public static void ReloadActiveSceneImmediate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[ReverseDeathScreenOverlay] Cannot reload: active scene is invalid.");
                return;
            }

            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }

            if (!string.IsNullOrEmpty(scene.name))
            {
                SceneManager.LoadScene(scene.name);
            }
        }
    }
}
