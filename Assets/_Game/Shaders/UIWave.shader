Shader "Custom/UIWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _WaveSpeed ("Wave Speed", Range(0.1, 10)) = 2
        _WaveAmountX ("Wave Amount X", Range(0, 0.05)) = 0.01
        _WaveAmountY ("Wave Amount Y", Range(0, 0.05)) = 0.008
        _WaveFreqX ("Wave Frequency X", Range(0.5, 20)) = 3
        _WaveFreqY ("Wave Frequency Y", Range(0.5, 20)) = 2.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _WaveSpeed;
                half _WaveAmountX;
                half _WaveAmountY;
                half _WaveFreqX;
                half _WaveFreqY;
            CBUFFER_END

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 waveUV = i.uv;
                waveUV.x += sin(t + i.uv.y * _WaveFreqX * 6.2832) * _WaveAmountX;
                waveUV.y += cos(t * 0.7 + i.uv.x * _WaveFreqY * 6.2832) * _WaveAmountY;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, waveUV);
                tex *= i.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip(tex.a - 0.001);
                #endif

                return tex;
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}
