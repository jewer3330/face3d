Shader "Unlit/FaceMask"
{
	Properties
	{
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        [NoScaleOffset]_LeftTex("LeftTex", 2D) = "white" {}
        [NoScaleOffset]_RightTex("RightTex", 2D) = "white" {}
        //[NoScaleOffset]_UpTex("UpTex", 2D) = "black" {}
        //[NoScaleOffset]_DownTex("DownTex", 2D) = "black" {}
        [NoScaleOffset]_MainMaskTex("MainMaskTex", 2D) = "black" {}
        _LeftMaskTex("LeftMaskTex", 2D) = "black" {}
        [NoScaleOffset]_UpMaskTex("UpMaskTex", 2D) = "black" {}
        [NoScaleOffset]_DownMaskTex("DownMaskTex", 2D) = "black" {}
        [NoScaleOffset]_ExtraMaskTex("鼻子mask", 2D) = "black" {}
        [NoScaleOffset]_NostrilMask("鼻孔mask", 2D) = "black" {}
        _Offset("Offset",Range(-1,1)) = 1
        _Alpha("Alpha",Range(0,1)) = 1
        _AlphaHole("AlphaHole",Range(0,1)) = 1
        _OffsetHole("OffsetHole",Range(0,1)) = 1
	}
    
     CGINCLUDE

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
        float2 uv_LeftMaskTex : TEXCOORD1;
        float2 uv_RightMaskTex : TEXCOORD2;
    };
    
  

    sampler2D _MainTex;
    sampler2D _LeftTex;
    sampler2D _RightTex;
    //sampler2D _UpTex;
    //sampler2D _DownTex;
    
    sampler2D _MainMaskTex;
    sampler2D _LeftMaskTex;
    float4 _LeftMaskTex_ST;
    sampler2D _RightMaskTex;
    sampler2D _UpMaskTex;
    sampler2D _DownMaskTex;
    sampler2D _ExtraMaskTex;
    sampler2D _NostrilMask;
    float _Offset;
    float _Alpha;
    float _AlphaHole;
    float _OffsetHole;
    
    
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        o.uv_LeftMaskTex = TRANSFORM_TEX(v.uv, _LeftMaskTex);
        o.uv_RightMaskTex = TRANSFORM_TEX(float2(1-v.uv.x, v.uv.y), _LeftMaskTex);
       
        return o;
    }
    
    float inverse_lerp(float a ,float b,float temp)
    {
        return (temp - a)/(b - a);
    }
    
    float4 findNoseHole(float4 color,float2 uv)
    {
        if(uv.x > 0.4 && uv.x < 0.6 && uv.y >0.45 && uv.y < 0.55)
        {
            color = color * 256;
            const int lenth = 20.0;
            float smoth1 = 0;
            float smoth2 = 0;
            float smoth3 = 0;
            
            smoth1 = saturate(inverse_lerp(0,0 + lenth,color.x));
            smoth2 = saturate(inverse_lerp(0,0 + lenth,color.y));
            smoth3 = saturate(inverse_lerp(0,0 + lenth,color.z));
            
            float min_smoth = min( min(smoth1,smoth2),smoth3);
            return 1 - min_smoth;
        }
            
        else
            return 0;
    }
  
    //调整肤色
    fixed4 frag (v2f i) : COLOR
    {
        //找出鼻洞的mask
        float4 src = tex2D(_MainTex, i.uv);
        float4  noseholeMask = findNoseHole(src,i.uv);
         
        //return noseholeMask;
        
        //查找鼻影的mask
        float4 noseL = tex2D(_ExtraMaskTex, i.uv_LeftMaskTex);
        float left_a = noseL.g;
        float right_a = noseL.r;
        float2 offsetLR = float2(-0.2,0);
        float2 offsetUD = float2(0,-0.2);
        
        //推UV 把鼻子Mask区域的UV + 鼻子Mask.a * offset
        float2 newUV = i.uv + left_a * _Offset * offsetLR - right_a * _Offset * offsetLR;
        newUV = newUV + left_a * _Alpha * offsetUD + right_a * _Alpha * offsetUD;
        
        float msk1 = saturate(tex2D(_MainMaskTex, i.uv).a);
        float4 col1 = tex2D(_MainTex, newUV);
        
        //return col1 ;
        
        
        
        float4 col2 = tex2D(_LeftTex, i.uv);
        float msk2 = saturate(tex2D(_LeftMaskTex, i.uv_LeftMaskTex).a);
       
        float4 col3 = tex2D(_RightTex, i.uv);
        float msk3 = saturate(tex2D(_LeftMaskTex, i.uv_RightMaskTex).a);
       
        float msk4 = tex2D(_UpMaskTex, i.uv).a;
        float msk5 = tex2D(_DownMaskTex, i.uv).a;

        //印章工具
        float2 offset = float2(0,-0.3);
        float4 noseholeColor = tex2D(_MainTex, i.uv + offset * _OffsetHole);
        col1 = col1 + noseholeColor * noseholeMask.a * _AlphaHole;
        
        //模型上的鼻孔
        float4 msk6 = tex2D(_NostrilMask,i.uv);
        col1 = float4(col1.rgb * msk6.a,col1.a); 
       
                                
        float sum = msk1 + msk2 + msk3 + msk4 + msk5;
        
        
        float3 outRgb =  msk1 / sum * col1.rgb + msk2 / sum * col2.rgb + msk3 / sum * col3.rgb + msk4 / sum * col1.rgb + msk5 / sum * col1.rgb;

        
        return float4(outRgb, col1.a);
    }
    
      
                    
    ENDCG
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Plane" }
       
        Cull Off ZWrite Off 
        
        
        
       
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDCG
        }
       
    }
	
}
