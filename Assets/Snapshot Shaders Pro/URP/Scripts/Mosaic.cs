using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class Mosaic : ScriptableRendererFeature
    {
        MosaicRenderPass pass;

        public override void Create()
        {
            pass = new MosaicRenderPass();
            name = "Mosaic";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<MosaicSettings>();

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

        class MosaicRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            private int xTileCount;
            private int yTileCount;

            public MosaicRenderPass()
            {
                profilingSampler = new ProfilingSampler("Mosaic");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/Mosaic");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Mosaic\".");
                    return;
                }

                material = new Material(shader);
            }

            private static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;

                int screenWidth = descriptor.width;
                int screenHeight = descriptor.height;

                var settings = VolumeManager.instance.stack.GetComponent<MosaicSettings>();
                float xTileCount = settings.xTileCount.value;
                float yTileCount = Mathf.RoundToInt((float)screenHeight / screenWidth * xTileCount);
                FilterMode filterMode = settings.usePointFiltering.value ? FilterMode.Point : FilterMode.Bilinear;

                descriptor.width = (int)xTileCount;
                descriptor.height = (int)yTileCount;

                return descriptor;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ResetTarget();

                var descriptor = GetCopyPassDescriptor(cameraTextureDescriptor);
                xTileCount = descriptor.width;
                yTileCount = descriptor.height;

                RenderingUtils.ReAllocateIfNeeded(ref tempTexHandle, descriptor, filterMode: FilterMode.Point);

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

                // Set Mosaic effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<MosaicSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                material.SetTexture("_OverlayTex", settings.overlayTexture.value ?? Texture2D.whiteTexture);
                material.SetColor("_OverlayColor", settings.overlayColor.value);
                material.SetInt("_XTileCount", xTileCount);
                material.SetInt("_YTileCount", yTileCount);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Mosaic effect.
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

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle source, Material material, int xTileCount, int yTileCount)
            {
                // Set Mosaic effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<MosaicSettings>();
                material.SetTexture("_OverlayTex", settings.overlayTexture.value ?? Texture2D.whiteTexture);
                material.SetColor("_OverlayColor", settings.overlayColor.value);
                material.SetInt("_XTileCount", xTileCount);
                material.SetInt("_YTileCount", yTileCount);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<MosaicSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                xTileCount = colorCopyDescriptor.width;
                yTileCount = colorCopyDescriptor.height;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_MosaicColorCopy", false, filterMode: FilterMode.Point);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Mosaic_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("Mosaic_MainPass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = copiedColor;

                    builder.UseTexture(copiedColor, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(context.cmd, data.inputTexture, data.material, xTileCount, yTileCount));
                }
            }

#endif
        }
    }
}
