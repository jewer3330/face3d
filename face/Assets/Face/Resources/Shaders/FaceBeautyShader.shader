Shader "Custom/FaceBeautyShader"
{
	Properties
	{
        _SrcTex("Texture", 2D) = "white" {}
		_MaskTex("MaskTex", 2D) = "white" {}
        _BlurTex("BlurTexture", 2D) = "white" {}
		_HighPassTex("HighPassTexture", 2D) = "white" {}
        
        _Radius("Radius",Range(1,20)) = 2
        _Blend("Blend",Range(0,1)) = 0.5
        _Whitening("Whitening",Range(1,2)) = 2
        _Whitening2("Whitening2",Vector) = (0,0,0,0)
        _R("R",Range(1,10)) = 4
        _Threshold("Threshold",Range(0,1)) = 0.3
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
    
    struct v2f_with_guass
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        half2 guass_uv[5] : TEXCOORD1;
    };

    sampler2D _SrcTex;
    sampler2D _MaskTex;
    float4 _SrcTex_ST;
    float4 _SrcTex_TexelSize;
    sampler2D _BlurTex;
    float4 _BlurTex_ST;
    sampler2D _HighPassTex;
    float4 _HighPassTex_ST;
    float4 _HighPassTex_TexelSize;
    
    float _Radius;
    float _Blend;
    float _Whitening;
    int _ColorsLength;
    uniform float _Colors[256];
    
    int _ColorsWhiteLength;
    uniform float _ColorsWhite[256];
    float4 _Whitening2;
    float _R;
    float _Threshold;
    
    
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _SrcTex);
        
        return o;
    }
    
    
    v2f_with_guass vertHorizon(appdata v)
    {
        v2f_with_guass o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _SrcTex);
        o.guass_uv[0] =  o.uv + float2(-2, 0) * _HighPassTex_TexelSize.x * _Radius;
        o.guass_uv[1] =  o.uv + float2(-1, 0) * _HighPassTex_TexelSize.x * _Radius;
        o.guass_uv[2] =  o.uv + float2(0, 0) * _HighPassTex_TexelSize.x * _Radius;
        o.guass_uv[3] =  o.uv + float2(1, 0) * _HighPassTex_TexelSize.x * _Radius;
        o.guass_uv[4] =  o.uv + float2(2, 0) * _HighPassTex_TexelSize.x * _Radius;
        return o;
    }

    v2f_with_guass vertVertical(appdata v)
    {
        v2f_with_guass o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _SrcTex);
        o.guass_uv[0] =  o.uv + float2(0, -2) * _HighPassTex_TexelSize.y * _Radius;
        o.guass_uv[1] =  o.uv + float2(0, -1) * _HighPassTex_TexelSize.y * _Radius;
        o.guass_uv[2] =  o.uv + float2(0, 0) * _HighPassTex_TexelSize.y * _Radius;
        o.guass_uv[3] =  o.uv + float2(0, 1) * _HighPassTex_TexelSize.y * _Radius;
        o.guass_uv[4] =  o.uv + float2(0, 2) * _HighPassTex_TexelSize.y * _Radius;
        return o;
    }

    fixed4 guass_blur(v2f_with_guass v) : COLOR
    {
        const float weight[5] = { 0.0544, 0.2442, 0.4027, 0.2442, 0.0544 };
        fixed3 col = tex2D(_HighPassTex, v.guass_uv[0]).rgb * weight[0];
        col += tex2D(_HighPassTex, v.guass_uv[1]).rgb * weight[1];
        col += tex2D(_HighPassTex, v.guass_uv[2]).rgb * weight[2];
        col += tex2D(_HighPassTex, v.guass_uv[3]).rgb * weight[3];
        col += tex2D(_HighPassTex, v.guass_uv[4]).rgb * weight[4];
        return fixed4(col, 1);
    }

    
    fixed4 high_pass (v2f input) : COLOR
    {
        float4 src = tex2D(_SrcTex, input.uv);
        float4 high = tex2D(_BlurTex, input.uv);
        high = (src - high ) + float4(0.5,0.5,0.5,1);
       
        return high;
    }
    
    float high_light_single(float x,float y)
    {
        if( x <= 0.5)
        {
            return 2 * x * y;
        }
        else
        {
            return 1 - 2 *(1 - x)*(1 - y);
        }
    }
    
    //强光混合模式
    fixed4 high_light (v2f input) : COLOR
    {
        float4 src = tex2D(_HighPassTex, input.uv);
        
        float4 high = src;
       
        high = float4(high_light_single(high.r,high.r),
        high_light_single(high.g,high.g),
        high_light_single(high.b,high.b),
        1);
        
        high = float4(high_light_single(high.r,high.r),
        high_light_single(high.g,high.g),
        high_light_single(high.b,high.b),
        1);
        
        high = float4(high_light_single(high.r,high.r),
        high_light_single(high.g,high.g),
        high_light_single(high.b,high.b),
        1);
        
       
        return high;
    }
    
    
      
    //3
    fixed4 skin_blend (v2f input) : COLOR
    {
        float4 src = tex2D(_SrcTex, input.uv);
        float4 dest = tex2D(_HighPassTex, input.uv);
        float alpha = 1 - dest.b;
        float4 orign = float4(_Whitening * src.rgb,1) ;
        float4 bright = orign * alpha + src *(1 - alpha);
        
        return  bright;
       
    }    
     
    fixed4 skin_white(v2f input) : COLOR
    {
       float4 high = tex2D(_HighPassTex, input.uv);
       //return high;
       int offr = (int)(high.r * 256);
       int offg = (int)(high.g * 256);
       int offb = (int)(high.b * 256);
       float r = _Colors[offr];
       float g = _Colors[offg];
       float b = _Colors[offb];
       return float4(r,g,b,1);
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
        const int lenth = 20;
        float smoth1 = 0;
        float smoth2 = 0;
        float smoth3 = 0;
        
        smoth1 = saturate(min(inverse_lerp(7 - lenth,7,c.x),inverse_lerp(20 + lenth,20,c.x)));
        smoth2 = saturate(inverse_lerp(28,28 + lenth,c.y));
        smoth3 = saturate(inverse_lerp(20,20 + lenth,c.z));
       
        
        return saturate(min(min( smoth1,smoth2),smoth3 ));
    }
       
    fixed4 skin_white1(v2f input) : COLOR
    {
       float4 high = tex2D(_HighPassTex, input.uv);
        return high;
        float3 hsv = RGB2HSV(high.rgb);
        float is_skin = is_skin_hsv(hsv);
        
        float3 light   = hsv * _Whitening2;
        float3 ret =  lerp(hsv,light,is_skin);
       
        return float4(HSV2RGB(ret),1);
    }  
    
    float3 surface_blur(float2 uv,float3 color,float2 delta)
    {
        float3 x = color;
        float3 col = float3(0, 0, 0);
        float3 sum = float3(0, 0, 0);
        int len = (int)_R;
        
        for (int i = -len; i <= len; i++)
        {
            for (int j = -len; j <= len; j++)
            {
                float u = clamp(i * delta.x + uv.x, 0, 1);
                float v = clamp(j * delta.y + uv.y, 0, 1);
                float2 offset = float2(u, v);
                float3 xi = tex2D(_HighPassTex, offset).rgb;
                float3 weight = max(0, 2500 - abs(xi - x) * 1000 / (_Threshold));
                col += weight * xi;
                sum += weight;
            }
        }

        return float3(sum == 0 ? x : col / sum);
    }
    
