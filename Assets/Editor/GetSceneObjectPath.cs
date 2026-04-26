using System.Linq;
using UnityEditor;
using UnityEngine;

public class GetSceneObjectPath : Editor
{
    static string objPath = string.Empty;

    [MenuItem("GameObject/获取该物体的路径")]
    static void GetPath()
    {
        objPath = string.Empty;
        GameObject gameObject = Selection.objects.First() as GameObject;
        GetPathString(gameObject.transform);
        if (objPath.EndsWith("/"))//移除最后一位的/
        {
            objPath=objPath.Remove(objPath.Length - 1);
        }
        GUIUtility.systemCopyBuffer = objPath;
        Debug.Log(objPath);
        Debug.Log("成功复制路径！");
    }
   
    private static void GetPathString(Transform obj)
    {
        if (obj != null&&obj.parent!=null)
        {
            objPath = objPath.Insert(0, $"{obj.name}/");
            GetPathString(obj.parent);
        }
    }
}
