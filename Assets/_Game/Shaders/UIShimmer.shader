Shader "Custom/UIShimmer"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShimmerColor ("Shimmer Color", Color) = (1, 1, 1, 0.6)
        _ShimmerWidth ("Shimmer Width", Range(0.01, 0.5)) = 0.15
        _ShimmerAngle ("Shimmer Angle", Range(-1, 1)) = 0.5
        _ShimmerSpeed ("Shimmer Speed", Range(0.1, 5)) = 1.0
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 2)) = 1.0

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
            float4 _ShimmerColor;
            float _ShimmerWidth;
            float _ShimmerAngle;
            float _ShimmerSpeed;
            float _ShimmerIntensity;

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

                float pos = i.uv.x + i.uv.y * _ShimmerAngle;
                float shimmerPos = frac(_Time.y * _ShimmerSpeed) * (1.0 + _ShimmerWidth * 2.0) - _ShimmerWidth;
                float dist = abs(pos - shimmerPos);
                float shimmer = saturate(1.0 - dist / _ShimmerWidth);
                shimmer = shimmer * shimmer * _ShimmerIntensity;

                tex.rgb += _ShimmerColor.rgb * shimmer * _ShimmerColor.a * tex.a;

                return tex;
            }
            ENDCG
        }
    }
}
