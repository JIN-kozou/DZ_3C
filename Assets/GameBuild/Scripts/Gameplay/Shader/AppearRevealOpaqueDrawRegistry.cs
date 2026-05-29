using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Per-frame draw list for Appear reveal opaque submeshes. Consumed by <see cref="AppearRevealOpaqueRenderFeature"/>.
/// </summary>
public static class AppearRevealOpaqueDrawRegistry
{
    public struct DrawRequest
    {
        public Mesh mesh;
        public Matrix4x4 matrix;
        public Material material;
        public int submeshIndex;
        public float reveal;
        public int layer;
    }

    static readonly List<DrawRequest> Requests = new List<DrawRequest>();
    static int _lastClearFrame = -1;

    public static IReadOnlyList<DrawRequest> ActiveRequests => Requests;

    public static void EnsureCurrentFrame()
    {
        int frame = Time.frameCount;
        if (_lastClearFrame == frame)
            return;

        _lastClearFrame = frame;
        Requests.Clear();
    }

    public static void Add(in DrawRequest request)
    {
        Requests.Add(request);
    }
}
