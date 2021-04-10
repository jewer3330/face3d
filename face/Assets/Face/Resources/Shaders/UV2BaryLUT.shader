Shader "Hidden/UV2BaryLUT"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float faceId : TEXCOORD1;
                float3 baryCoord : TEXCOORD2;
            };

            struct v2f
            {
                float faceId : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 baryCoord : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.faceId = v.faceId;
                o.baryCoord = v.baryCoord;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.baryCoord, i.faceId);
            }
            ENDCG
        }
    }
}
