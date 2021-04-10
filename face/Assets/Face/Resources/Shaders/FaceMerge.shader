Shader "Unlit/FaceMerge"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_FaceTex("Face", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		CGINCLUDE
		#include "UnityCG.cginc"
		uniform sampler2D _MainTex;
		uniform sampler2D _FaceTex;
		uniform sampler2D _SkinTex;
		uniform half4 _MainTex_TexelSize;

		float _GrayFactor;
		float4 _BlurRadius;
		float _BilaterFilterFactor;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		v2f vert(appdata_full v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			return o;
		}

		fixed4 frag_mergeFaceSkin(v2f i) : COLOR
		{
			const float3 GrayTrans = float3(1.0, 1.0, 1.0);
			float4 back = tex2D(_MainTex, i.uv); // 
			float4 face = tex2D(_FaceTex, i.uv); // tmp1
			
			return float4(lerp(back, face, face.a).rgb,1);
			
		}
		ENDCG

		Pass
		{
			Name "GenFaceSkin"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_mergeFaceSkin
			ENDCG
		}
	}
}
