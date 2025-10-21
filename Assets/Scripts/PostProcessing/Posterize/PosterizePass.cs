using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Posterize
{
    public class PosterizePass : ScriptableRenderPass
    {
        private PosterizeSettings m_Settings;
        private Material m_Material;
        private const string k_PassName = "Posterize Pass";
        private static readonly int s_ColorLevelsID = Shader.PropertyToID("_ColorLevels");
        private static readonly int s_IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int s_DitheringAmountID = Shader.PropertyToID("_DitheringAmount");
        private static readonly int s_BayerSizeID = Shader.PropertyToID("_BayerSize");

        private class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal int colorLevels;
            internal float intensity;
            internal bool enableDithering;
            internal float ditheringAmount;
            internal int bayerSize;
        }

        public PosterizePass(PosterizeSettings settings)
        {
            m_Settings = settings;
            m_Material = settings.material;
            profilingSampler = new ProfilingSampler(k_PassName);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle source = resourceData.activeColorTexture;

            if (!source.IsValid()) return;

            if (m_Settings.intensity <= 0.001f) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "PosterizeOutput", false);

            using (var builder =
                   renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData, profilingSampler))
            {
                passData.source = source;
                passData.material = m_Material;
                passData.colorLevels = m_Settings.colorLevels;
                passData.intensity = m_Settings.intensity;
                passData.enableDithering = m_Settings.enableDithering;
                passData.ditheringAmount = m_Settings.ditheringAmount;
                passData.bayerSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(m_Settings.bayerSize, 2, 8));

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat(s_ColorLevelsID, data.colorLevels);
                    data.material.SetFloat(s_IntensityID, data.intensity);
                    data.material.SetFloat(s_DitheringAmountID, data.enableDithering ? data.ditheringAmount : 0f);
                    data.material.SetInt(s_BayerSizeID, data.bayerSize);

                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder =
                   renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Copy Back", out var passData,
                       profilingSampler))
            {
                passData.source = destination;
                passData.material = m_Material;

                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }
        }

        public void Dispose()
        {
            m_Material = null;
        }
    }
}