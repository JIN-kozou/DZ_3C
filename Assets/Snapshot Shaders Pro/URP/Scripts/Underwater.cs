using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class Underwater : ScriptableRendererFeature
    {
        UnderwaterRenderPass pass;

        public override void Create()
        {
            pass = new UnderwaterRenderPass();
            name = "Underwater";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();

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

        class UnderwaterRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            public UnderwaterRenderPass()
            {
                profilingSampler = new ProfilingSampler("Underwater");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }
            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/Underwater");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Underwater\".");
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

                // Set Underwater effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                material.SetTexture("_BumpMap", settings.bumpMap.value);
                material.SetFloat("_Strength", settings.strength.value);
                material.SetColor("_WaterColor", settings.waterFogColor.value);
                material.SetFloat("_FogStrength", settings.fogStrength.value);

                if (settings.useCaustics.value)
                {
                    material.EnableKeyword("USE_CAUSTICS_ON");
                }
                else
                {
                    material.DisableKeyword("USE_CAUSTICS_ON");
                } 

                material.SetTexture("_CausticsTexture", settings.causticsTexture.value);
                material.SetFloat("_CausticsNoiseSpeed", settings.causticsNoiseSpeed.value);
                material.SetFloat("_CausticsNoiseScale", settings.causticsNoiseScale.value);
                material.SetFloat("_CausticsNoiseStrength", settings.causticsNoiseStrength.value);
                material.SetVector("_CausticsScrollVelocity1", settings.causticsScrollVelocity1.value);
                material.SetVector("_CausticsScrollVelocity2", settings.causticsScrollVelocity2.value);
                material.SetVector("_CausticsTiling", settings.causticsTiling.value);
                material.SetColor("_CausticsTint", settings.causticsTint.value);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Underwater effect.
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
                // Set Underwater effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();
                material.SetTexture("_BumpMap", settings.bumpMap.value);
                material.SetFloat("_Strength", settings.strength.value);
                material.SetColor("_WaterColor", settings.waterFogColor.value);
                material.SetFloat("_FogStrength", settings.fogStrength.value);

                if (settings.useCaustics.value)
                {
                    material.EnableKeyword("USE_CAUSTICS_ON");
                }
                else
                {
                    material.DisableKeyword("USE_CAUSTICS_ON");
                }

                material.SetTexture("_CausticsTexture", settings.causticsTexture.value);
                material.SetFloat("_CausticsNoiseSpeed", settings.causticsNoiseSpeed.value);
                material.SetFloat("_CausticsNoiseScale", settings.causticsNoiseScale.value);
                material.SetFloat("_CausticsNoiseStrength", settings.causticsNoiseStrength.value);
                material.SetVector("_CausticsScrollVelocity1", settings.causticsScrollVelocity1.value);
                material.SetVector("_CausticsScrollVelocity2", settings.causticsScrollVelocity2.value);
                material.SetVector("_CausticsTiling", settings.causticsTiling.value);
                material.SetColor("_CausticsTint", settings.causticsTint.value);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_UnderwaterColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Underwater_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("Underwater_MainPass", out var passData, profilingSampler))
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
