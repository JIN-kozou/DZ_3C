using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SplineDraggableContext
{
    public GameObject Dragable;
    public float Time;
    public bool Invalid;
    public GameObject DependentObject;
}

public enum DraggableSplineEndAction
{
    Respawn,
    Destroy
}

public class Spline : MonoBehaviour
{
    public GameObject DraggablePrefab;
    public float NewNodeOffset;
    public List<SplineSegment> Segments = new List<SplineSegment>();
    public float SpawnStartDelay = 0;
    public float SpawnDelay = 1;
    public int SpawnCount = 0;
    public float MoveSpeed = 0.5f;
    public DraggableSplineEndAction DraggableSplineEndAction;
    public float PreWarmTime = 100f;

    [Tooltip("编辑器勾选：场景内预览静止引导（与 MoveSpeed 无关）。运行时可配合 MoveSpeed≤0 生成真实静止引导线。")]
    public bool UseStaticGuideLine;
    [Tooltip("静止引导线在路径上的相邻间距（沿弧长）")]
    public float GuideGap = 1f;

    /// <summary>最近一次 Init() 得到的样条折线总长（约值），Inspector 可读。</summary>
    public float LastComputedSplineLength { get; private set; }

    [Tooltip("勾选后每次 Init() 会向 Console 打印样条总长（频繁刷日志时请关闭）")]
    public bool DebugLogSplineLength;

    private float _spawnTimer;
    private float _totalLen;
    private int _spawned;

    private List<SplineDraggableContext> _contexts = new List<SplineDraggableContext>();
    private readonly List<GameObject> _staticGuideInstances = new List<GameObject>();

    // Play Mode：运行时改 Inspector（预制体 / Gap）时用于检测并即时重建静止引导
    GameObject _cachedGuideDraggablePrefab;
    float _cachedGuideGap = -1f;
    bool _cachedUseStaticGuideLine;
    float _cachedMoveSpeed = float.NaN;
    public void AddNode()
    {
        Vector3 point = transform.position;
        Vector3 dir = Vector3.forward;
        float len = NewNodeOffset;
        if (_lines != null && _lines.Count > 0)
        {
            dir = (_lines.Last().To - _lines.Last().From).normalized;
            point = _lines.Last().From;
        }

        var obj = new GameObject($"Spline{Segments.Count}");
        obj.transform.parent = transform;
        obj.transform.position = point + dir * len;
        var segment = obj.AddComponent<SplineSegment>();

        var lastSegment = Segments.LastOrDefault();

        segment.H1 = new GameObject("Handle1").AddComponent<SplineHandle>();
        segment.H2 = new GameObject("Handle2").AddComponent<SplineHandle>();
        segment.H1.transform.parent = segment.transform;
        segment.H1.transform.position = point + dir * (len / 3);
        segment.H2.transform.parent = segment.transform;
        segment.H2.transform.position = point + dir * ((len / 3) * 2);

        Segments.Add(segment);
    }

    public void Close()
    {
        var first = Segments.First();
        var last = Segments.Last();
        last.transform.position = transform.position;
        Init();

        Segments.First().H1.SnapOppositeToAxis();
    }

    private bool Changed()
    {
        return Segments.Select(_ => _.Changed()).ToArray().Any(_ => _);
    }

    void Start()
    {
        // Play 开始后若线段未拖动，Changed() 为 false，首帧可能不会 Init，导致 _lines 为空、静止引导永远不生成，
        // 只会走动态 Instantiate（Hierarchy 全是 FX_Arrow_5(Clone) 而没有 SplineGuidePreview）。
        Init();
        SnapStaticGuideParamCache();
    }

    void OnValidate()
    {
        // Play：自定义 Inspector 在 ApplyModifiedProperties 后会显式 Init，避免与本回调叠两次
        if (Application.isPlaying)
            return;
        Init();
    }

    bool ShouldUseStaticGuideLine()
    {
        return UseStaticGuideLine && MoveSpeed <= 0f;
    }

    /// <summary>是否沿样条布置静止引导物：编辑模式勾选即可预览；运行时还需 MoveSpeed≤0。</summary>
    bool ShouldPlaceStaticGuideInstances()
    {
        if (!UseStaticGuideLine)
            return false;
        return Application.isPlaying ? MoveSpeed <= 0f : true;
    }

    /// <summary>供编辑器 Inspector 在非 Play 模式下在改字段、AddNode 等之后立刻刷新预览。</summary>
    public void RebuildSplineInEditorAfterInspectorEdit()
    {
        if (Application.isPlaying)
            return;
        Init();
    }

