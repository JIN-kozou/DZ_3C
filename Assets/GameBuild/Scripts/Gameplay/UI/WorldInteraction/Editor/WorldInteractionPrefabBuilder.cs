#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DZ_3C.UI.WorldInteraction.Editor
{
  public static class WorldInteractionPrefabBuilder
  {
    [MenuItem("DZ_3C/UI/Build World Interaction Prefabs")]
    public static void BuildPrefabs()
    {
      BuildPromptPrefab();
      BuildMarkerPrefab();
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      Debug.Log("[WorldInteraction] Built prompt and marker prefabs.");
    }

    private static void BuildPromptPrefab()
    {
      const string path = WorldInteractionUIBuilder.DefaultPromptPrefabPath;
      EnsureParentFolder(path);

      var temp = new GameObject("WorldInteractionPrompt_Temp");
      try
      {
        WorldInteractionPromptView view = WorldInteractionUIBuilder.CreatePrompt(temp.transform);
        GameObject root = view.gameObject;
        root.transform.SetParent(null, false);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
      }
      finally
      {
        Object.DestroyImmediate(temp);
      }
    }

    private static void BuildMarkerPrefab()
    {
      const string path = WorldInteractionUIBuilder.DefaultMarkerPrefabPath;
      EnsureParentFolder(path);

      var temp = new GameObject("WorldInteractionMarker_Temp");
      try
      {
        WorldInteractionMarkerView view = WorldInteractionUIBuilder.CreateMarker(temp.transform);
        GameObject root = view.gameObject;
        root.transform.SetParent(null, false);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
      }
      finally
      {
        Object.DestroyImmediate(temp);
      }
    }

    private static void EnsureParentFolder(string assetPath)
    {
      string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
      if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir))
      {
        return;
      }

      string[] parts = dir.Split('/');
      string current = parts[0];
      for (int i = 1; i < parts.Length; i++)
      {
        string next = current + "/" + parts[i];
        if (!AssetDatabase.IsValidFolder(next))
        {
          AssetDatabase.CreateFolder(current, parts[i]);
        }

        current = next;
      }
    }
  }
}
#endif
