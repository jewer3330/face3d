Shader "Unlit/BlendShader"
{
    Properties
    {
        [NoScaleOffset]_MainTex ("MainTex", 2D) = "white" {}
        [NoScaleOffset]_MainTex1 ("MainTex1", 2D) = "white" {}
        [NoScaleOffset]_MaskTexture ("MaskTexture", 2D) = "white" {}
      
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MainTex1;
            float4 _MainTex1_ST;
            sampler2D _MaskTexture;
            float4 _MaskTexture_ST;
            int _Num;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               
                    fixed4 col = tex2D(_MainTex, i.uv);
                    fixed alpha = tex2D(_MaskTexture, i.uv).a;
                
                    fixed4 col1 = tex2D(_MainTex1, i.uv);
                    return lerp(col1,col,alpha);
            }
            ENDCG
        }
    }
}
