using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DZ_3C.UI.WorldInteraction
{
  internal static class WorldInteractionPrefabCache
  {
    private static GameObject _promptPrefab;
    private static GameObject _markerPrefab;

    public static GameObject LoadPromptPrefab()
    {
      if (_promptPrefab != null)
      {
        return _promptPrefab;
      }

#if UNITY_EDITOR
      _promptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldInteractionUIBuilder.DefaultPromptPrefabPath);
#endif
      if (_promptPrefab == null)
      {
        _promptPrefab = Resources.Load<GameObject>("WorldInteraction/WorldInteractionPrompt");
      }

      return _promptPrefab;
    }

    public static GameObject LoadMarkerPrefab()
    {
      if (_markerPrefab != null)
      {
        return _markerPrefab;
      }

#if UNITY_EDITOR
      _markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldInteractionUIBuilder.DefaultMarkerPrefabPath);
#endif
      if (_markerPrefab == null)
      {
        _markerPrefab = Resources.Load<GameObject>("WorldInteraction/WorldInteractionMarker");
      }

      return _markerPrefab;
    }
  }
}