    void SnapStaticGuideParamCache()
    {
        _cachedGuideDraggablePrefab = DraggablePrefab;
        _cachedGuideGap = GuideGap;
        _cachedUseStaticGuideLine = UseStaticGuideLine;
        _cachedMoveSpeed = MoveSpeed;
    }

    bool StaticGuideInspectorParamsDirty()
    {
        return DraggablePrefab != _cachedGuideDraggablePrefab
               || !Mathf.Approximately(GuideGap, _cachedGuideGap)
               || UseStaticGuideLine != _cachedUseStaticGuideLine
               || !Mathf.Approximately(MoveSpeed, _cachedMoveSpeed);
    }

    void DestroySplineOwnedObject(GameObject go)
    {
        if (go == null)
            return;
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
    }

    void ClearStaticGuideInstances()
    {
        foreach (var go in _staticGuideInstances)
            DestroySplineOwnedObject(go);
        _staticGuideInstances.Clear();
    }

    void ClearDynamicContexts()
    {
        foreach (var ctx in _contexts)
            DestroySplineOwnedObject(ctx.Dragable);
        _contexts.Clear();
        _spawned = 0;
        _spawnTimer = 0f;
    }

    bool IsSplineClosed()
    {
        if (Segments == null || Segments.Count < 2)
            return false;
        var lastSeg = Segments[Segments.Count - 1];
        return lastSeg != null &&
               Vector3.Distance(lastSeg.transform.position, transform.position) < 1e-4f;
    }

    bool TryGetPoseAtArcLength(float arcLen, out Vector3 position, out Quaternion rotation)
    {
        position = transform.position;
        rotation = Quaternion.LookRotation(transform.forward);
        if (_lines == null || _lines.Count == 0 || _totalLen < 1e-5f)
            return false;

        float clamped = Mathf.Clamp(arcLen, 0f, _totalLen);
        float remaining = clamped;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            if (remaining <= line.Length + 1e-6f || i == _lines.Count - 1)
            {
                var t = line.Length > 1e-8f ? Mathf.Clamp01(remaining / line.Length) : 0f;
                position = Vector3.Lerp(line.From, line.To, t);
                var fwd = line.To - line.From;
                rotation = fwd.sqrMagnitude > 1e-8f
                    ? Quaternion.LookRotation(fwd.normalized)
                    : Quaternion.identity;
                return true;
            }

            remaining -= line.Length;
        }

