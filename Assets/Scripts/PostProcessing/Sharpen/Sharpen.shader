Shader "Custom/Sharpen"
{
    Properties
    {
        _Strength ("Strength", Float) = 1
        _Intensity ("Intensity", Range(0, 1)) = 1
        _Clamp ("Clamp", Range(0, 1)) = 0.5
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        half _Strength;
        half _Intensity;
        half _Clamp;
        float2 _TexelSize;

        struct SharpenData
        {
            half3 original;
            half3 sharpened;
        };

        half3 Sample9Point(float2 uv)
        {
            half3 color = 0;
            
            color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
            
            return color;
        }

        half3 SharpenLaplacian(float2 uv, half3 original)
        {
            half3 sum = 0;
            
            half3 center = original * 9.0;
            
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-1, -1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 0, -1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 1, -1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-1,  0) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 1,  0) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-1,  1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 0,  1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 1,  1) * _TexelSize).rgb;
            
            return center - sum;
        }

        half3 SharpenUnsharpMask(float2 uv, half3 original)
        {
            half3 blur = 0;
            const half weights[5] = { 0.06136, 0.24477, 0.38774, 0.24477, 0.06136 };
            
            for(int x = -2; x <= 2; x++)
            {
                float2 offset = float2(x, 0) * _TexelSize;
                blur += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb * weights[x + 2];
            }
            
            half3 finalBlur = 0;
            for(int y = -1; y <= 1; y++)
            {
                float2 offset = float2(0, y) * _TexelSize;
                finalBlur += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb;
            }
            finalBlur /= 3.0;
            
            return original - finalBlur;
        }

        half3 SharpenHighPass(float2 uv, half3 original)
        {
            half3 center = original * 5.0;
            half3 sum = 0;
            
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 0, -1) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-1,  0) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 1,  0) * _TexelSize).rgb;
            sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( 0,  1) * _TexelSize).rgb;
            
            return center - sum;
        }

        half3 ClampSharpen(half3 original, half3 sharpened, half clampValue)
        {
            half3 delta = sharpened - original;
            half maxDelta = clampValue;
            delta = clamp(delta, -maxDelta, maxDelta);
            return original + delta;
        }

        ENDHLSL

        Pass
        {
            Name "Sharpen Laplacian"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half3 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                half3 sharpenDetail = SharpenLaplacian(input.texcoord, original);
                
                half3 sharpened = original + sharpenDetail * _Strength;
                sharpened = ClampSharpen(original, sharpened, _Clamp);
                
                half3 finalColor = lerp(original, sharpened, _Intensity);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    finalColor = LinearToSRGB(finalColor);
                #endif
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Sharpen Unsharp Mask"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half3 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                half3 sharpenDetail = SharpenUnsharpMask(input.texcoord, original);
                
                half3 sharpened = original + sharpenDetail * _Strength;
                sharpened = ClampSharpen(original, sharpened, _Clamp);
                
                half3 finalColor = lerp(original, sharpened, _Intensity);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    finalColor = LinearToSRGB(finalColor);
                #endif
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Sharpen High Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half3 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                half3 sharpenDetail = SharpenHighPass(input.texcoord, original);
                
                half3 sharpened = original + sharpenDetail * _Strength;
                sharpened = ClampSharpen(original, sharpened, _Clamp);
                
                half3 finalColor = lerp(original, sharpened, _Intensity);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    finalColor = LinearToSRGB(finalColor);
                #endif
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Copy"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    color.rgb = LinearToSRGB(color.rgb);
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}
