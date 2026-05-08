using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class Synthwave : ScriptableRendererFeature
    {
        SynthwaveRenderPass pass;

        public override void Create()
        {
            pass = new SynthwaveRenderPass();
            name = "Synthwave";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();

            if (settings != null && settings.IsActive())
            {
                pass.ConfigureInput(ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            pass.Dispose();
            base.Dispose(disposing);
        }

        class SynthwaveRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            private static Dictionary<AxisMask, Vector3> axisMasks = new Dictionary<AxisMask, Vector3>
            {
                { AxisMask.XY, new Vector3(1.0f, 1.0f, 0.0f) },
                { AxisMask.XZ, new Vector3(1.0f, 0.0f, 1.0f) },
                { AxisMask.YZ, new Vector3(0.0f, 1.0f, 1.0f) },
                { AxisMask.XYZ, new Vector3(1.0f, 1.0f, 1.0f) }
            };

            public SynthwaveRenderPass()
            {
                profilingSampler = new ProfilingSampler("Synthwave");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/Synthwave");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Synthwave\".");
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

                // Set Synthwave effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                if (settings.useSceneColor.value == true)
                {
                    material.EnableKeyword("USE_SCENE_TEXTURE_ON");
                }
                else
                {
                    material.DisableKeyword("USE_SCENE_TEXTURE_ON");
                    material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                }

                material.SetColor("_LineColor1", settings.lineColor1.value);
                material.SetColor("_LineColor2", settings.lineColor2.value);
                material.SetFloat("_LineColorMix", settings.lineColorMix.value);
                material.SetFloat("_LineWidth", settings.lineWidth.value);
                material.SetFloat("_LineFalloff", settings.lineFalloff.value);
                material.SetVector("_GapWidth", settings.gapWidth.value);
                material.SetVector("_Offset", settings.offset.value);
                material.SetVector("_AxisMask", axisMasks[settings.axisMask.value]);

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
                // Set Synthwave effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();
                if (settings.useSceneColor.value == true)
                {
                    material.EnableKeyword("USE_SCENE_TEXTURE_ON");
                }
                else
                {
                    material.DisableKeyword("USE_SCENE_TEXTURE_ON");
                    material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                }

                material.SetColor("_LineColor1", settings.lineColor1.value);
                material.SetColor("_LineColor2", settings.lineColor2.value);
                material.SetFloat("_LineColorMix", settings.lineColorMix.value);
                material.SetFloat("_LineWidth", settings.lineWidth.value);
                material.SetFloat("_LineFalloff", settings.lineFalloff.value);
                material.SetVector("_GapWidth", settings.gapWidth.value);
                material.SetVector("_Offset", settings.offset.value);
                material.SetVector("_AxisMask", axisMasks[settings.axisMask.value]);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_SynthwaveColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Synthwave_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("Synthwave_MainPass", out var passData, profilingSampler))
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
