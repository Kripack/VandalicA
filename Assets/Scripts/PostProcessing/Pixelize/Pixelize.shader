Shader "Custom/Pixelize"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Pixelize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, input.texcoord);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    color = LinearToSRGB(color);
                #endif
                
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Pixelize Blend"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragBlend
            #pragma target 2.0
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_OriginalTex);
            SAMPLER(sampler_OriginalTex);
            
            half _Intensity;

            half4 fragBlend(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 pixelated = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, input.texcoord);
                half4 original = SAMPLE_TEXTURE2D(_OriginalTex, sampler_OriginalTex, input.texcoord);
                
                half4 color = lerp(original, pixelated, _Intensity);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    color = LinearToSRGB(color);
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}