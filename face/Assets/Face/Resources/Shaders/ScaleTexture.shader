Shader "Unlit/ScaleTexture"
{
    Properties
    {
        [NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
        _CenterX("CenterX", Range(0, 1)) = 0.5
        _CenterY("CenterY", Range(0, 1)) = 0.5
        _Scale("Scale", Range(-1, 1)) = 0
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
            float _CenterX, _CenterY, _Scale;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center=float2(_CenterX,_CenterY);
                float2 distance= center - i.uv;
                float x=center.x+ center.x*(-distance.x/center.x) *(1-_Scale);
                float y=center.y+ center.y*(-distance.y/center.y) *(1-_Scale);
                float2 uv = float2(x,y);
                return tex2D(_MainTex, uv);  
            }
            ENDCG
        }
    }
}
