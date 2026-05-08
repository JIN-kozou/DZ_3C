using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class HeightFog : ScriptableRendererFeature
    {
        HeightFogRenderPass pass;

        public override void Create()
        {
            pass = new HeightFogRenderPass();
            name = "Height Fog";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pass.Dispose();
            base.Dispose(disposing);
        }

        class HeightFogRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            public HeightFogRenderPass()
            {
                profilingSampler = new ProfilingSampler("Height Fog");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/HeightFog");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/HeightFog\".");
                    return;
                }

                material = new Material(shader);
            }

            private static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;

                return descriptor;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ResetTarget();

                var descriptor = GetCopyPassDescriptor(cameraTextureDescriptor);
                RenderingUtils.ReAllocateIfNeeded(ref tempTexHandle, descriptor);

                base.Configure(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.isPreviewCamera)
                {
                    return;
                }

                if (material == null)
                {
                    CreateMaterial();
                }

                CommandBuffer cmd = CommandBufferPool.Get();

                // Set Height Fog effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                material.SetFloat("_FogStrength", settings.strength.value);
                material.SetFloat("_StartDistance", settings.startDistance.value);
                material.SetColor("_DistanceStartColor", settings.distanceStartColor.value);
                material.SetFloat("_EndDistance", settings.endDistance.value);
                material.SetColor("_DistanceEndColor", settings.distanceEndColor.value);
                material.SetFloat("_DistanceFalloff", settings.distanceFalloff.value);
                material.SetFloat("_StartHeight", settings.startHeight.value);
                material.SetColor("_HeightStartColor", settings.heightStartColor.value);
                material.SetFloat("_EndHeight", settings.endHeight.value);
                material.SetColor("_HeightEndColor", settings.heightEndColor.value);
                material.SetFloat("_HeightFalloff", settings.heightFalloff.value);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Height Fog effect.
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    Blit(cmd, cameraTargetHandle, tempTexHandle);
                    Blit(cmd, tempTexHandle, cameraTargetHandle, material, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                tempTexHandle?.Release();
            }

#if UNITY_6000_0_OR_NEWER

            private class CopyPassData
            {
                public TextureHandle inputTexture;
            }

            private class MainPassData
            {
                public Material material;
                public TextureHandle inputTexture;
            }

            private static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source)
            {
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, false);
            }

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle source, Material material)
            {
                // Set Height Fog effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
                material.SetFloat("_FogStrength", settings.strength.value);
                material.SetFloat("_StartDistance", settings.startDistance.value);
                material.SetColor("_DistanceStartColor", settings.distanceStartColor.value);
                material.SetFloat("_EndDistance", settings.endDistance.value);
                material.SetColor("_DistanceEndColor", settings.distanceEndColor.value);
                material.SetFloat("_DistanceFalloff", settings.distanceFalloff.value);
                material.SetFloat("_StartHeight", settings.startHeight.value);
                material.SetColor("_HeightStartColor", settings.heightStartColor.value);
                material.SetFloat("_EndHeight", settings.endHeight.value);
                material.SetColor("_HeightEndColor", settings.heightEndColor.value);
                material.SetFloat("_HeightFalloff", settings.heightFalloff.value);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_HeightFogColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("HeightFog_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("HeightFog_MainPass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = copiedColor;

                    builder.UseTexture(copiedColor, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(context.cmd, data.inputTexture, data.material));
                }
            }

#endif
        }
    }
}
