using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spline))]
public class SplineInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var spline = (Spline)target;

        DrawPropertyExceptSegments("DraggablePrefab");
        DrawPropertyExceptSegments("NewNodeOffset");
        DrawPropertyExceptSegments("MoveSpeed");
        DrawPropertyExceptSegments("SpawnStartDelay");
        DrawSpawnDelayClamped();
        DrawPropertyExceptSegments("SpawnCount");
        DrawPropertyExceptSegments("DraggableSplineEndAction");
        DrawPropertyExceptSegments("PreWarmTime");
        DrawPropertyExceptSegments(nameof(Spline.DebugLogSplineLength));

        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(nameof(Spline.UseStaticGuideLine)),
            new GUIContent(
                "Use Static Guide Line",
                "编辑器：勾选后显示路径预览（与 Move Speed 无关）。运行时：勾选且 Move Speed≤0 时生成静止引导线。"));

        var gapProp = serializedObject.FindProperty(nameof(Spline.GuideGap));
        EditorGUILayout.PropertyField(gapProp, new GUIContent("Guide Gap", "沿弧长方向相邻引导物间距"));
        if (gapProp.floatValue < 0.01f)
            gapProp.floatValue = 0.01f;

        EditorGUILayout.LabelField(
            "Last spline length (m)",
            spline.LastComputedSplineLength > 1e-4f ? spline.LastComputedSplineLength.ToString("F2") : "(未 Init，勾选 Static 后会更新)");

        if (!Application.isPlaying && spline.UseStaticGuideLine &&
            spline.LastComputedSplineLength > 1e-4f &&
            spline.GuideGap > spline.LastComputedSplineLength * 0.99f)
        {
            EditorGUILayout.HelpBox(
                "Guide Gap 大于近似总长，沿路可能只在起点出现一个点（或看起来像「没预览」）。把 Gap 调小或看这行 Total。",
                MessageType.Warning);
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(spline);
            // Play：SerializedObject.Apply 有时会漏掉 OnValidate，这里强制刷新样条折线与静止引导
            if (Application.isPlaying)
                spline.Init();
        }

        if (!Application.isPlaying && spline.UseStaticGuideLine)
            EditorGUILayout.HelpBox(
                "编辑预览：预览物带 DontSave 不写磁盘。若为纯粒子Prefab，编辑器不会像 Play 一样自动模拟；Spline 已对预览做一次 Simulate，若仍看不到请在 Scene 视图左上角下拉菜单勾选显示粒子特效（Effects）。更稳妥可把「占位网格」Prefab 指派给 Draggable Prefab。", MessageType.Info);
        else if (spline.UseStaticGuideLine && spline.MoveSpeed > 0f)
            EditorGUILayout.HelpBox("运行时要生效静止引导，请把 Move Speed 设为 0。", MessageType.Info);

        EditorGUILayout.Space(4f);

        if (GUILayout.Button("Add point"))
        {
            Undo.RecordObject(spline, "Add Spline Point");
            spline.AddNode();
            EditorUtility.SetDirty(spline);
            serializedObject.Update();
            if (!Application.isPlaying)
                spline.RebuildSplineInEditorAfterInspectorEdit();
        }

        if (spline.Segments != null && spline.Segments.Count > 0 && GUILayout.Button("Close"))
        {
            Undo.RecordObject(spline, "Close Spline");
            spline.Close();
            EditorUtility.SetDirty(spline);
            serializedObject.Update();
            if (!Application.isPlaying)
                spline.RebuildSplineInEditorAfterInspectorEdit();
        }
    }

    void DrawPropertyExceptSegments(string propertyName)
    {
        var p = serializedObject.FindProperty(propertyName);
        if (p != null)
            EditorGUILayout.PropertyField(p, true);
    }

    void DrawSpawnDelayClamped()
    {
        var p = serializedObject.FindProperty(nameof(Spline.SpawnDelay));
        EditorGUILayout.PropertyField(p, true);
        if (p.floatValue < 0.1f)
            p.floatValue = 0.1f;
    }
}
