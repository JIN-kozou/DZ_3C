using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class Cutout : ScriptableRendererFeature
    {
        CutoutRenderPass pass;

        public override void Create()
        {
            pass = new CutoutRenderPass();
            name = "Cutout";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<CutoutSettings>();

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

        class CutoutRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            public CutoutRenderPass()
            {
                profilingSampler = new ProfilingSampler("Cutout");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/Cutout");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/Cutout\".");
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

                // Set Cutout effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<CutoutSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                Matrix4x4 rotationMatrix = Matrix4x4.identity;
                rotationMatrix[0, 0] = rotationMatrix[1, 1] = Mathf.Cos(settings.rotation.value * Mathf.Deg2Rad);
                rotationMatrix[0, 1] = Mathf.Sin(settings.rotation.value * Mathf.Deg2Rad);
                rotationMatrix[1, 0] = -rotationMatrix[0, 1];

                material.SetTexture("_CutoutTex", settings.cutoutTexture.value);
                material.SetColor("_BorderColor", settings.borderColor.value);
                material.SetInt("_Stretch", settings.stretch.value ? 1 : 0);
                material.SetFloat("_Zoom", settings.zoom.value);
                material.SetVector("_Offset", settings.offset.value);
                material.SetMatrix("_Rotation", rotationMatrix);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Cutout effect.
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
                // Set Cutout effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<CutoutSettings>();

                Matrix4x4 rotationMatrix = Matrix4x4.identity;
                rotationMatrix[0, 0] = rotationMatrix[1, 1] = Mathf.Cos(settings.rotation.value * Mathf.Deg2Rad);
                rotationMatrix[0, 1] = Mathf.Sin(settings.rotation.value * Mathf.Deg2Rad);
                rotationMatrix[1, 0] = -rotationMatrix[0, 1];

                material.SetTexture("_CutoutTex", settings.cutoutTexture.value);
                material.SetColor("_BorderColor", settings.borderColor.value);
                material.SetInt("_Stretch", settings.stretch.value ? 1 : 0);
                material.SetFloat("_Zoom", settings.zoom.value);
                material.SetVector("_Offset", settings.offset.value);
                material.SetMatrix("_Rotation", rotationMatrix);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<CutoutSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_CutoutColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Cutout_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("Cutout_MainPass", out var passData, profilingSampler))
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
