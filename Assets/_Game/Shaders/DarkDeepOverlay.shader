Shader "Custom/DarkDeepOverlay"
{
    Properties
    {
        _DarkColor ("Dark Color", Color) = (0, 0.01, 0.03, 1)
        _NoiseScale ("Noise Scale", Float) = 2.5
        _NoiseSpeed ("Noise Speed", Float) = 0.08
        _DarkMin ("Min Darkness (bright spots)", Range(0, 1)) = 0.55
        _DarkMax ("Max Darkness (dark spots)", Range(0, 1)) = 0.97
        _Contrast ("Contrast", Range(1, 5)) = 1.0
        _Opacity ("Overall Opacity", Range(0, 1)) = 1.0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _DarkColor;
            float _NoiseScale;
            float _NoiseSpeed;
            float _DarkMin;
            float _DarkMax;
            float _Contrast;
            float _Opacity;

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float val = 0.0;
                float amp = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 5; i++)
                {
                    val += amp * noise(p);
                    p = p * 2.1 + shift;
                    amp *= 0.5;
                }
                return val;
            }

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
                float t = _Time.y * _NoiseSpeed;
                float2 uv = i.uv * _NoiseScale;

                float n1 = fbm(uv + float2(t * 0.6, t * 0.3));
                float n2 = fbm(uv * 1.4 + float2(-t * 0.3, t * 0.5) + 7.0);
                float n3 = fbm(uv * 0.7 + float2(t * 0.2, -t * 0.4) + 13.0);

                float combined = (n1 + n2 + n3) / 3.0;
                combined = saturate(pow(combined, 1.0 / _Contrast) * _Contrast + (1.0 - _Contrast) * 0.5);

                float darkness = lerp(_DarkMin, _DarkMax, combined);

                fixed4 col = _DarkColor;
                col.a = darkness * _Opacity * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
