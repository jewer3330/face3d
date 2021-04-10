Shader "Hidden/FaceBeautification/LocalTranslationWarps"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_c_orig ("Control Orig", Vector) = (0.2,0.2,0,0)
		_c_mou ("Control Mouse", Vector) = (0.8,0.8,0,0)
		_c_max_dist ("Control Max Dist", Float) = 0.5
		
		[Space][Header(Debug)]
		[Toggle]_Debug ("Debug", Float) = 0
		[Enum(Radius(Add),0,Radius(Overlay),1,Radius(Diff),2)]
		_DebugMode ("Debug Mode", Int) = 0
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

	// c: warp_control
	float2 _c_mou;
	float2 _c_orig;
	float _c_max_dist;
	
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
        // 4.4.1. Local translation warps 
        // http://www.gson.org/thesis/warping-thesis.pdf
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
	        #pragma shader_feature _DEBUG_ON

			float hypotsq(float2 p){
				return p.x * p.x + p.y * p.y;
			}

			float4 frag(v2f i) : COLOR
			{
				float c_max_dist_sq = _c_max_dist * _c_max_dist;
				float2 fuv = i.uv;
				float2 dx = fuv - _c_orig;
				if(dx.x > -_c_max_dist && dx.x < _c_max_dist &&
					dx.y > -_c_max_dist && dx.y < _c_max_dist)
				{
					float rsq = hypotsq(dx);
					if(rsq < c_max_dist_sq){
						float msq = hypotsq(dx - _c_mou);
						float edge_dist = c_max_dist_sq - rsq;
						float a = edge_dist / (edge_dist + msq);
						fuv -= a * a * _c_mou;
					}
				}

				float3 input = tex2D(_MainTex, fuv).rgb;
				
				#if _DEBUG_ON
                    float debug = clamp(length(fuv - i.uv), -1, 1);
					debug += sign(debug) * 0.3;
                    
                    if(_DebugMode == 0 ){ // radius (Add)
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
						float d = fuv - i.uv;
                        float3 cDebug = saturate(float3(d, 0, -d));
                        return d != 0 ? float4(cDebug, 1) : 0; 
				    }
                #endif
                
                return float4(input, 1);
			}
			ENDCG
		}
	}
}
