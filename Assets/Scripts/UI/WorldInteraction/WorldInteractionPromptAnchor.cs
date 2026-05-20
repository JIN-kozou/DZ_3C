using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.UI.WorldInteraction
{
  public enum WorldInteractionMode
  {
    Tap = 0,
    Hold = 1,
  }

  public enum WorldInteractionMarkerMode
  {
    DefaultPrefab = 0,
    CustomPrefab = 1,
    Hidden = 2,
  }

  [DisallowMultipleComponent]
  public sealed class WorldInteractionPromptAnchor : MonoBehaviour
  {
    [Header("Prompt")]
    [SerializeField] private string promptText = "交互";
    [SerializeField] private string keyLabel = "E";
    [SerializeField] private WorldInteractionMode interactionMode = WorldInteractionMode.Tap;
    [SerializeField] private Transform anchorTransform;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Marker")]
    [SerializeField] private WorldInteractionMarkerMode markerMode = WorldInteractionMarkerMode.DefaultPrefab;
    [SerializeField] private GameObject customMarkerPrefab;
    [SerializeField] private Color markerTint = new Color(1f, 0.92f, 0.35f, 0.95f);

    [Header("Prefabs")]
    [SerializeField] private GameObject activePromptPrefab;
    [SerializeField] private GameObject markerPrefab;

    private Transform _pivot;
    private WorldInteractionPromptView _promptView;
    private WorldInteractionMarkerView _markerView;
    private IWorldInteractionHoldProgress _holdProgress;
    private bool _playerInRange;
    private bool _isAvailable = true;
    private PromptDisplayState _displayState = PromptDisplayState.Hidden;

    private enum PromptDisplayState
    {
      Hidden,
      Active,
      Marker,
    }

    public string PromptText => promptText;
    public string KeyLabel => keyLabel;
    public WorldInteractionMode InteractionMode => interactionMode;
    public bool IsPlayerInRange => _playerInRange;
    public bool IsAvailable => _isAvailable && _playerInRange;

    /// <summary>
    /// 距离内是否显示圆点标记（不要求玩家在触发器内；由 SetAvailable 表示交互物是否仍有效）。
    /// </summary>
    public bool IsMarkerCueEligible(Vector3 playerPosition, float markerMaxSqr)
    {
      if (!_isAvailable)
      {
        return false;
      }

      float sqrDist = (WorldPosition - playerPosition).sqrMagnitude;
      return sqrDist <= markerMaxSqr;
    }

    public bool IsWithinActiveDistance(Vector3 playerPosition, float activeMaxSqr)
    {
      float sqrDist = (WorldPosition - playerPosition).sqrMagnitude;
      return sqrDist <= activeMaxSqr;
    }

    public bool ShouldShowActivePrompt(
      Vector3 playerPosition,
      float activeMaxSqr,
      WorldInteractionPromptAnchor focusedAnchor)
    {
      return focusedAnchor == this
             && IsAvailable
             && IsWithinActiveDistance(playerPosition, activeMaxSqr);
    }

    public Vector3 WorldPosition
    {
      get
      {
        Transform t = anchorTransform != null ? anchorTransform : transform;
        return t.position + worldOffset;
      }
    }

    private void Awake()
    {
      _holdProgress = GetComponent<IWorldInteractionHoldProgress>();
      EnsurePivot();
    }

    private void OnEnable()
    {
      WorldInteractionPromptManager.Register(this);
      HidePrompts();
    }

    private void OnDisable()
    {
      WorldInteractionPromptManager.Unregister(this);
      HidePrompts();
    }

    public void Configure(string text, WorldInteractionMode mode, string key = "E")
    {
      promptText = text;
      interactionMode = mode;
      keyLabel = key;
    }

    public void ConfigureWithOffset(
      string text,
      WorldInteractionMode mode,
      Vector3 offset,
      string key = "E")
    {
      Configure(text, mode, key);
      worldOffset = offset;
    }

    public void SetPlayerInRange(bool inRange)
    {
      _playerInRange = inRange;
    }

    public void SetAvailable(bool available)
    {
      _isAvailable = available;
    }

    public float GetHoldProgress()
    {
      if (_holdProgress == null)
      {
        return 0f;
      }

      return Mathf.Clamp01(_holdProgress.NormalizedProgress);
    }

    public void ShowAsActive()
    {
      ApplyDisplay(PromptDisplayState.Active);
    }

    public void ShowAsMarker()
    {
      if (markerMode == WorldInteractionMarkerMode.Hidden)
      {
        ApplyDisplay(PromptDisplayState.Hidden);
        return;
      }

      ApplyDisplay(PromptDisplayState.Marker);
    }

    public void HidePrompts()
    {
      ApplyDisplay(PromptDisplayState.Hidden);
    }

    private void ApplyDisplay(PromptDisplayState state)
    {
      _displayState = state;
      EnsurePivot();

      switch (state)
      {
        case PromptDisplayState.Hidden:
          SetPromptVisible(false);
          SetMarkerVisible(false);
          break;

        case PromptDisplayState.Active:
          SetMarkerVisible(false);
          EnsurePromptView();
          SetPromptVisible(true);
          if (_promptView != null)
          {
            _promptView.SetKeyLabel(keyLabel);
            _promptView.SetActionText(promptText);
            bool showBar = interactionMode == WorldInteractionMode.Hold;
            float progress = showBar ? GetHoldProgress() : 0f;
            _promptView.SetHoldProgress(progress, showBar);
          }

          break;

        case PromptDisplayState.Marker:
          SetPromptVisible(false);
          EnsureMarkerView();
          SetMarkerVisible(true);
          break;
      }
    }

    private void SetPromptVisible(bool visible)
    {
      if (visible)
      {
        EnsurePromptView();
      }

      Transform promptRoot = GetPromptRootTransform();
      if (promptRoot != null)
      {
        if (visible)
        {
          EnsurePivotActive();
          promptRoot.gameObject.SetActive(true);
        }
        else
        {
          promptRoot.gameObject.SetActive(false);
        }
      }

      if (_promptView == null)
      {
        return;
      }

      _promptView.SetActiveVisual(visible);
    }

    private Transform GetPromptRootTransform()
    {
      if (_pivot == null)
      {
        return null;
      }

      return _pivot.Find("WorldInteractionPrompt");
    }

    private void EnsurePivotActive()
    {
      if (_pivot != null)
      {
        _pivot.gameObject.SetActive(true);
      }
    }

    private void SetMarkerVisible(bool visible)
    {
      if (visible)
      {
        EnsureMarkerView();
      }

      Transform markerRoot = GetMarkerRootTransform();
      if (markerRoot != null)
      {
        if (visible)
        {
          EnsurePivotActive();
          markerRoot.gameObject.SetActive(true);
        }
        else
        {
          markerRoot.gameObject.SetActive(false);
        }
      }

      if (_markerView == null)
      {
        return;
      }

      _markerView.SetVisible(visible, _pivot);
      if (visible)
      {
        _markerView.SetTint(markerTint);
      }
    }

    private Transform GetMarkerRootTransform()
    {
      if (_pivot == null)
      {
        return null;
      }

      return _pivot.Find("WorldInteractionMarker");
    }

    private void LateUpdate()
    {
      if (_pivot != null)
      {
        _pivot.position = WorldPosition;
      }

      if (_displayState == PromptDisplayState.Active && interactionMode == WorldInteractionMode.Hold && _promptView != null)
      {
        float progress = GetHoldProgress();
        _promptView.SetHoldProgress(progress, true);
      }
    }

    private void EnsurePivot()
    {
      if (_pivot != null)
      {
        return;
      }

      Transform existing = transform.Find("InteractionPromptPivot");
      if (existing != null)
      {
        _pivot = existing;
        _pivot.position = WorldPosition;
        return;
      }

      var pivotGo = new GameObject("InteractionPromptPivot");
      pivotGo.transform.SetParent(transform, false);
      _pivot = pivotGo.transform;
      _pivot.position = WorldPosition;
    }

    private void EnsurePromptView()
    {
      EnsurePivot();

      Transform existingRoot = GetPromptRootTransform();
      if (existingRoot != null)
      {
        _promptView = existingRoot.GetComponent<WorldInteractionPromptView>();
        if (_promptView != null)
        {
          ReparentViewToPivot(_promptView.transform);
          return;
        }
      }

      _promptView = TryInstantiatePrompt();
      if (_promptView == null)
      {
        _promptView = WorldInteractionUIBuilder.CreatePrompt(_pivot);
      }

      ReparentViewToPivot(_promptView != null ? _promptView.transform : null);
    }

    private void EnsureMarkerView()
    {
      EnsurePivot();

      Transform existingRoot = GetMarkerRootTransform();
      if (existingRoot != null)
      {
        _markerView = existingRoot.GetComponent<WorldInteractionMarkerView>();
        if (_markerView != null)
        {
          ReparentViewToPivot(_markerView.transform);
          return;
        }
      }

      _markerView = TryInstantiateMarker();
      if (_markerView == null)
      {
        _markerView = WorldInteractionUIBuilder.CreateMarker(_pivot);
      }

      ReparentViewToPivot(_markerView != null ? _markerView.transform : null);
    }

    private void ReparentViewToPivot(Transform viewTransform)
    {
      if (viewTransform == null || _pivot == null)
      {
        return;
      }

      if (viewTransform.parent != _pivot)
      {
        viewTransform.SetParent(_pivot, false);
      }
    }

    private WorldInteractionPromptView TryInstantiatePrompt()
    {
      if (activePromptPrefab == null)
      {
        activePromptPrefab = WorldInteractionPrefabCache.LoadPromptPrefab();
      }

      if (activePromptPrefab == null)
      {
        return null;
      }

      var instance = Instantiate(activePromptPrefab, _pivot);
      instance.name = "WorldInteractionPrompt";
      instance.SetActive(false);
      return instance.GetComponent<WorldInteractionPromptView>()
             ?? instance.GetComponentInChildren<WorldInteractionPromptView>(true);
    }

    private WorldInteractionMarkerView TryInstantiateMarker()
    {
      GameObject prefab = markerMode == WorldInteractionMarkerMode.CustomPrefab && customMarkerPrefab != null
        ? customMarkerPrefab
        : markerPrefab != null ? markerPrefab : WorldInteractionPrefabCache.LoadMarkerPrefab();

      if (prefab == null)
      {
        return null;
      }

      var instance = Instantiate(prefab, _pivot);
      instance.name = "WorldInteractionMarker";
      WorldInteractionMarkerView view = instance.GetComponent<WorldInteractionMarkerView>();
      if (view == null)
      {
        Object.Destroy(instance);
        return null;
      }

      view.Bind(instance, instance.GetComponentInChildren<Image>(true));
      instance.SetActive(false);
      return view;
    }
  }
}
