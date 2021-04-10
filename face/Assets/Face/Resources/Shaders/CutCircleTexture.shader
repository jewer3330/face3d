Shader "Unlit/CutCircleTexture"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
        _CenterX("CenterX", Range(0, 1)) = 0.5
        _CenterY("CenterY", Range(0, 1)) = 0.5
        _Radius("Radius", Range(0, 1)) = 1
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
            float _CenterX, _CenterY, _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 mCol = tex2D(_MainTex,i.uv);
                float4 retCol = mCol;
                float2 pos = i.uv-float2(_CenterX, _CenterY);
                if(pos.x*pos.x+pos.y*pos.y > _Radius*_Radius)
                {
                    retCol =float4(0,0,0,0);
                }
                return retCol;
            }
            ENDCG
        }
    }
}
