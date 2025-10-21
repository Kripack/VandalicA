using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Posterize
{
    [System.Serializable]
    public class PosterizeSettings
    {
        [Header("Posterization Settings")]
        [Range(2, 256)]
        [Tooltip("Number of color levels per channel")]
        public int colorLevels = 8;
        
        [Range(0f, 1f)]
        [Tooltip("Effect intensity (0 = original, 1 = full posterization)")]
        public float intensity = 1f;
        
        [Header("Dithering Settings")]
        [Tooltip("Enable dithering to smooth color banding")]
        public bool enableDithering = true;
        
        [Tooltip("Use colored dithering (separate noise per RGB channel) vs monochrome")]
        public bool coloredDithering = false;
        
        [Range(0f, 1f)]
        [Tooltip("Dithering amount")]
        public float ditheringAmount = 0.5f;
        
        [Range(2, 8)]
        [Tooltip("Bayer matrix size (2, 4, or 8)")]
        public int bayerSize = 4;
        
        [Header("Material")]
        public Material material;
        
        [Header("Render Pass Event")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
}