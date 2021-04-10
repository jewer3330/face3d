Shader "Custom/HSLShader"
{
	Properties
	{
        _MainTex("MainTex", 2D) = "white" {}
        _H("H",Range(0,1)) = 0
        _S("S",Range(0,1)) = 0
        _L("L",Range(0,1)) = 0
	}
    
    
    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
       
    }; 

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };
    
   

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float _H;
    float _S;
    float _L;
    
    
 
        
     float3 RGB2HSV(float3 c)  
    { 
        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);  
        float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));  
        float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));  
        float d = q.x - min(q.w, q.y);  
        float e = 1.0e-10;  
        return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);  
    }  
    
    float3 HSV2RGB(float3 c)  
    {
        float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);  
        float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);  
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);  
    }
   
    float3 HSV2HSL(float3 HSV)
    {
        float h = HSV.x;
        float v = HSV.z;
        float l =  v -  v  * HSV.y/2;
        float s = 0;
        if(l == 0 || l == 1)
        {
            s = 0;
        }
        else
        {
            s = (v - l)/min(l ,1-l);
        }
        
        return float3(h,s,l);
    }
    
    float3 HSL2HSV(float3 HSL)
    {
        float h = HSL.x;
        float l = HSL.z;
        float v=  l + HSL.y * min(l, 1- l);
        float s = 0;
        if(v == 0)
        {
            s = 0;
        }
        else
        {
            s = 2- 2* l / v;
        }
        return float3(h,s,v);
    } 
    
   
   
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        
        return o;
    }
   
   
    fixed4 frag(v2f input) : COLOR
    {
        float4 src = tex2D(_MainTex, input.uv);
        float3 skin = src.rgb;
        //float3 hsl =  RGBtoHSL(skin);
        float3 hsl =  HSV2HSL(RGB2HSV(skin));
        float h = lerp(-1,1,  _H )+ hsl.r;
        float s = pow(_S,log(hsl.g)/log(0.5));
        float l = pow(_L,log(hsl.b)/log(0.5));
        
        //return float4(HSLtoRGB(float3(h,s,l)),src.a);
        return float4(HSV2RGB(HSL2HSV(float3(h,s,l))),src.a);
    } 
   
  
                                                       
    ENDCG
    
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        Cull Off ZWrite Off 
	    //Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        
        }
	}
}
