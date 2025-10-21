using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Sharpen
{
    public class SharpenRendererFeature : ScriptableRendererFeature
    {
        public SharpenSettings settings = new SharpenSettings();
        private SharpenPass m_Pass;

        public override void Create()
        {
            if (settings.material == null)
            {
                Debug.LogWarning("SharpenRendererFeature: Material is not assigned!");
                return;
            }

            m_Pass = new SharpenPass(settings);
            m_Pass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null || m_Pass == null)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(m_Pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            m_Pass = null;
        }
    }
}
