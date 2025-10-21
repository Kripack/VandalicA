Shader "Custom/Posterize"
{
    Properties
    {
        _ColorLevels ("Color Levels", Range(2, 256)) = 8
        _Intensity ("Intensity", Range(0, 1)) = 1
        _DitheringAmount ("Dithering Amount", Range(0, 1)) = 0.5
        _BayerSize ("Bayer Size", Int) = 4
        _ColoredDithering ("Colored Dithering", Int) = 0
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
            Name "Posterize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorLevels;
            float _Intensity;
            float _DitheringAmount;
            int _BayerSize;
            int _ColoredDithering;

            float BayerValue(int x, int y)
            {
                const float bayer[64] = {
                    0.0/64.0,  32.0/64.0,  8.0/64.0,  40.0/64.0,  2.0/64.0,  34.0/64.0, 10.0/64.0, 42.0/64.0,
                    48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
                    12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
                    60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
                    3.0/64.0,  35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
                    51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
                    15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
                    63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
                };
                
                int size = _BayerSize;
                int index = (x % size) + (y % size) * size;
                
                if (size == 4)
                    index = ((x % 4) * 2) + ((y % 4) * 2) * 8;
                else if (size == 2)
                    index = ((x % 2) * 4) + ((y % 2) * 4) * 8;
                
                return bayer[index];
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                float2 screenPos = input.texcoord * _ScreenParams.xy;
                int x = int(screenPos.x);
                int y = int(screenPos.y);
                
                float3 bayerRGB;
                
                if (_ColoredDithering > 0)
                {
                    float bayerR = BayerValue(x, y);
                    float bayerG = BayerValue(x + 1, y);
                    float bayerB = BayerValue(x, y + 1);
                    bayerRGB = float3(bayerR, bayerG, bayerB);
                }
                else
                {
                    float bayer = BayerValue(x, y);
                    bayerRGB = float3(bayer, bayer, bayer);
                }
                
                float ditherScale = _DitheringAmount / _ColorLevels;
                half3 dithered = original.rgb + (bayerRGB - 0.5) * ditherScale;
                
                half levelsMinusOne = _ColorLevels - 1.0;
                half3 posterized = floor(saturate(dithered) * levelsMinusOne + 0.5) / levelsMinusOne;
                
                half3 color = lerp(original.rgb, posterized, _Intensity);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    color = LinearToSRGB(color);
                #endif
                
                return half4(color, original.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Copy"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragCopy
            #pragma target 2.0
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 fragCopy(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    color = LinearToSRGB(color);
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}