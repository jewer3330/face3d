Shader "Hidden/ImageScaleShader"
{
    Properties
    {
        _InputTex ("InputTexture", 2D) = "white" {}
        _BorderColor ("BorderColor", Color) = (0,0,0,0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Tags {"PreviewType"="Plane"}

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _InputTex;
            float4 _InputTex_ST;
            fixed4 _BorderColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _InputTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_InputTex, i.uv);
                if (i.uv.x >= 0 && i.uv.x <= 1 &&
                    i.uv.y >= 0 && i.uv.y <= 1)
                {
                    return col;
                }
                else
                {
                    return _BorderColor;
                }
            }
            ENDCG
        }
    }
}
