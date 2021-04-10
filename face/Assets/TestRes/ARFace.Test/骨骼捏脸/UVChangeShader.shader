Shader "Unlit/UVChangeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UV2Tex ("Texture", 2D) = "white" {}
        _Num("Num",int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _UV2Tex;
            float4 _UV2Tex_ST;
            int _Num;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _UV2Tex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                if(_Num == 0)
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                else
                {
                    fixed4 col = tex2D(_UV2Tex, i.uv1);
                    return col;
                }
            }
            ENDCG
        }
    }
}
