Shader "Clx/character/skinpbr"
{
	Properties
	{
		[NoScaleOffset]_MainTex("皮肤纹理", 2D) = "white" {}
		[NoScaleOffset]_NormalMap("法线贴图",2D) = "bump"{}
		_NormalAmount("法线强度",Range(0,2)) = 1
		[NoScaleOffset]_MaskTex("AO(R)粗糙度(G)金属度(B)",2D) = "white"{}
		_Occlusion("AO",Range(0,1)) = 0.5
		//[NoScaleOffset]_SmoothTex("皮肤光泽贴图",2D) = "white"{}
		//[NoScaleOffset]_BeckmannLUT("Beckmann LUT",2D) = "white"{}
		[NoScaleOffset]_SSSLUT("SSSLUT",2D) = "white"{}
		//_SSSLUTColor("sssColor",Color) = (1,1,1,1)
		_Cureve("光线弯曲值",Range(0,1)) = 1
		_Metalness("金属度",Range(0,1)) = 0
		_Roughness("粗糙度",Range(0,1)) = 1
		_Brightness("明度",Range(0,1)) = 0
		//_Specular("高光",Range(0,20)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

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

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 pos : SV_POSITION;
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 TtoW0 : TEXCOORD4;
				float4 TtoW1 : TEXCOORD5;
				float4 TtoW2 : TEXCOORD6;
				SHADOW_COORDS(7)
			};

			sampler2D _MainTex;
			sampler2D _NormalMap;
			sampler2D _MaskTex;
			sampler2D _SSSLUT;
			//sampler2D _SmoothTex;
			//sampler2D _BeckmannLUT;
			//fixed4 _MainTex_ST;
			fixed _NormalAmount;
			fixed _Metalness;
			fixed _Roughness;
			fixed _Occlusion;
			fixed _Cureve;
			//fixed _Specular;
			fixed4 _SSSLUTColor;
			fixed _Brightness;

			VertexOutput vert (VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				fixed3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				//unity官方代码
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldTanget = UnityObjectToWorldDir(v.tangent);
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBitangent = cross(worldNormal, worldTanget) * tangentSign;
				o.TtoW0 = half4(worldTanget.x, worldBitangent.x, worldNormal.x, worldPos.x);
				o.TtoW1 = half4(worldTanget.y, worldBitangent.y, worldNormal.y, worldPos.y);
				o.TtoW2 = half4(worldTanget.z, worldBitangent.z, worldNormal.z, worldPos.z);

				//环境光与灯光计算
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb*0.5 * (1 - _Brightness) + _Brightness;	//灯光校正弱化皮肤明暗
				o.ambient = ShadeSH9(half4(worldNormal, 1));

				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (VertexOutput i) : SV_Target
			{
				half4 fragColor = fixed4(1,1,1,1);
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				fixed4 maskTex = tex2D(_MaskTex, i.uv);
				//fixed4 smoothTex = tex2D(_SmoothTex, i.uv);
				//ao
				mainTex.rgb = lerp(mainTex.rgb, mainTex.rgb * maskTex.r, _Occlusion);

				fixed3 worldPos = fixed3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
				//法线转换
				fixed3 tnormal = UnpackNormal(tex2D(_NormalMap,i.uv));
				fixed3 worldNormal;
				worldNormal.x = dot(i.TtoW0, tnormal);
				worldNormal.y = dot(i.TtoW1, tnormal);
				worldNormal.z = dot(i.TtoW2, tnormal);
				worldNormal.xy = worldNormal.xy * _NormalAmount;
				fixed3 normalDirection = normalize(worldNormal);
				//灯光方向 实现方法 半角向量
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldPos));
				fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
				//非标准化半角向量
				fixed3 h = (viewDir + worldLightDir);
				fixed3 halfDir = normalize(h);
				//粗糙度 金属度
				fixed metalness = _Metalness * maskTex.b;
				fixed roughness = _Roughness * maskTex.g ;
				//环境反射
				fixed3 reflectDirection = normalize(reflect(-viewDir, normalDirection));
				half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDirection);
				half3 skyColor = DecodeHDR(skyData,unity_SpecCube0_HDR) ;
				//Lambert光照模型
				half lightingModel = max(0, dot(normalDirection, worldLightDir));//灯光校正弱化皮肤明暗
				//灯光 视线 法线 方向计算
				fixed NdotH = max(0, dot(normalDirection, halfDir));
				//float NdotH = saturate(dot(halfDir, worldNormal));
				fixed NdotL = max(0, dot(normalDirection, worldLightDir));
				fixed NdotV = max(0, dot(normalDirection, viewDir));
				fixed LdotH = max(0, dot(worldLightDir, halfDir));
				//fixed LdotV = max(0, dot(worldLightDir, viewDir));
				fixed VdotH = max(0, dot(viewDir, halfDir));

				//SSSLUT
				//fixed4 sssLUT = tex2D(_SSSLUT, float2(NdotL * 0.5 + 0.5, _Cureve));
				fixed4 sssLUT = tex2D(_SSSLUT, float2(VdotH * 0.5, _Cureve));

				//Fresnel 
				//half3 F0 = lerp(half3(0.04, 0.04, 0.04), mainTex.rgb, metalness);

				//Fresnel 皮肤参数
				half3 F0 = lerp(half3(0.028, 0.028, 0.028), mainTex.rgb, metalness);
				//half3 F0 = lerp(half3(0.028, 0.028, 0.028), mainTex.rgb, roughness);

				half3 fresnel = FresnelSchlick(LdotH, F0);
				//half3 fresnel = fresnelReflectance(halfDir, viewDir, F0);
				//fresnel = lerp(col, fresnel, _Fresnel);			

				//half3 KD = (1 - fresnel) * (1 - metalness);
				half F90 = 0.5 + 2 * roughness * pow(LdotH, 2);
				half3 KD = (1 + (F90 -1) * pow((1 - LdotH), 5) * (1 + (F90 - 1) * pow(1 - NdotV, 5)));
				KD *= 1 - metalness;
				//阴影计算
				fixed shadow = SHADOW_ATTENUATION(i)*_LightColor0;
				//shadow *= _LightColor0.rgb;

				////kelemenLUT
				//float NdH = dot(normalDirection, halfDir);
				//float PH = pow(2.0 * tex2D(_BeckmannLUT, float2(NdotH, smoothTex.r)), 10);
				//half3 Specular = max(PH * fresnel / dot(h, h), 0)  * VdotH * _Specular * shadow;

				//NDF
				half SpecularDistribution = CookTorranceGGXNDF(NdotH, roughness)*lightingModel;
				//SpecularDistribution = GaussianNDF(NdotH,roughness) * shadow;
				//GSF
				half GeometricShadow = GeometrySmith(NdotV, NdotL, roughness);
				//高光
				half3 specularity = (SpecularDistribution * GeometricShadow * fresnel) / (4 * NdotV * NdotL + 0.0001);
				specularity *= (1 - roughness) * 10;	//粗糙度高光风格化
				//specularity += lerp(specularity, skyColor, fresnel);
				//皮肤反射
				//specularity += lerp(specularity, skyColor * maskTex.g, fresnel);
				//fragColor.rgb =  (specularity*lightingModel + KD * mainTex.rgb) * (sssLUT.rgb + lightingModel * shadow * i.diff + i.ambient);
				//fragColor.rgb =  (specularity + KD * mainTex.rgb) * (sssLUT.rgb + lightingModel * shadow * i.diff + i.ambient);
				fragColor.rgb = (specularity + KD * mainTex.rgb) * (sssLUT.rgb + lightingModel * shadow *0.5 * (1 - _Brightness) + _Brightness + i.ambient);

				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