        var lastLine = _lines[_lines.Count - 1];
        position = lastLine.To;
        var lf = lastLine.To - lastLine.From;
        rotation = lf.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(lf.normalized) : Quaternion.identity;
        return true;
    }

    void FlagStaticGuideAsEditorPreview(GameObject root)
    {
        if (root == null || Application.isPlaying)
            return;
#if UNITY_EDITOR
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags |= HideFlags.DontSave;
#endif
    }

    /// <summary>纯粒子/特效预制体在 Edit 模式下不会自动 Tick；预跑一点时间让 Scene 里能画出当前帧。</summary>
    void EditorPreviewBakeParticleSnapshots(GameObject instance)
    {
        if (Application.isPlaying || instance == null)
            return;

        var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0)
            return;

        foreach (var ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            const float warmupSeconds = 4f;
            ps.Simulate(warmupSeconds, true, true, true);
        }
    }

    void RegisterStaticGuidePreviewInstance(GameObject inst)
    {
        FlagStaticGuideAsEditorPreview(inst);
        EditorPreviewBakeParticleSnapshots(inst);
        _staticGuideInstances.Add(inst);
    }

    void RebuildStaticGuideInstances()
    {
        ClearStaticGuideInstances();

        if (!ShouldPlaceStaticGuideInstances())
        {
            SnapStaticGuideParamCache();
            return;
        }

        if (DraggablePrefab == null)
        {
            SnapStaticGuideParamCache();
            return;
        }

        var gap = Mathf.Max(GuideGap, 0.01f);
        if (_totalLen < 1e-5f || _lines == null || _lines.Count == 0)
        {
            SnapStaticGuideParamCache();
            return;
        }

        var closed = IsSplineClosed();
        const int kMaxGuideInstances = 4096;

        if (closed)
        {
            for (var d = 0f; d < _totalLen - 1e-4f && _staticGuideInstances.Count < kMaxGuideInstances; d += gap)
            {
                TryGetPoseAtArcLength(d, out var pos, out var rot);
                var inst = (GameObject)Instantiate(DraggablePrefab, transform);
                inst.transform.SetPositionAndRotation(pos, rot);
                inst.name = $"{DraggablePrefab.name}_SplineGuidePreview_{_staticGuideInstances.Count}";
                RegisterStaticGuidePreviewInstance(inst);
            }
        }
        else
        {
            for (var d = 0f; d <= _totalLen + 1e-4f && _staticGuideInstances.Count < kMaxGuideInstances; d += gap)
            {
                var arcLen = Mathf.Min(d, _totalLen);
                TryGetPoseAtArcLength(arcLen, out var pos, out var rot);
                var inst = (GameObject)Instantiate(DraggablePrefab, transform);
                inst.transform.SetPositionAndRotation(pos, rot);
                inst.name = $"{DraggablePrefab.name}_SplineGuidePreview_{_staticGuideInstances.Count}";
                RegisterStaticGuidePreviewInstance(inst);

                if (arcLen >= _totalLen - 1e-4f)
                    break;
            }
        }

        SnapStaticGuideParamCache();
    }

    void DrawDraggable(SplineDraggableContext ctx, float delta)
    {
        if (ShouldUseStaticGuideLine())
            return;

        ctx.Time += delta;

        var dist = MoveSpeed * ctx.Time;
        if (dist > _totalLen)
        {
            if (DraggableSplineEndAction == DraggableSplineEndAction.Respawn)
            {
                dist = dist % _totalLen;
                var loopTime = _totalLen / MoveSpeed;
                ctx.Time = ctx.Time % loopTime;
            }
            else
            {
                ctx.Invalid = true;
                return;
            }
        }

        for (int i = 0; i < _lines.Count; ++i)
        {
            if (dist < _lines[i].Length)
            {
                var dir = (_lines[i].To - _lines[i].From).normalized;
                ctx.Dragable.transform.position = _lines[i].From;
                ctx.Dragable.transform.LookAt(_lines[i].To);
                return;
            }
            else
                dist -= _lines[i].Length;
        }
    }

    void DoPrewarm()
    {
        if (ShouldUseStaticGuideLine())
            return;

        while (PreWarmTime > 0)
        {
            var dt = 0f;
            if (SpawnCount == 0 || _spawned < SpawnCount)
            {
                if (_contexts.Count == 0)
                {
                    var d = SpawnStartDelay;
                    PreWarmTime -= d;

                }
                else
                {
                    var d = SpawnDelay;
                    PreWarmTime -= d;
                }

                var obj = Instantiate(DraggablePrefab, transform);

                var particleSystem = obj.GetComponentInChildren<ParticleSystem>();
                _contexts.Add(new SplineDraggableContext()
                {
                    Dragable = obj,
                    DependentObject = particleSystem != null ? particleSystem.gameObject : null
                });
                ++_spawned;

                if (PreWarmTime > 0)
                {
                    dt = Mathf.Min(SpawnDelay, PreWarmTime);
                    PreWarmTime -= dt;
                }
            }
            else
            {
                dt = PreWarmTime;
                PreWarmTime = 0;
            }

            foreach (var context in _contexts)
            {
                DrawDraggable(context, dt);
                if (context.Invalid || context.DependentObject == null)
                    Destroy(context.Dragable);
            }

            _contexts.RemoveAll(_ => _.Invalid || _.DependentObject == null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Changed())
            Init();
        if(Segments.RemoveAll(_ => _ == null) > 0)
            Init();

        // Play：Inspector 必须用 SerializedObject.ApplyModifiedProperties；仍用每帧 dirty 比对兜底
        if (!Application.isPlaying)
            return;

        var placeStatic = ShouldPlaceStaticGuideInstances();

        if (_staticGuideInstances.Count > 0 && !placeStatic)
        {
            ClearStaticGuideInstances();
            SnapStaticGuideParamCache();
        }

        if (placeStatic)
        {
            ClearDynamicContexts();
            PreWarmTime = 0;
            if (StaticGuideInspectorParamsDirty())
                RebuildStaticGuideInstances();
            return;
        }

        if (PreWarmTime > 0)
        {
            DoPrewarm();
        }

        var dt = Time.deltaTime;
        if (SpawnCount == 0 || _spawned < SpawnCount)
        {
            var spawnNew = false;
            _spawnTimer += dt;
            if (_contexts.Count == 0)
            {
                if (spawnNew = _spawnTimer > SpawnStartDelay)
                {
                    _spawnTimer -= SpawnStartDelay;
                }
            }
            else
            {
                if (spawnNew = _spawnTimer > SpawnDelay)
                    _spawnTimer -= SpawnDelay;
            }

            if (spawnNew)
            {
                var obj = Instantiate(DraggablePrefab, transform);

                var particleSystem = obj.GetComponentInChildren<ParticleSystem>();
                _contexts.Add(new SplineDraggableContext()
                {
                    Dragable = obj,
                    DependentObject = particleSystem != null ? particleSystem.gameObject : null
                });
                ++_spawned;
            }
        }

        foreach (var context in _contexts)
        {
            DrawDraggable(context, dt);
            if (context.Invalid || context.DependentObject == null)
                Destroy(context.Dragable);
        }

        _contexts.RemoveAll(_ => _.Invalid || _.DependentObject == null);
        PreWarmTime = 0;
    }


    public void Init()
    {
        _lines.Clear();
        _controls.Clear();
        _totalLen = 0;
        Segments.RemoveAll(_ => _ == null);
        var p0 = transform.position;
        for(var s = 0; s < Segments.Count; ++s)
        {
            var segment = Segments[s];
            Vector3 p1 = segment.H1.transform.position;
            Vector3 p2 = segment.H2.transform.position;
            Vector3 p3 = segment.transform.position;
            Vector3 v0 = p0;

            Gizmos.color = Color.white;
            for (int i = 1; i < 1001; i++)
            {
                var t = i / 1000.0f;
                var v1 = Mathf.Pow(1f - t, 3) * p0
                         + 3 * t * Mathf.Pow(1 - t, 2) * p1
                         + 3 * Mathf.Pow(t, 2) * (1 - t) * p2
                         + Mathf.Pow(t, 3) * p3;

                var len = Vector3.Distance(v0, v1);
                _totalLen += len;
                _lines.Add(new Line()
                {
                    From = v0,
                    To = v1,
                    Length = len
                });
                v0 = v1;
            }
            _controls.Add(new Line()
            {
                From = p0,
                To = p1
            });
            _controls.Add(new Line()
            {
                From = p3,
                To = p2
            });

            p0 = p3;

            segment.H1.Opposite = null;
            segment.H1.Origin = null;
            segment.H2.Opposite = null;
            segment.H2.Origin = null;

            if (s > 0)
            {
                var prevSegment = Segments[s - 1];
                prevSegment.H2.Opposite = segment.H1.gameObject;
                prevSegment.H2.Origin = prevSegment.gameObject;
                segment.H1.Opposite = prevSegment.H2.gameObject;
                segment.H1.Origin = prevSegment.gameObject;

                if (s + 1 == Segments.Count)
                {
                    var firstSegment = Segments.First();
                    var dist = Vector3.Distance(segment.transform.position, transform.position);
                    if (dist < 0.0000001f)
                    {
                        segment.H2.Opposite = firstSegment.H1.gameObject;
                        segment.H2.Origin = gameObject;
                        firstSegment.H1.Opposite = segment.H2.gameObject;
                        firstSegment.H1.Origin = gameObject;
                    }
                }
            }
        }

        LastComputedSplineLength = _totalLen;
        if (DebugLogSplineLength)
            Debug.Log($"[Spline:{name}] full len ≈ {_totalLen:F4} m ({Segments?.Count ?? 0} segments).", this);
        transform.hasChanged = false;

        RebuildStaticGuideInstances();
    }

    void DrawEditorStaticGuideSpacingGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || !UseStaticGuideLine)
            return;
        if (_lines == null || _lines.Count == 0 || _totalLen < 1e-5f)
            return;

        var gap = Mathf.Max(GuideGap, 0.01f);
        var closed = IsSplineClosed();

        void DrawPoint(float arcLen)
        {
            if (!TryGetPoseAtArcLength(arcLen, out var pos, out var rot))
                return;
            Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.92f);
            Gizmos.DrawSphere(pos, 0.065f * Mathf.Min(1f, _totalLen / 3f + 0.3f));
            var tip = pos + rot * (Vector3.forward * Mathf.Min(0.35f, _totalLen / 12f + 0.08f));
            Gizmos.DrawLine(pos, tip);
        }

        if (closed)
        {
            for (var d = 0f; d < _totalLen - 1e-4f; d += gap)
                DrawPoint(d);
        }
        else
        {
            for (var d = 0f; d <= _totalLen + 1e-4f; d += gap)
            {
                var arcLen = Mathf.Min(d, _totalLen);
                DrawPoint(arcLen);
                if (arcLen >= _totalLen - 1e-4f)
                    break;
            }
        }
#endif
    }

    void OnDrawGizmos()
    {
        if(Segments.RemoveAll(_ => _ == null) > 0)
            Init();
        if (Changed())
            Init();

        void DrawPolyline(Color color, List<Line> lines)
        {
            Gizmos.color = color;
            foreach (var line in lines)
                Gizmos.DrawLine(line.From, line.To);
        }

        DrawPolyline(Color.white, _lines);
        DrawPolyline(Color.green, _controls);

        DrawEditorStaticGuideSpacingGizmos();
    }

    private List<Line> _controls = new List<Line>();
    private List<Line> _lines = new List<Line>();

    struct Line
    {
        public Vector3 From;
        public Vector3 To;
        public float Length;
    }

    
}
