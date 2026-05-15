using UnityEngine;

public static class AudioDefaultPrefabs
{
    public static GameObject Load(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }
}
