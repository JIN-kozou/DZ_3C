using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class LightStreaks : ScriptableRendererFeature
    {
        LightStreaksRenderPass pass;

        public override void Create()
        {
            pass = new LightStreaksRenderPass();
            name = "Light Streaks";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<LightStreaksSettings>();

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

        class LightStreaksRenderPass : ScriptableRenderPass
        {
            private Material material;

            private RTHandle tempTexHandle;
            private RTHandle blurTexHandle;
            private string profilerTag;

            public LightStreaksRenderPass()
            {
                profilingSampler = new ProfilingSampler("LightStreaks");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/LightStreaks");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Blur\".");
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

            private static RenderTextureDescriptor GetMainPassDescriptor(RenderTextureDescriptor descriptor, int downsampleAmount)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;
                descriptor.width /= downsampleAmount;
                descriptor.height /= downsampleAmount;

                return descriptor;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // Allocate the 'regular' temp texture.
                ResetTarget();

                var descriptor = GetCopyPassDescriptor(cameraTextureDescriptor);
                RenderingUtils.ReAllocateIfNeeded(ref tempTexHandle, descriptor);

                // Allocate the 'blur' temp texture which will contain only the light streaks.
                ResetTarget();

                var settings = VolumeManager.instance.stack.GetComponent<LightStreaksSettings>();
                descriptor = GetMainPassDescriptor(cameraTextureDescriptor, settings.downsampleAmount.value);
                RenderingUtils.ReAllocateIfNeeded(ref blurTexHandle, descriptor);

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

                // Set Light Streaks effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<LightStreaksSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                material.SetInt("_KernelSize", settings.strength.value);
                material.SetFloat("_Spread", settings.strength.value / 7.5f);
                material.SetFloat("_LuminanceThreshold", settings.luminanceThreshold.value);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Light Streaks effect.
                using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
                {
                    Blit(cmd, cameraTargetHandle, tempTexHandle);
                    Blit(cmd, cameraTargetHandle, blurTexHandle, material, 0);

                    material.SetTexture("_BlurTex", blurTexHandle);
                    Blit(cmd, tempTexHandle, cameraTargetHandle, material, 1);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                blurTexHandle?.Release();
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

            private class CompositePassData
            {
                public Material material;
                public TextureHandle inputTexture;
                public TextureHandle blurTexture;
            }

            private static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source)
            {
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, false);
            }

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle source, Material material)
            {
                // Set Light Streaks effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<LightStreaksSettings>();
                material.SetInt("_KernelSize", settings.strength.value);
                material.SetFloat("_Spread", settings.strength.value / 7.5f);
                material.SetFloat("_LuminanceThreshold", settings.luminanceThreshold.value);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            private static void ExecuteCompositePass(RasterCommandBuffer cmd, RTHandle source, RTHandle blurTex, Material material)
            {
                material.SetTexture("_BlurTex", blurTex);
                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 1);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<LightStreaksSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_LightStreaksColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("LightStreaks_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                var mainPassDescriptor = GetMainPassDescriptor(cameraData.cameraTargetDescriptor, settings.downsampleAmount.value);
                TextureHandle blurTexture = TextureHandle.nullHandle;

                blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, mainPassDescriptor, "_LightStreaksBlurTex", false);

                // Perform first main pass (temp -> blur).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("LightStreaks_MainPass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(blurTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(context.cmd, data.inputTexture, data.material));
                }

                // Perform second main pass (blur -> source).
                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("LightStreaks_CompositePass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = copiedColor;
                    passData.blurTexture = blurTexture;

                    builder.UseTexture(copiedColor, AccessFlags.Read);
                    builder.UseTexture(blurTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) => ExecuteCompositePass(context.cmd, data.inputTexture, data.blurTexture, data.material));
                }
            }
#endif
        }
    }
}
