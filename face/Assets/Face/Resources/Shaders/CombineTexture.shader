Shader "Unlit/CombineTexture"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _CombineTex("CombineTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType" = "Plane"}
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

            sampler2D _MainTex, _CombineTex;
            float4 _MainTex_ST, _CombineTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _CombineTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 mCol = tex2D(_MainTex,i.uv);
                float4 cCol = tex2D(_CombineTex, i.uv1);
                float4 retCol = mCol;
                if(mCol.a < 1)
                {
                    retCol = cCol;
                }
                return retCol;
            }
            ENDCG
        }
    }
}
