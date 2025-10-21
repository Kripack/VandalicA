using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Pixelize
{
    [System.Serializable]
    public class PixelizationSettings
    {
        [Header("Pixelization Settings")]
        [Range(1, 512)]
        public int pixelSize = 4;
        
        [Range(0f, 1f)]
        public float intensity = 1f;
        
        [Header("Material")]
        public Material material;
        
        [Header("Render Pass Event")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
}