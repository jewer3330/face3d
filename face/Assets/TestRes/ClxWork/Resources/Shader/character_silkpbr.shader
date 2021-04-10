Shader "Clx/character/silkpbr"
{
	Properties
	{
		_Color1 ("TintColor1", Color) = (1,1,1,1)
		_Hue1("颜色1", Range(0, 1)) = 0
        _Saturation1("饱和1", Range(0, 1)) = 0.333
		_Brightness1("明度1", Range(0, 1)) = 0.85
		_Color2 ("TintColor2", Color) = (1,1,1,1)
		_Hue2("颜色2", Range(0, 1)) = 0
        _Saturation2("饱和2", Range(0, 1)) = 0.333
		_Brightness2("明度2", Range(0, 1)) = 0.85
		_MainTex ("MainTex", 2D) = "white" {}
		[NoScaleOffset]_NormalMap("(RG)法线贴图(B)换色蒙版", 2D) = "bump"{}
		_NormalScale("法线强度",Range(0,2)) = 1
		[NoScaleOffset]_MaskTex("AO(R)粗糙度(G)金属度(B)丝绸遮罩(A)",2D) = "white"{}
		_Occlusion("AO",Range(0,1)) = 1
		_Metalness("金属度",Range(0,1)) = 1
		_Roughness("粗糙度",Range(0,1)) = 1
		_Specular("高光强度", Range(0,1)) = 1
		_SpecularColor1("高光颜色1", Color) = (1,1,1,1) 
		_SpecularColor2("高光颜色2", Color) = (1,1,1,1)
		_SpecularColor3("高光颜色3", Color) = (1,1,1,1)
		//_SpecularPower1("Specular Power1", Range(0,1)) = 0.5
		//_SpecularPower2("Specular Power2", Range(0,1)) = 0.5
		//_SpecularPower3("Specular Power3", Range(0,1)) = 0.5
		_AnisoOffset1("高光偏移1", Range(-1,1)) = -0.2
		_AnisoOffset2("高光偏移2", Range(-1,1)) = 0.2
		_AnisoOffset3("高光偏移3", Range(-1,1)) = 0.4
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Float) = 2 
	}
	SubShader
	{
		Tags {"Queue" = "Geometry" "RenderType"="Opaque"}

		//Pass{
		//	ZWrite On 
		//	ColorMask 0
		//	}

		Pass
		{
			Tags{"LightMode"="ForwardBase"}

			Cull [_Cull]
			//ZWrite Off 
			//Cull Off
			//Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase			

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "UnityStandardUtils.cginc"
			#include "PBRFunction.cginc"

			fixed4 _Color1, _Color2;
			fixed _Hue1, _Hue2, _Saturation1, _Saturation2, _Brightness1, _Brightness2;
			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _NormalMap;
			fixed4 _MainTex_ST;
			fixed _NormalScale;
			fixed _Metalness;
			fixed _Roughness;
			fixed _Occlusion;
			fixed4 _SpecularColor1;
			fixed4 _SpecularColor2;
			fixed4 _SpecularColor3;
			fixed _Specular;
			//fixed _SpecularPower1;
			//fixed _SpecularPower2;
			//fixed _SpecularPower3;
			fixed _AnisoOffset1;
			fixed _AnisoOffset2;
			fixed _AnisoOffset3;

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct VertexOutput
			{
				float4 pos : SV_POSITION;
				float3 ambient : COLOR0;
				float3 diff :COLOR1;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal :TEXCOORD2;
				float3 worldTangent : TEXCOORD3;
				float3 worldBinormal : TEXCOORD4;
				UNITY_FOG_COORDS(5)
				SHADOW_COORDS(6)
			};

			VertexOutput vert (VertexInput v)
			{
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTangent = normalize(mul(unity_ObjectToWorld, half4(v.tangent.xyz, 0)).xyz);
				o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);
				half hl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = hl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));	//环境光计算
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (VertexOutput i) : SV_Target
			{
				half4 fragColor = half4(1,1,1,1);
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				fixed4 maskTex = tex2D(_MaskTex, i.uv);	//遮罩图采样
				fixed3 col = lerp(mainTex.rgb, mainTex.rgb * maskTex.r, _Occlusion);
				fixed4 normalmask = tex2D(_NormalMap, i.uv);
				fixed3 normal = normalmask.xyz * 2 - 1;	//自定义采样法线蒙版贴图法线部分
				normal.xy *= _NormalScale ;
				fixed colmask = normalmask.z;
				normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));	
				fixed4 hsvcol1 = TexHSV(_Color1, _Hue1, _Saturation1, _Brightness1)*_Brightness1;
				fixed4 hsvcol2 = TexHSV(_Color2, _Hue2, _Saturation2, _Brightness2)*_Brightness2;
				fixed4 hsvcol = lerp(hsvcol1, hsvcol2, colmask);	//换色混合
				col *= hsvcol;

				i.worldNormal = normalize(i.worldNormal);
				float3x3 tangentTransform = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);	//切线到世界坐标系转换矩阵
				fixed3 normalDir = normalize(mul(normal, tangentTransform));

				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed3 halfDir = normalize(worldLightDir + viewDir);
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
				fixed HdotA  = dot(normalize(i.worldNormal + normalDir), halfDir);	//计算偏移法线向量
				float LdotH = max(0, dot(worldLightDir, halfDir));
				//阴影计算
				fixed3 shadow = SHADOW_ATTENUATION(i) * _LightColor0;
				half lightingModel = max(0,dot(normalDir, worldLightDir));		//光照模型
				//菲涅尔部分
				half3 F0 = lerp(half3(0.04, 0.04, 0.04), col.rgb, metalness);
				half3 fresnel = FresnelSchlick(LdotH, F0);
				half3 fresnel1 = FresnelSchlick(NdotV, F0);
				//NDF部分
				fixed SpecularDistribution = CookTorranceGGXNDF(NdotH, roughness)*lightingModel;
				//GSF部分
				fixed GeometricShadow = GeometrySmith(NdotV, NdotL, roughness);
				//规范的GGX计算
				fixed3 pbrspec = (SpecularDistribution * fresnel * GeometricShadow )  / (4 * NdotV * NdotL + 0.0001);
				//降低效果但速度更快的计算
				//float3 specularity = (SpecularDistribution * fresnel * GeometricShadow ) / (3.1415926);
				pbrspec += lerp(pbrspec, skyColor , fresnel);
				pbrspec += pow(NdotH, 50) * pbrspec * 50 * col * roughness;
				half3 skyreflection = skyColor * fresnel * roughness * 0.5;//校正天空盒反射
				//计算高光偏移
				fixed spec1 = AnisoFunction(HdotA , _AnisoOffset1, roughness);
				fixed spec2 = AnisoFunction(HdotA , _AnisoOffset2, roughness);
				fixed spec3 = AnisoFunction(HdotA , _AnisoOffset3, roughness);
				fixed3 anisospec = spec1 * _SpecularColor1 + spec2 * _SpecularColor2 + spec3 * _SpecularColor3;
				anisospec *= _Specular;

				//fixed3 anisospecblend = anisospec * hsvcol;
				//anisospec = lerp(anisospec, anisospecblend, colmask);

				// anisospec *= skyColor;	//各向异性高光是否反射天空盒
				
				fixed3 KD = (1 - metalness);
				//fixed3 KD = (1 - fresnel) * (1 - metalness);	//diffuse部分系数	
				half3 specularity = pbrspec * maskTex.a + anisospec *(1 - maskTex.a);
				fragColor.rgb = skyreflection + (specularity + KD * col ) * (shadow * lightingModel + i.ambient);
				//fragColor.rgb = (specularity + KD * col ) *  (lightingModel * shadow * i.diff + i.ambient);
				
				//fragColor.a = mainTex.a;
				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	//FallBack "Mobile/VertexLit"
}
