    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.RenderGraphModule;
    using UnityEngine.Rendering.Universal;

    namespace PostProcessing.Pixelize
    {
        public class PixelizePass : ScriptableRenderPass
        {
            PixelizationSettings m_Settings;
            Material m_Material;
            const string k_PassName = "Pixelize Pass";
            static readonly int s_IntensityID = Shader.PropertyToID("_Intensity");

            class PassData
            {
                internal TextureHandle source;
                internal TextureHandle destination;
                internal Material material;
                internal int downscaleWidth;
                internal int downscaleHeight;
                internal float intensity;
            }

            public PixelizePass(PixelizationSettings settings)
            {
                m_Settings = settings;
                m_Material = settings.material;
                profilingSampler = new ProfilingSampler(k_PassName);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (m_Material == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
            
                if (!source.IsValid())
                    return;

                int screenWidth = cameraData.cameraTargetDescriptor.width;
                int screenHeight = cameraData.cameraTargetDescriptor.height;
            
                int downscaleWidth = Mathf.Max(1, screenWidth / m_Settings.pixelSize);
                int downscaleHeight = Mathf.Max(1, screenHeight / m_Settings.pixelSize);

                RenderTextureDescriptor tempDesc = cameraData.cameraTargetDescriptor;
                tempDesc.width = downscaleWidth;
                tempDesc.height = downscaleHeight;
                tempDesc.depthBufferBits = 0;
                tempDesc.msaaSamples = 1;

                TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, tempDesc, "PixelizeTemp", false, FilterMode.Point);

                RenderTextureDescriptor outputDesc = cameraData.cameraTargetDescriptor;
                outputDesc.depthBufferBits = 0;
                outputDesc.msaaSamples = 1;

                TextureHandle upscaledTexture = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, outputDesc, "PixelizeUpscaled", false, FilterMode.Point);

                bool needBlending = m_Settings.intensity < 0.999f;
                TextureHandle originalCopy = default;

                if (needBlending)
                {
                    originalCopy = UniversalRenderer.CreateRenderGraphTexture(
                        renderGraph, outputDesc, "PixelizeOriginal", false, FilterMode.Bilinear);

                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Copy Original", out var passData, profilingSampler))
                    {
                        passData.source = source;
                        passData.destination = originalCopy;
                        passData.material = m_Material;

                        builder.UseTexture(source, AccessFlags.Read);
                        builder.SetRenderAttachment(originalCopy, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                                data.material, 0);
                        });
                    }
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Downscale", out var passData, profilingSampler))
                {
                    passData.source = source;
                    passData.destination = tempTexture;
                    passData.material = m_Material;
                    passData.downscaleWidth = downscaleWidth;
                    passData.downscaleHeight = downscaleHeight;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                            data.material, 0);
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Upscale", out var passData, profilingSampler))
                {
                    passData.source = tempTexture;
                    passData.destination = upscaledTexture;
                    passData.material = m_Material;

                    builder.UseTexture(tempTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(upscaledTexture, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                            data.material, 0);
                    });
                }

                if (needBlending)
                {
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Blend", out var passData, profilingSampler))
                    {
                        passData.source = upscaledTexture;
                        passData.destination = source;
                        passData.material = m_Material;
                        passData.intensity = m_Settings.intensity;

                        builder.UseTexture(upscaledTexture, AccessFlags.Read);
                        builder.UseTexture(originalCopy, AccessFlags.Read);
                        builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            data.material.SetFloat(s_IntensityID, data.intensity);
                            data.material.SetTexture("_OriginalTex", originalCopy);
                            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                                data.material, 1);
                        });
                    }
                }
                else
                {
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Final", out var passData, profilingSampler))
                    {
                        passData.source = upscaledTexture;
                        passData.destination = source;
                        passData.material = m_Material;

                        builder.UseTexture(upscaledTexture, AccessFlags.Read);
                        builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                                data.material, 0);
                        });
                    }
                }
            }

            public void Dispose()
            {
                m_Material = null;
            }
        }
    }