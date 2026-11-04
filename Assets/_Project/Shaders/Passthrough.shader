// The passthrough screen: the camera image on a quad in front of the AR camera. It's the background,
// so it draws first, ignores depth and writes none, and everything virtual draws over it wherever
// it is. The render state is fixed here rather than in material properties, which URP resets from
// the surface type. It has only a forward pass: a depth-only pass would put the quad in URP's depth
// prepass and cut through virtual things behind it.
Shader "CardboardXR/Passthrough"
{
    Properties
    {
        [MainTexture] _BaseMap ("Camera Image", 2D) = "black" {}
        _Zoom ("Zoom", Float) = 1
        _Pan ("Pan", Vector) = (0, 0, 0, 0)
        _Brightness ("Brightness", Float) = 1
        _Contrast ("Contrast", Float) = 1
        _Saturation ("Saturation", Float) = 1
        _Opacity ("Opacity", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Background"
        }

        Pass
        {
            Name "Passthrough"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Zoom;
                float4 _Pan;
                float _Brightness;
                float _Contrast;
                float _Saturation;
                float _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = (input.uv - 0.5) / max(_Zoom, 0.01) + 0.5 + _Pan.xy;
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half luma = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(luma.xxx, color, _Saturation);
                color = (color - 0.5) * _Contrast + 0.5;
                color *= _Brightness;
                return half4(saturate(color), _Opacity);
            }
            ENDHLSL
        }
    }
}
