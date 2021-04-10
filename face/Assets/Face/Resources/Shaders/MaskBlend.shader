Shader "Hidden/MaskBlend"
{
    Properties
    {
        _ForegroundTex ("Foreground", 2D) = "white" {}
        _BackgroundTex ("Background", 2D) = "black" {}
        [NoScaleOffset] _MaskTex ("Mask (Alpha)", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1
        _SrcBlend ("Src Blend Mode", Int) = 5
        _DstBlend ("Dst Blend Mode", Int) = 10
    }
    SubShader
    {
        Tags { "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always
        Blend One Zero

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_ForegroundTex : TEXCOORD1;
                float2 uv_BackgroundTex : TEXCOORD2;
            };

            sampler2D _ForegroundTex;
            sampler2D _BackgroundTex;
            sampler2D _MaskTex;
            float4 _ForegroundTex_ST;
            float4 _BackgroundTex_ST;
            float _Alpha;
            int _SrcBlend;
            int _DstBlend;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv_ForegroundTex = TRANSFORM_TEX(v.uv, _ForegroundTex);
                o.uv_BackgroundTex = TRANSFORM_TEX(v.uv, _BackgroundTex);
                return o;
            }
            
            float4 applyBlend(float4 c, int mode, float4 src, float4 dest)
            {
                if (mode == 0) //Zero
                    return 0;
                else if (mode == 1) //One
                    return c;
                else if (mode == 5) //SrcAlpha
                    return c * src.a;
                else if (mode == 10) //OneMinusDstAlpha
                    return c * (1 - src.a);
                else
                    return float4(1, 0, 1, 1) / 2;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fg = tex2D(_ForegroundTex, i.uv_ForegroundTex);
                fixed4 bg = tex2D(_BackgroundTex, i.uv_BackgroundTex);
                fixed mask = tex2D(_MaskTex, i.uv).a;
                float a = mask * fg.a * _Alpha;
                
                float4 src = float4(fg.rgb, a);
                float4 dst = bg;
                float4 c =
                    applyBlend(src, _SrcBlend, src, dst) +
                    applyBlend(dst, _DstBlend, src, dst);
                return c;
            }
            ENDCG
        }
    }
}