using System.Collections.Generic;
using DZ_3C.MachineRepair;
using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.UI.WorldInteraction
{
  [DisallowMultipleComponent]
  public sealed class WorldInteractionPromptManager : MonoBehaviour
  {
    private static WorldInteractionPromptManager _instance;

    [SerializeField] private WorldInteractionConfig config;

    private readonly List<WorldInteractionPromptAnchor> _anchors = new List<WorldInteractionPromptAnchor>(32);
    private readonly List<WorldInteractionPromptAnchor> _markerCandidates = new List<WorldInteractionPromptAnchor>(16);
    private readonly List<WorldInteractionPromptAnchor> _focusCandidates = new List<WorldInteractionPromptAnchor>(8);
    private Transform _playerTransform;
    private Camera _aimCamera;
    private WorldInteractionPromptAnchor _currentAimedAnchor;

    public static WorldInteractionPromptManager Instance => _instance;

    public WorldInteractionPromptAnchor CurrentAimedAnchor => _currentAimedAnchor;

    public float ActivePromptMaxDistance => GetConfig().activePromptMaxDistance;
    public float MarkerMaxDistance => GetConfig().markerMaxDistance;

    public static WorldInteractionPromptManager EnsureOnPlayer(Player player)
    {
      if (player == null)
      {
        return _instance;
      }

      if (_instance != null)
      {
        _instance._playerTransform = player.transform;
        _instance.RefreshAnchorRegistry();
        return _instance;
      }

      var manager = player.GetComponent<WorldInteractionPromptManager>();
      if (manager == null)
      {
        manager = player.gameObject.AddComponent<WorldInteractionPromptManager>();
      }

      manager._playerTransform = player.transform;
      manager.RefreshAnchorRegistry();
      return manager;
    }

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(this);
        return;
      }

      _instance = this;
      if (config == null)
      {
        config = Resources.Load<WorldInteractionConfig>("Config/UI/WorldInteractionConfig");
      }

      if (_playerTransform == null)
      {
        var player = GetComponent<Player>();
        if (player != null)
        {
          _playerTransform = player.transform;
        }
      }

      RefreshAnchorRegistry();
    }

    private void OnDestroy()
    {
      if (_instance == this)
      {
        _instance = null;
      }

      WorldInteractionMovementLock.SetLocked(false);
    }

    private void LateUpdate()
    {
      if (_playerTransform == null)
      {
        var player = GetComponent<Player>();
        if (player != null)
        {
          _playerTransform = player.transform;
        }
      }

      RefreshDisplay();
      RefreshMovementLock();
    }

    public static void Register(WorldInteractionPromptAnchor anchor)
    {
      if (anchor == null)
      {
        return;
      }

      Player player = Object.FindObjectOfType<Player>();
      if (player != null)
      {
        EnsureOnPlayer(player);
      }

      if (_instance == null)
      {
        return;
      }

      if (!_instance._anchors.Contains(anchor))
      {
        _instance._anchors.Add(anchor);
      }
    }

    public static void Unregister(WorldInteractionPromptAnchor anchor)
    {
      if (anchor == null || _instance == null)
      {
        return;
      }

      _instance._anchors.Remove(anchor);
      anchor.HidePrompts();
    }

    public bool TryGetFocusedInteractableAnchor(out WorldInteractionPromptAnchor anchor)
    {
      anchor = _currentAimedAnchor;
      return anchor != null && anchor.IsAvailable;
    }

    private void RefreshAnchorRegistry()
    {
      WorldInteractionPromptAnchor[] found =
        Object.FindObjectsOfType<WorldInteractionPromptAnchor>(true);
      for (int i = 0; i < found.Length; i++)
      {
        WorldInteractionPromptAnchor anchor = found[i];
        if (anchor != null && !_anchors.Contains(anchor))
        {
          _anchors.Add(anchor);
        }
      }
    }

    private WorldInteractionConfig GetConfig()
    {
      if (config != null)
      {
        return config;
      }

      config = Resources.Load<WorldInteractionConfig>("Config/UI/WorldInteractionConfig");
      return config != null ? config : CreateRuntimeDefaults();
    }

    private static WorldInteractionConfig CreateRuntimeDefaults()
    {
      var runtime = ScriptableObject.CreateInstance<WorldInteractionConfig>();
      runtime.activePromptMaxDistance = 3f;
      runtime.markerMaxDistance = 12f;
      runtime.aimRayMaxDistance = 40f;
      runtime.aimLayerMask = ~0;
      runtime.allowAngleFocusFallback = false;
      runtime.focusMaxViewAngle = 12f;
      return runtime;
    }

    private void RefreshDisplay()
    {
      _markerCandidates.Clear();
      _focusCandidates.Clear();
      _currentAimedAnchor = null;

      if (_playerTransform == null)
      {
        HideAll();
        return;
      }

      WorldInteractionConfig settings = GetConfig();
      Vector3 playerPos = _playerTransform.position;
      float markerMaxSqr = settings.markerMaxDistance * settings.markerMaxDistance;
      float activeMaxSqr = settings.activePromptMaxDistance * settings.activePromptMaxDistance;

      for (int i = 0; i < _anchors.Count; i++)
      {
        WorldInteractionPromptAnchor anchor = _anchors[i];
        if (anchor == null)
        {
          continue;
        }

        if (!anchor.IsMarkerCueEligible(playerPos, markerMaxSqr))
        {
          anchor.HidePrompts();
          continue;
        }

        _markerCandidates.Add(anchor);

        if (anchor.IsAvailable)
        {
          float sqrDist = (anchor.WorldPosition - playerPos).sqrMagnitude;
          if (sqrDist <= activeMaxSqr)
          {
            _focusCandidates.Add(anchor);
          }
        }
      }

      if (_markerCandidates.Count == 0)
      {
        return;
      }

      Camera cam = ResolveAimCamera();
      WorldInteractionPromptAnchor aimed = null;
      if (cam != null && _focusCandidates.Count > 0)
      {
        WorldInteractionAimUtility.TryResolveFocusedAnchor(
          cam,
          _playerTransform,
          settings.aimRayMaxDistance,
          settings.aimLayerMask,
          settings.focusMaxViewAngle,
          _focusCandidates,
          activeMaxSqr,
          settings.allowAngleFocusFallback,
          out aimed);
      }

      _currentAimedAnchor = aimed;

      for (int i = 0; i < _markerCandidates.Count; i++)
      {
        WorldInteractionPromptAnchor anchor = _markerCandidates[i];
        if (anchor.ShouldShowActivePrompt(playerPos, activeMaxSqr, aimed))
        {
          anchor.ShowAsActive();
        }
        else
        {
          anchor.ShowAsMarker();
        }
      }
    }

    private void RefreshMovementLock()
    {
      bool block = false;
      for (int i = 0; i < _anchors.Count; i++)
      {
        WorldInteractionPromptAnchor anchor = _anchors[i];
        if (anchor == null)
        {
          continue;
        }

        IWorldInteractionHoldProgress hold = anchor.GetComponent<IWorldInteractionHoldProgress>();
        if (hold != null && hold.BlocksMovement)
        {
          block = true;
          break;
        }
      }

      if (block != WorldInteractionMovementLock.IsLocked)
      {
        WorldInteractionMovementLock.SetLocked(block);
        if (block && InputService.Instance != null)
        {
          InputService.Instance.SnapMoveSmoothToCurrentDiscrete();
        }
      }
    }

    private Camera ResolveAimCamera()
    {
      if (_aimCamera != null && _aimCamera.isActiveAndEnabled)
      {
        return _aimCamera;
      }

      _aimCamera = Camera.main;
      if (_aimCamera != null)
      {
        return _aimCamera;
      }

      Player player = GetComponent<Player>();
      if (player != null && player.camTransform != null)
      {
        _aimCamera = player.camTransform.GetComponent<Camera>();
        if (_aimCamera != null)
        {
          return _aimCamera;
        }

        _aimCamera = player.camTransform.GetComponentInChildren<Camera>(true);
      }

      return _aimCamera;
    }

    private void HideAll()
    {
      _currentAimedAnchor = null;
      for (int i = 0; i < _anchors.Count; i++)
      {
        _anchors[i]?.HidePrompts();
      }
    }

    public bool TryPerformFocusedTapInteraction(RepairInteractionHub hub)
    {
      if (hub == null || !TryGetFocusedInteractableAnchor(out WorldInteractionPromptAnchor anchor))
      {
        return false;
      }

      if (anchor.InteractionMode != WorldInteractionMode.Tap)
      {
        return false;
      }

      MachinePartReceiver receiver = anchor.GetComponent<MachinePartReceiver>();
      if (receiver != null && hub.TrySubmitFocusedReceiver(receiver))
      {
        return true;
      }

      MachinePart part = anchor.GetComponent<MachinePart>();
      if (part != null && hub.TryPickupFocusedPart(part))
      {
        return true;
      }

      return false;
    }
  }
}
