using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
    using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace SnapshotShaders.URP
{
    public class TextAdventure : ScriptableRendererFeature
    {
        TextAdventureRenderPass pass;

        public override void Create()
        {
            pass = new TextAdventureRenderPass();
            name = "Text Adventure";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<TextAdventureSettings>();

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

        class TextAdventureRenderPass : ScriptableRenderPass
        {
            private Material material;
            private RTHandle tempTexHandle;

            private Vector2Int pixelSize;

            public TextAdventureRenderPass()
            {
                profilingSampler = new ProfilingSampler("TextAdventure");

#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            private void CreateMaterial()
            {
                var shader = Shader.Find("SnapshotProURP/TextAdventure");

                if (shader == null)
                {
                    Debug.LogError("Cannot find shader: \"SnapshotProURP/TextAdventure\".");
                    return;
                }

                material = new Material(shader);
            }

            private static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = (int)DepthBits.None;

                var settings = VolumeManager.instance.stack.GetComponent<TextAdventureSettings>();
                float size = settings.characterSize.value;
                float aspect = (float)Screen.height / Screen.width;
                var pixelSize = new Vector2Int(Mathf.CeilToInt((Screen.width) / size),
                    Mathf.CeilToInt(Screen.height / size));

                descriptor.width = pixelSize.x;
                descriptor.height = pixelSize.y;

                return descriptor;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ResetTarget();

                var descriptor = GetCopyPassDescriptor(cameraTextureDescriptor);
                pixelSize = new Vector2Int(descriptor.width, descriptor.height);
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

                // Set Text Adventure effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<TextAdventureSettings>();
                renderPassEvent = settings.renderPassEvent.value;
                material.SetTexture("_CharacterAtlas", settings.characterAtlas.value);
                material.SetInteger("_CharacterCount", settings.characterCount.value);
                material.SetVector("_CharacterSize", (Vector2)pixelSize);
                material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                material.SetColor("_CharacterColor", settings.characterColor.value);

                RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Perform the Blit operations for the Text Adventure effect.
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

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle source, Material material, Vector2Int pixelSize)
            {
                // Set Text Adventure effect properties.
                var settings = VolumeManager.instance.stack.GetComponent<TextAdventureSettings>();
                material.SetTexture("_CharacterAtlas", settings.characterAtlas.value);
                material.SetInteger("_CharacterCount", settings.characterCount.value);
                material.SetVector("_CharacterSize", (Vector2)pixelSize);
                material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                material.SetColor("_CharacterColor", settings.characterColor.value);

                Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), material, 0);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if(material == null)
                {
                    CreateMaterial();
                }

                var settings = VolumeManager.instance.stack.GetComponent<TextAdventureSettings>();
                renderPassEvent = settings.renderPassEvent.value;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
                pixelSize = new Vector2Int(colorCopyDescriptor.width, colorCopyDescriptor.height);
                TextureHandle copiedColor = TextureHandle.nullHandle;

                // Perform the intermediate copy pass (source -> temp).
                copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_TextAdventureColorCopy", false, filterMode: FilterMode.Point);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("TextAdventure_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture));
                }

                // Perform main pass (temp -> source).
                using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("TextAdventure_MainPass", out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.inputTexture = copiedColor;

                    builder.UseTexture(copiedColor, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(context.cmd, data.inputTexture, data.material, pixelSize));
                }
            }

#endif
        }
    }
}
