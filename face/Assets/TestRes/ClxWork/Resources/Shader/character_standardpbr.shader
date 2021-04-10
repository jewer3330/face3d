Shader "Clx/character/standardpbr"
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
		[NoScaleOffset]_MainNormalMap("法线贴图", 2D) = "bump"{}
		_NormalScale("法线强度",Range(0,3)) = 1
		[NoScaleOffset]_MaskTex("AO/自发光(R)粗糙度(G)金属度(B)",2D) = "white"{}
		[MaterialToggle] _Emission_ON ("开启自发光", Float) = 0
		[HDR]_EmissionColor("自发光颜色", Color) = (1, 1, 1, 1)
		_Occlusion("AO",Range(0,1)) = 1
		_Metalness("金属度",Range(0,1)) = 1
		_Roughness("粗糙度",Range(0,1)) = 1
		//_ReflectionIntensity("反射强度",Range(0, 3)) = 3
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			Tags{"LightMode"="ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase 			
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "UnityStandardUtils.cginc"
			#include "PBRFunction.cginc"

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MainNormalMap;
			fixed4 _MainTex_ST;
			fixed _NormalScale;
			fixed _Metalness;
			fixed _Roughness;
			fixed _Occlusion;
			fixed _Emission_ON;
			fixed4 _EmissionColor;
			//fixed _ReflectionIntensity;
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float4 color : COLOR;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID	//gpu instance
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				float3 diff : COLOR1;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal :TEXCOORD2;
				float3 worldTangent : TEXCOORD3;
				float3 worldBinormal : TEXCOORD4;
				UNITY_FOG_COORDS(5)
				//UNITY_SHADOW_COORDS(6)	//mixlighting
				SHADOW_COORDS(6)	//mixlighting
				float2 lightmapUV :TEXCOORD7;
				float3 ambient :COLOR2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			////阴影衰减
			//float FadeShadows(VertexOutput i, float attenuation)
			//{
			//	#if HANDLE_SHADOWS_BLENDING_IN_GI
			//		float viewZ = dot(_WorldSpaceCameraPos - i.worldPos, UNITY_MATRIX_V[2].xyz);//构建视角矩阵转换Z分量
			//		float shadowFadeDistance = UnityComputeShadowFadeDistance(i.worldPos, viewZ);
			//		float shadowFade = UnityComputeShadowFade(shadowFadeDistance);//计算阴影衰减
			//		float bakedAttenuation = UnitySampleBakedOcclusion(i.lightmapUV, i.worldPos);//shadowmask采样,输入UV坐标和世界坐标
			//		//attenuation = saturate(attenuation + shadowFade);
			//		attenuation = UnityMixRealtimeAndBakedShadows(attenuation, bakedAttenuation, shadowFade);
			//		#endif
			//	return attenuation;
			//}
			////引用内置灯光参数
			//UnityLight CreateLight(VertexOutput i)
			//{
			//	UnityLight light;
			//	#if defined(DEFERRED_PASS) || SUBTRACTIVE_LIGHTING
			//		light.dir = float3(0, 1, 0);
			//		light.color = 0;
			//	#else
			//		#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
			//			light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
			//		#else
			//			light.dir = _WorldSpaceLightPos0.xyz;
			//		#endif
			//	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz);
			//	attenuation = FadeShadows(i, attenuation);
			//	light.color = _LightColor0.rgb * attenuation;
			//	#endif
			//	return light;
			//}

			VertexOutput vert (VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_OUTPUT(VertexOutput, o);	//重置顶点输出
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTangent = normalize(mul(unity_ObjectToWorld, half4(v.tangent.xyz, 0)).xyz);
				o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);
				o.lightmapUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));
				half hl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = hl * _LightColor0.rgb;
				//UNITY_TRANSFER_SHADOW(o, v.texcoord1);	//mixlighting
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			fixed4 frag (VertexOutput i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				half4 fragColor = half4(1,1,1,1);
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				fixed4 maskTex = tex2D(_MaskTex, i.uv);
				fixed3 colocclusion = lerp(mainTex.rgb, mainTex.rgb * maskTex.r, _Occlusion );
				fixed3 colemission = mainTex.rgb + _EmissionColor * maskTex.r;
				fixed3 col = lerp(colocclusion, colemission, _Emission_ON);

				fixed3 normalMap = UnpackNormal(tex2D(_MainNormalMap, i.uv));
				normalMap.xy *= _NormalScale ;
				normalMap.z = sqrt(1 - saturate(dot(normalMap.xy, normalMap.xy)));			

				float3x3 tangentTransform = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);

				fixed3 normalDir = normalize(mul(normalMap, tangentTransform));
				fixed3 worldLightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
				fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
				fixed3 halfDir = normalize(viewDir + worldLightDir);
				//获取金属度粗糙度
				fixed metalness = _Metalness * maskTex.b + 0.001;
				fixed roughness = _Roughness * maskTex.g + 0.001;	//粗糙度校正适应皮革等半粗糙材质
				//反射球体
				fixed3 reflectDirection = normalize(reflect(-viewDir, normalDir));
				half4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectDirection, roughness*UNITY_SPECCUBE_LOD_STEPS);
				//half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR)*(1-roughness);
				half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR) * metalness;

				fixed NdotH = max(0, dot(normalDir, halfDir));
				fixed NdotL = max(0, dot(normalDir, worldLightDir));
				fixed NdotV = max(0, dot(normalDir, viewDir));
				fixed LdotH = max(0, dot(worldLightDir, halfDir));
				half lightingModel = max(0, dot(normalDir, worldLightDir));	
				//阴影计算
				fixed3 shadow = SHADOW_ATTENUATION(i) * _LightColor0;
				//菲涅尔部分
				half3 F0 = lerp(half3(0.04, 0.04, 0.04), col.rgb, metalness);
				//half3 F0 = lerp(half3(0.04, 0.04, 0.04), col.rgb, metalness);
				half3 fresnel = FresnelSchlick(LdotH, F0);
				half3 fresnel1 = FresnelSchlick(NdotV, F0);
				//NDF部分
				half SpecularDistribution = CookTorranceGGXNDF(NdotH, roughness)*lightingModel;
				//GSF部分
				half GeometricShadow = GeometrySmith(NdotV, NdotL, roughness);

				half3 KD = (1 - metalness);		
				//规范的GGX计算
				half3 specularity = (SpecularDistribution * fresnel * GeometricShadow ) / (4 * NdotV * NdotL + 0.0001);
				//specularity*= _LightColor0;
				//降低效果但速度更快的计算
				//float3 specularity = (SpecularDistribution * fresnel * GeometricShadow ) / (3.1415926);
				specularity += lerp(specularity, skyColor, fresnel);
				half3 skyreflection = skyColor * fresnel * roughness * 0.5;//校正天空盒反射
				//specularity += fresnel * _ReflectionIntensity * skyColor * (roughness);
				specularity += pow(NdotH, 50) * specularity * 50 * col * roughness;
				half3 indirectColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUV));//计算间接光采样
				fragColor.rgb = skyreflection + (specularity + KD * col)*(lightingModel * shadow + i.ambient);
				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
