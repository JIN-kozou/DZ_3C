using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将源 GameObject 上勾选的组件（含序列化数据）复制到一个或多个目标 GameObject。
/// 使用 EditorUtility.CopySerialized；跨物体引用可能仍指向源场景中的对象，需手动检查。
/// </summary>
public class GameObjectComponentCopyTool : EditorWindow
{
    private GameObject source;
    private readonly List<GameObject> targets = new List<GameObject>();
    private Vector2 scrollComponents;
    private Vector2 scrollTargets;
    private readonly List<bool> componentEnabled = new List<bool>();
    private Component[] sourceComponents = Array.Empty<Component>();

    [MenuItem("Tools/GameObject 组件批量复制")]
    public static void Open()
    {
        GetWindow<GameObjectComponentCopyTool>("组件批量复制");
    }

    private void OnEnable()
    {
        RefreshSourceComponents();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("源物体", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        GameObject newSource = (GameObject)EditorGUILayout.ObjectField(source, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && newSource != source)
        {
            source = newSource;
            RefreshSourceComponents();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("要复制的组件", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(60)))
            SetAllComponentToggles(true);
        if (GUILayout.Button("全不选", GUILayout.Width(60)))
            SetAllComponentToggles(false);
        if (GUILayout.Button("反选", GUILayout.Width(60)))
            InvertComponentToggles();
        EditorGUILayout.EndHorizontal();

        scrollComponents = EditorGUILayout.BeginScrollView(scrollComponents, GUILayout.MinHeight(120), GUILayout.MaxHeight(260));
        if (source == null)
        {
            EditorGUILayout.HelpBox("请指定源 GameObject。", MessageType.Info);
        }
        else if (sourceComponents.Length == 0)
        {
            EditorGUILayout.HelpBox("源物体上没有可复制的组件。", MessageType.Warning);
        }
        else
        {
            for (int i = 0; i < sourceComponents.Length; i++)
            {
                Component c = sourceComponents[i];
                if (c == null)
                    continue;

                EnsureToggleCount();
                string label = $"{c.GetType().Name}  ({c.GetType().FullName})";
                componentEnabled[i] = EditorGUILayout.ToggleLeft(label, componentEnabled[i]);
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("目标物体列表", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从当前选中添加"))
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go != null && !targets.Contains(go))
                    targets.Add(go);
            }
        }
        if (GUILayout.Button("清空列表", GUILayout.Width(80)))
            targets.Clear();
        EditorGUILayout.EndHorizontal();

        scrollTargets = EditorGUILayout.BeginScrollView(scrollTargets, GUILayout.MinHeight(80), GUILayout.MaxHeight(200));
        for (int i = 0; i < targets.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            targets[i] = (GameObject)EditorGUILayout.ObjectField(targets[i], typeof(GameObject), true);
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                targets.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        using (new EditorGUI.DisabledScope(!CanCopy()))
        {
            if (GUILayout.Button("复制到所有目标", GUILayout.Height(28)))
                CopyToAllTargets();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "说明：对已有同类型组件会覆盖序列化数据；没有则先 AddComponent 再写入。" +
            " Transform 也可勾选以同步本地坐标/旋转/缩放等。" +
            " 脚本间引用若指向源物体上的组件，复制后可能仍指向源，请在 Inspector 中核对。",
            MessageType.None);
    }

    private bool CanCopy()
    {
        if (source == null)
            return false;
        if (targets.Count == 0 || targets.All(t => t == null))
            return false;
        for (int i = 0; i < sourceComponents.Length; i++)
        {
            if (sourceComponents[i] != null && i < componentEnabled.Count && componentEnabled[i])
                return true;
        }
        return false;
    }

    private void CopyToAllTargets()
    {
        int copied = 0;
        int failed = 0;

        foreach (GameObject dst in targets)
        {
            if (dst == null || dst == source)
                continue;

            Undo.SetCurrentGroupName("Copy GameObject components");
            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < sourceComponents.Length; i++)
            {
                Component srcComp = sourceComponents[i];
                if (srcComp == null)
                    continue;
                if (i >= componentEnabled.Count || !componentEnabled[i])
                    continue;

                Type type = srcComp.GetType();
                try
                {
                    Component dstComp = dst.GetComponent(type);
                    if (dstComp == null)
                        dstComp = Undo.AddComponent(dst, type);

                    Undo.RecordObject(dstComp, "Paste component values");
                    EditorUtility.CopySerialized(srcComp, dstComp);
                    EditorUtility.SetDirty(dstComp);
                    copied++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.LogWarning($"[组件复制] 跳过 {type.Name} → {dst.name}: {ex.Message}", dst);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        Debug.Log($"[组件复制] 完成：成功写入 {copied} 条组件操作，失败/跳过 {failed} 次。目标数量：{targets.Count(t => t != null)}。");
    }

    private void RefreshSourceComponents()
    {
        componentEnabled.Clear();
        if (source == null)
        {
            sourceComponents = Array.Empty<Component>();
            return;
        }

        sourceComponents = source.GetComponents<Component>().Where(c => c != null).ToArray();
        for (int i = 0; i < sourceComponents.Length; i++)
            componentEnabled.Add(true);
    }

    private void EnsureToggleCount()
    {
        while (componentEnabled.Count < sourceComponents.Length)
            componentEnabled.Add(true);
        while (componentEnabled.Count > sourceComponents.Length)
            componentEnabled.RemoveAt(componentEnabled.Count - 1);
    }

    private void SetAllComponentToggles(bool value)
    {
        EnsureToggleCount();
        for (int i = 0; i < componentEnabled.Count; i++)
            componentEnabled[i] = value;
    }

    private void InvertComponentToggles()
    {
        EnsureToggleCount();
        for (int i = 0; i < componentEnabled.Count; i++)
            componentEnabled[i] = !componentEnabled[i];
    }
}
