Shader "Unlit/RotateTexture"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Rotate("Rotate",float) = 0
        _CenterX("CenterX",Range(0,1)) = 0.5
        _CenterY("CenterY",Range(0,1)) = 0.5
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Rotate;
            float _CenterX,_CenterY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float2 offset = float2(_CenterX, _CenterY);
                 _Rotate = _Rotate * 3.1415926 / 180;
                float sin_ret = sin(_Rotate);
                float cos_ret = cos(_Rotate);
                float2x2 trans = float2x2(cos_ret,- sin_ret,sin_ret,cos_ret);
                o.uv -= offset;
                o.uv = mul(o.uv,trans) + offset;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 ret = tex2D(_MainTex,i.uv);
                return ret;
            }
            ENDCG
        }
    }
}
