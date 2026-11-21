Shader "Hidden/Gameplay/Rendering/DualKawaseBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        // Core.hlsl defines the TEXTURE2D_X/SAMPLE_TEXTURE2D_X macros used by Blit.hlsl.
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _BlitTexture_TexelSize;
        float _DualKawaseBlurRadius;
        float _DualKawaseBlurIntensity;

        half4 DualKawaseDownsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 offset = _BlitTexture_TexelSize.xy * _DualKawaseBlurRadius;
            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-offset.x, -offset.y));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(offset.x, -offset.y));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-offset.x, offset.y));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(offset.x, offset.y));
            return color * 0.25h;
        }

        half4 SampleDualKawaseUpsample(float2 uv)
        {
            float2 offset = _BlitTexture_TexelSize.xy * _DualKawaseBlurRadius;
            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-2.0 * offset.x, 0.0));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, offset.y)) * 2.0h;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, 2.0 * offset.y));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, offset.y)) * 2.0h;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(2.0 * offset.x, 0.0));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, -offset.y)) * 2.0h;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, -2.0 * offset.y));
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, -offset.y)) * 2.0h;
            return color / 12.0h;
        }

        half4 DualKawaseUpsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return SampleDualKawaseUpsample(input.texcoord);
        }

        half4 DualKawaseComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 color = SampleDualKawaseUpsample(input.texcoord).rgb;
            return half4(color, saturate(_DualKawaseBlurIntensity));
        }
        ENDHLSL

        Pass
        {
            Name "DualKawaseDownsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseDownsample
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseUpsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseUpsample
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseComposite"
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseComposite
            ENDHLSL
        }
    }
}
