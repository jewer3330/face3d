Shader "Custom/FaceSkinShader"
{
	Properties
	{
        _MainTex("MainTex", 2D) = "white" {}
        _MaskOuterTex("MaskOuterTex", 2D) = "white" {}
		_MaskInnerTex("MaskInnerTex", 2D) = "white" {}
        _Whitening("Whitening",Vector) = (0,0,0,0)
        _AlphaCutOuter("AlphaCutOuter",Range(0,1)) = 0.6
        _AlphaCutInner("AlphaCutInner",Range(0,1)) = 0.6
        _Length("Length",Range(0,100)) = 10
        _Hmin("Hmin",Range(0,256)) = 7
        _Hmax("Hmax",Range(0,256)) = 20
        _S("S",Range(0,256)) = 28
        _V("V",Range(0,256)) = 20
        _Cut("Cut",Range(0,1)) = 0
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
        float2 uv1 : TEXCOORD1;
        float2 uv2 : TEXCOORD2;
        float4 vertex : SV_POSITION;
    };
    
  

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    float4 _Whitening;
    sampler2D _MaskOuterTex;
    float4 _MaskOuterTex_ST;
    
    sampler2D _MaskInnerTex;
    float4 _MaskInnerTex_ST;
    float _AlphaCutOuter;
    float _AlphaCutInner;
    float _Hmin;
    float _Hmax;
    float _S;
    float _V;
    float _Length;
    float _Cut;
    
    
    
    
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.uv1 = TRANSFORM_TEX(v.uv, _MaskInnerTex);
        o.uv2 = TRANSFORM_TEX(v.uv, _MaskOuterTex);
        return o;
    }
    
       
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
    
    float inverse_lerp(float a ,float b,float temp)
    {
        return (temp - a)/(b - a);
    }
    
    float is_skin_hsv(float3 c)
    {
        c *= 256;
        int lenth = _Length;
        float smoth1 = 0;
        float smoth2 = 0;
        float smoth3 = 0;
        
        smoth1 = saturate(min(inverse_lerp(_Hmin - lenth,_Hmin,c.x),inverse_lerp(_Hmax + lenth,_Hmax,c.x)));
        smoth2 = saturate(inverse_lerp(_S,_S + lenth,c.y));
        smoth3 = saturate(inverse_lerp(_V,_V + lenth,c.z));
        
        return saturate(min(min( smoth1,smoth2),smoth3 ));
    }
    //调整肤色
    fixed4 skin_white(v2f input) : SV_Target
    {
       float4 c = tex2D(_MainTex, input.uv);
       float3 hsv = RGB2HSV(c.rgb);
       float3 light;
        
       light.r = lerp(0,1.0, (hsv.r * _Whitening.x ));
       light.g = lerp(0,1.0, (hsv.g * _Whitening.y ));
       light.b = lerp(0,1.0, (hsv.b * _Whitening.z ));
        
       return float4(HSV2RGB(light),c.a);
    }  
    
    float skin_color(float3 skin)
    {
        float3 hsv =  RGB2HSV(skin);
        float is_skin = is_skin_hsv(hsv);
        float ret = is_skin;
        return ret;
    }
    
     //计算非五官区域肤色mask               
    fixed4 skin_color_mask_without_wuguan(v2f input) : SV_Target
    {
        float4 skin = tex2D(_MainTex, input.uv);
        float  mask_outer = step(_AlphaCutOuter,tex2D(_MaskOuterTex, input.uv2).a);
        float  mask_inner = step(_AlphaCutInner,tex2D(_MaskInnerTex, input.uv1).a);
        //return mask_outer;
        //return mask_inner;
        //return mask_outer *(1 - mask_inner);
        //return 1 - mask_outer + mask_inner;
        //return   (mask_outer - mask_inner) * skin_color_without_wuguan(skin.rgb,input.uv);
        float ret = (mask_outer - mask_inner) * skin_color(skin.rgb)+ mask_inner + 1 - mask_outer;
        return ret;
    }
    
     //计算非五官区域肤色mask               
    fixed4 get_eyebrow(v2f i) : SV_Target
    {
        float4 skin = tex2D(_MainTex, i.uv);
        if(i.uv.x > 0.2 && i.uv.x < 0.5 && i.uv.y > 0.72 && i.uv.y < 0.86)
        {
            float ret = 0;
            ret =   skin_color(skin.rgb);
            return step(_Cut,ret);
        }
        return skin.a;
    }
    
      
     //计算肤色mask                 
    fixed4 skin_color_mask(v2f input) : SV_Target
    {
        float3 skin = tex2D(_MainTex, input.uv).rgb;
        return  skin_color(skin);
    }                  
    ENDCG
    
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        Cull Off ZWrite Off 
		
       
        Pass //0
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_color_mask

            ENDCG
        }
       
        Pass //1
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_white

            ENDCG
        }
        
        //非五官区域肤色mask 2
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_color_mask_without_wuguan

            ENDCG
        }
        
        //五官区域肤色mask3
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment get_eyebrow

            ENDCG
        }
	}
}
