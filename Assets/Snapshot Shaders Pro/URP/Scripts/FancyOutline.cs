using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class FancyOutline : ScriptableRendererFeature
    {
        FancyOutlineRenderPass pass;

        public override void Create()
        {
            pass = new FancyOutlineRenderPass();
            name = "Fancy Outline";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<FancyOutlineSettings>();

            if (settings != null && settings.IsActive())
            {
                pass.ConfigureInput(ScriptableRenderPassInput.Depth);
                pass.ConfigureInput(ScriptableRenderPassInput.Normal);
                renderer.EnqueuePass(pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pass.Dispose();
            base.Dispose(disposing);
        }

        class FancyOutlineRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            public FancyOutlineRenderPass()
            {
                profilingSampler = new ProfilingSampler("Outlines");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/Outline");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Outline\".");
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

                // Set Fancy Outline effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<FancyOutlineSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                if (settings.useSceneColor.value)
                {
                    material.EnableKeyword("USE_SCENE_TEXTURE_ON");
                }
                else
                {
                    material.DisableKeyword("USE_SCENE_TEXTURE_ON");
                    material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                }

                material.SetColor("_OutlineColor", settings.outlineColor.value);
                material.SetFloat("_ColorSensitivity", settings.colorSensitivity.value);
                material.SetFloat("_ColorStrength", settings.colorStrength.value);
                material.SetFloat("_DepthSensitivity", settings.depthSensitivity.value);
                material.SetFloat("_DepthStrength", settings.depthStrength.value);
                material.SetFloat("_NormalsSensitivity", settings.normalSensitivity.value);
                material.SetFloat("_NormalsStrength", settings.normalStrength.value);
                material.SetFloat("_DepthThreshold", settings.depthThreshold.value);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Colorize effect.
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
                // Set Fancy Outline effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<FancyOutlineSettings>();
                if (settings.useSceneColor.value)
                {
                    material.EnableKeyword("USE_SCENE_TEXTURE_ON");
                }
                else
                {
                    material.DisableKeyword("USE_SCENE_TEXTURE_ON");
                    material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                }

                material.SetColor("_OutlineColor", settings.outlineColor.value);
                material.SetFloat("_ColorSensitivity", settings.colorSensitivity.value);
                material.SetFloat("_ColorStrength", settings.colorStrength.value);
                material.SetFloat("_DepthSensitivity", settings.depthSensitivity.value);
                material.SetFloat("_DepthStrength", settings.depthStrength.value);
                material.SetFloat("_NormalsSensitivity", settings.normalSensitivity.value);
                material.SetFloat("_NormalsStrength", settings.normalStrength.value);
                material.SetFloat("_DepthThreshold", settings.depthThreshold.value);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<FancyOutlineSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_FancyOutlineColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("FancyOutline_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("FancyOutline_MainPass", out var passData, profilingSampler))
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
