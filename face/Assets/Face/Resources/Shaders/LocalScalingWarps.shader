Shader "Hidden/FaceBeautification/LocalScalingWarps"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		
		_PixelCenter ("Pixel Center", Vector) = (0,0,0,0)
		_PixelRadius ("Pixel Radius", Float) = 0
		_A ("a", Range(-1, 1)) = 0
		
		[Toggle]_Debug ("Debug", Float) = 0
		[Enum(Radius(Add),0,Radius(Overlay),1,Radius(Diff),2)]
		_DebugMode ("Debug Mode", Int) = 0
		_Offset ("Offset", Vector) = (0,0,0,0)
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
	};
	
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float2 _MainTex_TexelSize;
	
	float2 _PixelCenter;
	float _PixelRadius;
	float _A;
	float2 _Offset;
	
	int _DebugMode;

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}
    ENDCG
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		ZWrite Off
 
        // Interactive Image Warping
        // 4.4.2. Local scaling warps 
        // http://www.gson.org/thesis/warping-thesis.pdf
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
	        #pragma shader_feature _DEBUG_ON

			float4 frag(v2f i) : COLOR
			{
			    _PixelCenter *= 1 + _Offset;
			
			    // to pixel space
			    float2 pos = i.uv / _MainTex_TexelSize;
                float rMax = _PixelRadius;
                float r0 = distance(_PixelCenter, pos);
                float r;
                float2 deltaPos = pos - _PixelCenter;
                float a = _A;
                float2 uv = i.uv;
                float debug = 0;
                
                if (r0 < rMax) {
                    float rScale = (1 - pow((r0 / rMax - 1), 2) * a);
                    r = rScale * r0;
                    float2 posOut = _PixelCenter + (pos - _PixelCenter) * rScale;
                    uv = posOut * _MainTex_TexelSize;
                    debug = r * _MainTex_TexelSize;
                }
                else {
                    r = r;
                }
                
				float3 input = tex2D(_MainTex, uv).rgb;
				
				#if _DEBUG_ON
                    debug = clamp(r / rMax, -1, 1);
                    
                    if(_DebugMode == 0 && r0 < rMax){ // radius (Add)
                        float3 cDebug = 0;
                        if(debug>0)
                            cDebug.r = debug;
                        if(debug<0)
                            cDebug.b = -debug;
                        input += cDebug;
				    }
				    else if(_DebugMode == 1){ // radius (Overlay)
                        float3 cDebug = 0;
                        if(debug>0)
                            cDebug.r = debug;
                        if(debug<0)
                            cDebug.b = -debug;
                        return float4(cDebug, 1); 
				    }
				    else if(_DebugMode == 2){ // radius (Diff)
                        float3 cDebug = saturate(float3(r0 - r, 0, r - r0) / rMax);
                        return r0 < rMax ? float4(cDebug, 1) : 0; 
				    }
                #endif
                
                return float4(input, 1);
			}
			ENDCG
		}
	}
}
