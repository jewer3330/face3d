Shader "Unlit/FaceSkinGen"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SkinColor("Skin Color",Color) = (1,1,1,1)
		_BlurRadius("Blur Radius",Vector) = (0.001,0,0,0)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGINCLUDE
		#include "UnityCG.cginc"
		uniform sampler2D _MainTex;
		uniform half4 _MainTex_TexelSize;
		fixed4 _SkinColor;
		float4 _BlurRadius;

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

		fixed4 frag_gaussian(v2f i) : COLOR
		{
			float2 delta = _MainTex_TexelSize.xy * _BlurRadius.xy;
			fixed4 faceSkin = tex2D(_MainTex, i.uv);
            return fixed4(faceSkin.rgb, 1); // disable
			fixed4 col = 0.37004405286 * faceSkin;
			col += 0.31718061674 * tex2D(_MainTex,i.uv - delta);
			col += 0.31718061674 * tex2D(_MainTex,i.uv + delta);
			col += 0.19823788546 * tex2D(_MainTex,i.uv - 2.0 * delta);
			col += 0.19823788546 * tex2D(_MainTex,i.uv + 2.0 * delta);
			col += 0.11453744493 * tex2D(_MainTex,i.uv - 3.0 * delta);
			col += 0.11453744493 * tex2D(_MainTex,i.uv + 3.0 * delta);
			col /= 0.37004405286 + 0.31718061674 + 0.31718061674 + 0.19823788546 + 0.19823788546 + 0.11453744493 + 0.11453744493;
			return fixed4(col.rgb, 1.0f);
		}

		fixed4 frag_mergeGenSkin(v2f i) : COLOR
		{
			const float3 GrayTrans = float3(1.0, 1.0, 1.0);
			float4 faceSkin = tex2D(_MainTex, i.uv);
			float mixAlpha = faceSkin.a;
			float3 outRGB = faceSkin.rgb * mixAlpha + _SkinColor.rgb * (1.0 - mixAlpha);
			float4 c = float4(outRGB, 1.0f);
            
            //
            return faceSkin;
            c.rgb = lerp(_SkinColor.rgb, faceSkin.rgb, faceSkin.a);
            c.a = 1;
            return c;
            
            // todo: ??
			if (c.r > 0.3725 && c.g > 0.1568 && c.b > 0.0784 && c.r > c.b &&
				(max(max(c.r, c.g), c.b) - min(min(c.r, c.g), c.b)) > 0.0588f && abs(c.r - c.g) > 0.0588f)
			{
				c = _SkinColor;
			}
			return c;
		}

		ENDCG

		Pass
		{
			Name "GenSkin"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_mergeGenSkin
			ENDCG
		}

		Pass
		{
			Name "GaussianSkin"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_gaussian 
			ENDCG
		}
	}
}
