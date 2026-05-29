using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws Appear reveal opaque submeshes inside URP: DepthNormals during prepass (for Fancy Neon / SSAO),
/// ForwardLit after opaques (color + depth).
/// </summary>
public sealed class AppearRevealOpaqueRenderFeature : ScriptableRendererFeature
{
    public const int ForwardLitPassIndex = 0;
    public const int DepthOnlyPassIndex = 1;
    public const int DepthNormalsPassIndex = 2;

    static class UrpPrepassTargets
    {
        static readonly FieldInfo NormalsField = typeof(UniversalRenderer).GetField(
            "m_NormalsTexture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo DepthField = typeof(UniversalRenderer).GetField(
            "m_DepthTexture",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryGet(ScriptableRenderer renderer, out RTHandle depth, out RTHandle normals)
        {
            depth = null;
            normals = null;

            if (renderer is not UniversalRenderer universalRenderer)
                return false;

            normals = NormalsField?.GetValue(universalRenderer) as RTHandle;
            depth = DepthField?.GetValue(universalRenderer) as RTHandle;
            return depth != null && normals != null;
        }
    }

    sealed class AppearRevealOpaqueDrawPass : ScriptableRenderPass
    {
        static readonly int RevealId = Shader.PropertyToID("_Reveal");
        static readonly RTHandle[] ColorTargets = new RTHandle[1];

        readonly string _profilerTag;
        readonly int _shaderPassIndex;
        readonly bool _bindPrepassTargets;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public AppearRevealOpaqueDrawPass(string profilerTag, RenderPassEvent passEvent, int shaderPassIndex, bool bindPrepassTargets)
        {
            _profilerTag = profilerTag;
            _shaderPassIndex = shaderPassIndex;
            _bindPrepassTargets = bindPrepassTargets;
            renderPassEvent = passEvent;
            profilingSampler = new ProfilingSampler(profilerTag);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!_bindPrepassTargets)
                return;

            if (!UrpPrepassTargets.TryGet(renderingData.cameraData.renderer, out RTHandle depth, out RTHandle normals))
                return;

            ColorTargets[0] = normals;
            ConfigureTarget(ColorTargets, depth);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var requests = AppearRevealOpaqueDrawRegistry.ActiveRequests;
            if (requests.Count == 0)
                return;

            Camera camera = renderingData.cameraData.camera;
            if (camera == null)
                return;

            if (_bindPrepassTargets && !UrpPrepassTargets.TryGet(renderingData.cameraData.renderer, out _, out _))
                return;

            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    AppearRevealOpaqueDrawRegistry.DrawRequest request = requests[i];
                    if (request.mesh == null || request.material == null)
                        continue;

                    if ((camera.cullingMask & (1 << request.layer)) == 0)
                        continue;

                    if (request.submeshIndex < 0 || request.submeshIndex >= request.mesh.subMeshCount)
                        continue;

                    _block.Clear();
                    _block.SetFloat(RevealId, request.reveal);

                    cmd.DrawMesh(
                        request.mesh,
                        request.matrix,
                        request.material,
                        request.submeshIndex,
                        _shaderPassIndex,
                        _block);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    AppearRevealOpaqueDrawPass _depthNormalsPass;
    AppearRevealOpaqueDrawPass _forwardPass;

    public override void Create()
    {
        _depthNormalsPass = new AppearRevealOpaqueDrawPass(
            "AppearRevealOpaque DepthNormals",
            RenderPassEvent.AfterRenderingPrePasses,
            DepthNormalsPassIndex,
            bindPrepassTargets: true);
        _depthNormalsPass.ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);

        _forwardPass = new AppearRevealOpaqueDrawPass(
            "AppearRevealOpaque Forward",
            RenderPassEvent.AfterRenderingOpaques,
            ForwardLitPassIndex,
            bindPrepassTargets: false);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (AppearRevealOpaqueDrawRegistry.ActiveRequests.Count == 0)
            return;

        renderer.EnqueuePass(_depthNormalsPass);
        renderer.EnqueuePass(_forwardPass);
    }
}
