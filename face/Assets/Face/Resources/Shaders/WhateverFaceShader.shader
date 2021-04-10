Shader "Unlit/WhateverFaceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _MaskEyebrow ("MaskEyebrow", 2D) = "white" {}
        _Blend("Blend",Range(0,1)) = 1
        _BlendEyebrow("BlendEyebrow",Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Plane"}
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
                float2 uv2 : TEXCOORD2;
               
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            sampler2D _MaskEyebrow;
            float4 _MaskEyebrow_ST;
            float _Blend;
            float _BlendEyebrow;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _MaskTex);
                o.uv2 = TRANSFORM_TEX(v.uv2, _MaskEyebrow);
                o.uv3 = TRANSFORM_TEX(float2(1 - v.uv2.x,v.uv2.y), _MaskEyebrow);
                
                return o;
            }
            
            float mask(float origin,float mask,float blend)
            {
                float range = step(mask,0);
                float partA = range * origin;
                float partB = (1 - range)*(1 - (1 -origin) * blend);
                return partA + partB;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 c =  tex2D(_MainTex,i.uv);
                float4 c1 = tex2D(_MaskTex,i.uv1);
                float4 c2 = tex2D(_MaskEyebrow,i.uv2);
                float4 c3 = tex2D(_MaskEyebrow,i.uv3);
                float ret = mask(c.r,c1.a,_Blend);
                float ret1 = mask(ret,c2.a,_BlendEyebrow);
                float ret2 = mask(ret1,c3.a,_BlendEyebrow);
                return ret2;
            }
            ENDCG
        }
    }
}
