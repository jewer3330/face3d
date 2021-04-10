Shader "Custom/FaceImageConvert"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MaskTex("Mask Texture", 2D) = "white" {}
		_BodyTex("Body Texture", 2D) = "white" {}
		_R("Radius", Range(0,25)) = 4
		_Threshold("Threshold", Range(0,1)) = 0.2
		_Color("Color Tint", Color) = (1, 1, 1, 1)
		_Whitening("Whitening", Range(2,10)) = 2
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;//
				float2 uv : TEXCOORD0;//脸部图形的UV
				float2 uv2 : TEXCOORD1;//AR相机的UV
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			sampler2D _MaskTex;
            float4 _MaskTex_ST;
			sampler2D _BodyTex;
			half _R;
			half _Threshold;
			half _Whitening;
			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv2 = TRANSFORM_TEX(v.uv2, _MaskTex);
				
				return o;
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
						float3 xi = tex2D(_MainTex, offset).rgb;
						float3 weight = max(0, 1 - abs(xi - x) / (_Threshold));
						col += weight * xi;
						sum += weight;
					}
				}

				return float3(sum == 0 ? x : col / sum);
			}

			bool is_skin_color(float3 color)
			{
				color *= 255;
				if (color.r > 95 && color.g > 40 && color.b > 20
					&& color.r > color.b
					&& color.r > color.g
					&& abs(color.r - color.g) > 15
					)
				{
					return 1;
				}
				return 0;
			}

			float3 process(float2 uv,float2 uv2)
			{
				float3 ret = tex2D(_MainTex, uv).rgb;
				bool isskin = is_skin_color(ret);
                float3 ret1 = surface_blur(uv,ret,_MainTex_TexelSize.xy);
                
				//非肤色过滤
				if (isskin)
				{
					ret = ret1;
				}
				else
				{
					ret = (ret1 + ret) * 0.5f;
				}
				//漂白处理
				//ret = ret + _Whitening * _Color;
                ret = log(ret *(_Whitening - 1) + 1) / log(_Whitening);

				/*float3 skinret = float3(isskin, isskin, isskin);
				return float4(skinret, 1);*/

				//uv2为深度摄像机
				//过滤边缘融合角色皮肤
				float4 msk = tex2D(_MaskTex, uv2);
				float4 body = tex2D(_BodyTex, uv2);
				float3 outRgb = ret.rgb * msk.a + body.rgb * (1 - msk.a);
				return outRgb;
			}

			fixed4 frag (v2f input) : COLOR
			{
				
				float3 outRgb = process(input.uv,input.uv2);

				return float4(outRgb,1);
			}
			ENDCG
		}
	}
}
