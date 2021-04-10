Shader "Clx/character/eyeball"
{
	Properties
	{
		[NoScaleOffset]_MainTex ("巩膜贴图", 2D) = "white" {}
		[NoScaleOffset]_NormalMap("巩膜法线贴图", 2D) = "bump"{}
		[NoScaleOffset]_EyeTex ("眼瞳贴图", 2D) = "black" {}
		[NoScaleOffset]_EyeNormalMap("眼瞳法线贴图", 2D) = "bump"{}
		_Hue1("颜色", Range(0, 1)) = 0
        _Saturation1("饱和", Range(0, 1)) = 0.333
		_Brightness1("浓淡", Range(0, 1)) = 0.85
		_UVScale ("眼瞳缩放", Range(0.7, 1.5)) = 1
		_SpecularIntensity ("高光强度", Range(0, 5)) = 2
		_Gloss1 ("高光光泽度", Range(5, 25)) = 7
		_CubeMap ("反射天空盒", Cube) = "Cube" {}
		_Reflection ("反射强度", Range(0, 0.2)) = 0.05
		_Gloss2 ("反光光泽度", Range(0.1, 1)) = 0
		_X ("高光横向偏移", Range(-1, 1)) = 0.4
		_Y ("高光纵向偏移", Range(-1, 1)) = -0.15
	}
	SubShader
	{
		Tags {"Queue" = "Geometry" "RenderType"="Opaque" }

		Pass
		{
			Tags{"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase	

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#include "PBRFunction.cginc"

			sampler2D _MainTex;
			sampler2D _EyeTex;
			sampler2D _NormalMap;
			sampler2D _EyeNormalMap;
			fixed _Hue1, _Saturation1, _Brightness1;
			fixed _SpecularIntensity;
			fixed _Gloss1;
			fixed _Gloss2;
			samplerCUBE _CubeMap;
			fixed _Reflection;
			fixed _X;
			fixed _Y;
			fixed _UVScale;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct VertexOutput
			{
				float4 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float3 worldPos : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
				float3 worldTangent : TEXCOORD4;
				float3 worldBinormal : TEXCOORD5;
				SHADOW_COORDS(6)
			};

			VertexOutput vert (VertexInput v)
			{
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);

				o.uv.xy = v.uv;
				float2 center = float2(0.5, 0.5);
				o.uv.zw = (v.uv - center) * _UVScale + center;
				
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTangent = normalize(mul(unity_ObjectToWorld, half4(v.tangent.xyz, 0)).xyz);
				o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);
				//环境光与灯光计算
				half nl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb;	//灯光校正弱化皮肤明暗
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));
				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (VertexOutput i) : SV_Target
			{
				half4 fragColor = half4(1,1,1,1);
				//灯光方向 实现方法 半角向量
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

				fixed4 mainTex = tex2D(_MainTex, i.uv.xy);
				fixed4 tex1 = tex2D(_EyeTex, i.uv.zw);
				fixed4 hsvcol1 = TexHSV(tex1, _Hue1, _Saturation1, _Brightness1)*_Brightness1;
				//hsvcol1 *= tex1;
				mainTex.rgb = lerp(mainTex.rgb, hsvcol1, tex1.a);

				half3 normalMap = UnpackNormal(tex2D(_NormalMap, i.uv.xy));
				half3 normal1 = UnpackNormal(tex2D(_EyeNormalMap, i.uv.zw));
				half3 normal = lerp(normalMap, normal1, tex1.a);
				normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));			
				//从顶点着色器储存切线到世界坐标系转换矩阵
				float3x3 tangentTransform = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
				fixed3 normalDir = normalize(mul(normal, tangentTransform));

				//非标准化半角向量
				//fixed mask = tex2D(_MainTex, i.uv).a;
				fixed3 halfDir = normalize((viewDir + worldLightDir + fixed3(_X, _Y, 0)));
				//fixed2 displacement = _Refraction*(mask)*(viewDir.y);
				//fixed2 displacement = _Refraction*(mask)*(viewDir.xy/viewDir.z);
				//fixed4 mainTex = tex2D(_MainTex, (i.uv-displacement));
				////环境反射
				half3 reflectDirection = normalize(reflect(-viewDir, normalDir));
				//half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDirection);
				//half3 skyColor = DecodeHDR(skyData,unity_SpecCube0_HDR) ;
				half3 skyColor = texCUBE(_CubeMap, reflectDirection);
				//Lambert光照模型
				half lightingModel = max(0, dot(normalDir, worldLightDir));//灯光校正弱化皮肤明暗
				//灯光 视线 法线 方向计算
				fixed NdotH = max(0, dot(normalDir, halfDir));
				//高光blinnphong公式
				fixed3 skyspecular = pow(NdotH, _Gloss2 * 50);
				fixed3 specular = pow(NdotH, _Gloss1 * 50) * _SpecularIntensity;
				//阴影计算
				fixed shadow = SHADOW_ATTENUATION(i);
				half3 skyreflection = skyColor * _Reflection;//校正天空盒反射
				//fragColor.rgb = (skyreflection * mask * 100 + mainTex) * (i.diff * shadow + i.ambient);
				fragColor.rgb = (specular + skyspecular * skyreflection + mainTex) * (i.diff * shadow + i.ambient);
				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
