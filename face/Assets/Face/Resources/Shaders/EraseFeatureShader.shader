Shader "Hidden/EraseFeatureShader"
{
    Properties
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset]_NoFaceTex ("NoFaceTexture", 2D) = "white" {}
        [NoScaleOffset]_MaskTex ("MaskTexture", 2D) = "white" {}

        _R("(R)Eyebrows", float) = 0
        _G("(G)Mouth", float) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex, _NoFaceTex, _MaskTex;
            float _R, _G;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col1 = tex2D(_NoFaceTex, i.uv);
                fixed4 col2 = tex2D(_MaskTex, i.uv);
                fixed temp = col2.r * _R;
                col = lerp(col, col1, temp);
                temp = col2.g * _G;
                col = lerp(col, col1, temp);
                return col;
            }
            ENDCG
        }
    }
}
