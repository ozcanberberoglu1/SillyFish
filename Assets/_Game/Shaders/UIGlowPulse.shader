Shader "Custom/UIGlowPulse"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1, 0.8, 0.2, 1)
        _GlowSpeed ("Glow Speed", Range(0.1, 5)) = 1.5
        _GlowMin ("Glow Min", Range(0, 1)) = 0.0
        _GlowMax ("Glow Max", Range(0, 1)) = 0.5
        _GlowSize ("Glow Edge Size", Range(0, 0.5)) = 0.15

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float4 _Color;
            float4 _GlowColor;
            float _GlowSpeed;
            float _GlowMin;
            float _GlowMax;
            float _GlowSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color * i.color;

                float2 center = i.uv - 0.5;
                float dist = length(center);
                float edge = smoothstep(0.5 - _GlowSize, 0.5, dist);

                float pulse = sin(_Time.y * _GlowSpeed) * 0.5 + 0.5;
                float glowStrength = lerp(_GlowMin, _GlowMax, pulse);

                float glow = edge * glowStrength;
                tex.rgb = lerp(tex.rgb, _GlowColor.rgb, glow * tex.a);
                tex.rgb += _GlowColor.rgb * glowStrength * 0.1 * tex.a;

                return tex;
            }
            ENDCG
        }
    }
}