//R>95 AND G>40 B>20 AND MAX(R,G,B)-MIN(R,G,B)>15 AND ABS(R-G)>15 AND R>G AND R>B

//在侧光拍摄环境下：

//R>220 AND G>210 AND B>170 AND ABS(R-G)<=15 AND R>B AND G>B
    bool is_skin_color(float3 color)
    {
        color *= 256;
        if ((color.r > 95 && color.g > 40 && color.b > 20
            && color.r > color.b
            && color.r > color.g
            && abs(color.r - color.g) > 15
            ) ||
            (color.r > 220 && color.g > 210 && color.b > 170 && abs(color.r - color.g) <= 15 && color.r > color.b && color.g > color.b)
            )
        {
            return 1;
        }
        return 0;
    }
    
    fixed4 fast_surface(v2f input) : COLOR
    {
        float3 skin = tex2D(_HighPassTex, input.uv).rgb;
        float3 ret = surface_blur(input.uv,skin,_HighPassTex_TexelSize.xy);
        return float4(ret,1);
        
       
    }
    
    // Pass 7    
    fixed4 surface(v2f input) : COLOR
    {
        float4 c = tex2D(_HighPassTex, input.uv);
        float4 c1 = tex2D(_SrcTex, input.uv);
        float3 skin = c.rgb;
        //return float4(skin,c1.a);
        float4 mask = tex2D(_MaskTex, input.uv);
        
        float is_skin = is_skin_hsv(RGB2HSV(skin));
        //bool is_skin = is_skin_color(skin);
           
        float3 ret = surface_blur(input.uv,skin,_HighPassTex_TexelSize.xy);
        //return float4(ret,c1.a);
        float3 dest = lerp(skin,ret,is_skin);
        //return float4(dest,1);
        return float4(lerp(skin,dest,_Blend * mask.a),c1.a);
        
        //if(!is_skin)
        //    return fixed4(skin,1); 
        //else
            //return fixed4(ret,1);
    }          
      
                       
     fixed4 skin_color(v2f input) : COLOR
    {
        float3 skin = tex2D(_HighPassTex, input.uv).rgb;
        float3 hsv =  RGB2HSV(skin);
        float is_skin = is_skin_hsv(hsv);
        if(is_skin)
            return float4(hsv.xyz , is_skin);
        else
            return float4(0,0,0,0);
    } 
    
    
     float soft_light(float src,float dest) 
    {
        float ret = 1;
        if(src <= 0.5)
        {
            ret = (2 * src - 1) * (dest - dest * dest) + dest;
        }
        else
        {
            ret = (2 * src - 1) * (sqrt(dest) - dest) + dest;
        }
        return ret;
    } 
    
    
    fixed4 remove_light(v2f input) : COLOR
    {
        float3 skin = tex2D(_HighPassTex, input.uv).rgb;
        float3 orign = skin;
        //orign.g = max(0,skin.g - 0.1f);
        float3 hsv =  RGB2HSV(skin);
        hsv.g = 0;
        float3 ret = 1 - HSV2RGB(hsv);
        ret = float3( 
            soft_light(ret.r,orign.r),
            soft_light(ret.g,orign.g),
            soft_light(ret.b,orign.b)
            );
        return float4(ret,1);    
    } 
    
     
   
                                                       
    ENDCG
    
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        Cull Off ZWrite Off 
		//0
        Pass
        {
            Name "high_pass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment high_pass
            ENDCG
        
        }
        //1
        Pass
        {
            Name "guass_blur_h"
            CGPROGRAM
            #pragma vertex vertHorizon
            #pragma fragment guass_blur

            ENDCG
        }
        //2
        Pass
        {
            Name "guass_blur_v"
            CGPROGRAM
            #pragma vertex vertVertical
            #pragma fragment guass_blur

            ENDCG
        }
        //3
        Pass
        {
            Name "skin_blend"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_blend

            ENDCG
        }
        //4
        Pass
        {
            Name "skin_white"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_white

            ENDCG
        }
        //5
        Pass
        {
            Name "high_light"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment high_light

            ENDCG
        }
        
        //6
        Pass
        {
            Name "skin_white1"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_white1

            ENDCG
        }
        
        //7
        Pass
        {
            Name "surface"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment surface

            ENDCG
        }
        
        
         //8
        Pass
        {
            Name "skin_color"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment skin_color

            ENDCG
        }
        
          //9 
        Pass
        {
            Name "remove_light"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment remove_light

            ENDCG
        }
        
        
          //10 fast_surface 
        Pass
        {
            Name "fast_surface"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fast_surface

            ENDCG
        }
	}
}
