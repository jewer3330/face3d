Shader "Hidden/ARFace/Math/Add"
{
    Properties
    {
        [NoScaleOffset] _Lhs ("lhs", 2D) = "white" {}
        [NoScaleOffset] _Rhs ("rhs", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags { "PreviewType"="Plane" }

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

            sampler2D _Lhs;
            sampler2D _Rhs;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_Lhs, i.uv) + tex2D(_Rhs, i.uv);
            }
            ENDCG
        }
    }
}
