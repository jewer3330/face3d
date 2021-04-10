Shader "Unlit/CenterOffset"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
        _OffsetX("OffsetX", Range(-0.5, 0.5)) = 0
        _OffsetY("OffsetY", Range(-0.5, 0.5)) = 0
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
            float _OffsetX, _OffsetY;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 retCol = tex2D(_MainTex,i.uv+float2(_OffsetX, _OffsetY));
                return retCol;
            }
            ENDCG
        }
    }
}
