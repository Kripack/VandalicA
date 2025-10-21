using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Sharpen
{
    public class SharpenPass : ScriptableRenderPass
    {
        private SharpenSettings m_Settings;
        private Material m_Material;
        private const string k_PassName = "Sharpen Pass";
        
        private static readonly int s_StrengthID = Shader.PropertyToID("_Strength");
        private static readonly int s_IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int s_ClampID = Shader.PropertyToID("_Clamp");
        private static readonly int s_TexelSizeID = Shader.PropertyToID("_TexelSize");

        class PassData
        {
            internal TextureHandle source;
            internal Material material;
            internal float strength;
            internal float intensity;
            internal float clamp;
            internal int method;
            internal Vector2 texelSize;
        }

        public SharpenPass(SharpenSettings settings)
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

            if (m_Settings.intensity <= 0.001f || m_Settings.strength <= 0.001f)
                return;

            int screenWidth = cameraData.cameraTargetDescriptor.width;
            int screenHeight = cameraData.cameraTargetDescriptor.height;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "SharpenOutput", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData, profilingSampler))
            {
                passData.source = source;
                passData.material = m_Material;
                passData.strength = m_Settings.strength;
                passData.intensity = m_Settings.intensity;
                passData.clamp = m_Settings.clamp;
                passData.method = (int)m_Settings.method;
                passData.texelSize = new Vector2(1f / screenWidth, 1f / screenHeight);

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat(s_StrengthID, data.strength);
                    data.material.SetFloat(s_IntensityID, data.intensity);
                    data.material.SetFloat(s_ClampID, data.clamp);
                    data.material.SetVector(s_TexelSizeID, data.texelSize);
                    
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                        data.material, data.method);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName + " Copy Back", out var passData, profilingSampler))
            {
                passData.source = destination;
                passData.material = m_Material;

                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                        data.material, 3);
                });
            }
        }

        public void Dispose()
        {
            m_Material = null;
        }
    }
}
