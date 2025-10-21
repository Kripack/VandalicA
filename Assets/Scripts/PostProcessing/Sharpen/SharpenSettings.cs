using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Sharpen
{
    [System.Serializable]
    public class SharpenSettings
    {
        [Header("Sharpen Settings")]
        [Range(0f, 5f)]
        [Tooltip("Sharpening strength (0 = no effect, 1 = normal, >1 = strong)")]
        public float strength = 1f;
        
        [Range(0f, 1f)]
        [Tooltip("Effect intensity for blending with original")]
        public float intensity = 1f;
        
        [Header("Advanced")]
        [Tooltip("Sharpen method")]
        public SharpenMethod method = SharpenMethod.Laplacian;
        
        [Range(0f, 1f)]
        [Tooltip("Clamp excessive sharpening to reduce halos")]
        public float clamp = 0.5f;
        
        [Header("Material")]
        public Material material;
        
        [Header("Render Pass Event")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public enum SharpenMethod
    {
        Laplacian = 0,
        UnsharpMask = 1,
        HighPass = 2
    }
}
