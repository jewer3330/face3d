Shader "Clx/character/facedecal"
{
	Properties
	{
		_Color ("皮肤颜色", Color) = (1,1,1,1)
		[NoScaleOffset]_MainTex("皮肤纹理1UV", 2D) = "white" {}
		[NoScaleOffset]_NormalMap("法线贴图",2D) = "bump"{}
		_NormalAmount("法线强度",Range(0,2)) = 1
		[NoScaleOffset]_MaskTex("AO(R)粗糙度(G)金属度(B)",2D) = "white"{}

		_Occlusion("AO",Range(0,1)) = 0.5
		[NoScaleOffset]_SSSLUT("SSSLUT",2D) = "white"{}
		_Cureve("光线弯曲值",Range(0,1)) = 1
		_Metalness("金属度",Range(0,1)) = 0
		_Roughness("粗糙度",Range(0,1)) = 1
		_Brightness("明度",Range(0,1)) = 0

		[NoScaleOffset]_Tex1("眉部2UV", 2D) = "black" {}
		_Offset2Y ("上下调整", Range(-0.05, 0.05)) = 0
		_Hue1("颜色", Range(0, 1)) = 0
        _Saturation1("饱和", Range(0, 1)) = 0.333
		_Alpha1("浓淡", Range(0, 1)) = 0.85

		[NoScaleOffset]_Tex2("嘴部3UV", 2D) = "black" {}
		[NoScaleOffset]_Normal2("嘴部法线3UV",2D) = "bump"{}
		_Normal2Scale("法线强度",Range(0,2)) = 1
		_Hue2("颜色", Range(0, 1)) = 0
        _Saturation2("饱和", Range(0, 1)) = 0.333
		_Alpha2("浓淡", Range(0, 1)) = 0.85

		[NoScaleOffset]_Tex3("贴花1UV", 2D) = "black" {}
		_Offset3X ("左右调整", Range(-0.2, 0.2)) = 0
		_Offset3Y ("上下调整", Range(-0.2, 0.2)) = 0
		_Hue3("颜色", Range(0, 1)) = 0
        _Saturation3("饱和", Range(0, 1)) = 0.333
		_Alpha3("浓淡", Range(0, 1)) = 0.85

		[NoScaleOffset]_Tex4("眼妆1UV", 2D) = "black" {}
		[NoScaleOffset]_Mask4("AO(R)粗糙度(G)金属度(B)",2D) = "white"{}
		_Hue4("颜色", Range(0, 1)) = 0
        _Saturation4("饱和", Range(0, 1)) = 0.333
		_Alpha4("浓淡", Range(0, 1)) = 0.85

		//[NoScaleOffset]_Tex5("老年斑1UV", 2D) = "black" {}
		//[NoScaleOffset]_Normal3("老年斑法线1UV",2D) = "bump"{}
		//_Normal3Scale("法线强度",Range(0,2)) = 1
		//_Hue8("颜色", Range(0, 1)) = 0
		//_Saturation8("饱和", Range(0, 1)) = 0.333
		//_Alpha5("浓淡", Range(0, 1)) = 0.85

		[NoScaleOffset]_ShiningMask("珠粉遮罩(R)嘴唇(G)脸颊(B)眼妆", 2D) = "black" {}
		[NoScaleOffset]_ShiningTex("珠粉贴图", 2D) = "black" {}

		_ShiningScale1("嘴唇珠粉缩放", Range(10, 50)) = 10
		_Hue5("嘴唇珠粉颜色", Range(0, 1)) = 0
        _Saturation5("嘴唇珠粉饱和", Range(0, 0.32)) = 0.2
		_Brightness1("嘴唇珠粉明度",Range(0,1)) = 1
		_ShiningScale2("脸颊珠粉缩放", Range(1, 10)) = 10
		_Hue6("脸颊珠粉颜色", Range(0, 1)) = 0
        _Saturation6("脸颊珠粉饱和", Range(0, 0.32)) = 0.2
		_Brightness2("脸颊珠粉明度",Range(0, 0.5)) = 0.5
		_ShiningScale3("眼妆珠粉缩放", Range(10, 50)) = 10
		_Hue7("眼妆珠粉颜色", Range(0, 1)) = 0
        _Saturation7("眼妆珠粉饱和", Range(0, 0.32)) = 0.2
		_Brightness3("眼妆珠粉明度",Range(0, 1)) = 1

		[NoScaleOffset]_WireTex ("高亮贴图", 2D) = "white" {}
		_Area1 ("分区切换区域", Range(0,255)) = 10
		_Area2 ("整体切换区域", Range(0,255)) = 10
		[MaterialToggle]_Swicth ("切换开关", Float) = 0
		_Clip ("边缘裁切", Range(0, 1)) = 0.3
		_WireBrightness ("亮度", Range(0, 1)) = 0.5
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
			#pragma multi_compile _ ARFACE_BAKE_MAT

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#include "PBRFunction.cginc"

			#pragma multi_compile _ ARFACE

			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
#if ARFACE
				float2 uv4 : TEXCOORD4;
#endif
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				float4 uv1 : TEXCOORD9;
#if ARFACE
				float2 uvMainTex : TEXCOORD10;
#endif
				UNITY_FOG_COORDS(1)
#if ARFACE_BAKE_MAT
				float4 uvPos : SV_POSITION;
				float4 pos : TEXCOORD11;
#else
				float4 pos : SV_POSITION;
#endif
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float3 worldNormal :TEXCOORD4;
				float3 worldTangent : TEXCOORD5;
				float3 worldBinormal : TEXCOORD6;
				float3 worldPos : TEXCOORD8;
				SHADOW_COORDS(7)
			};

			sampler2D _MainTex;
			sampler2D _Tex1, _Tex2, _Tex3, _Tex4;
			sampler2D _ShiningTex,_ShiningMask, _NormalMap, _Normal2, _Mask4, _MaskTex, _SSSLUT, _WireTex;
			fixed _Offset3X, _Offset2Y, _Offset3Y;
			fixed _NormalAmount, _Normal2Scale;
			fixed _Metalness, _Roughness, _Occlusion, _Cureve;
			fixed4 _Color, _SSSLUTColor;
			fixed _Brightness, _Brightness1, _Brightness2, _Brightness3;
			fixed _Hue1, _Hue2, _Hue3, _Hue4, _Hue5, _Hue6, _Hue7;
			fixed _Saturation1, _Saturation2, _Saturation3, _Saturation4, _Saturation5, _Saturation6, _Saturation7;
			fixed _Alpha1, _Alpha2, _Alpha3, _Alpha4;
			int _Area1, _Area2;
			fixed _Clip, _WireBrightness, _Swicth;
			fixed _ShiningScale1, _ShiningScale2, _ShiningScale3;
			//fixed4 _ShiningColor1;

			float4 _SetCamera;

			VertexOutput vert (VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;
				o.pos = UnityObjectToClipPos(v.vertex);
#if ARFACE_BAKE_MAT
				o.uvPos = UnityObjectToClipPos(float4(v.uv, 0, 1));
#endif
#if ARFACE
				// arface除mainTex外都使用映射后的texcoord4
				o.uv = v.uv4;
				o.uvMainTex = v.uv;
#else
				o.uv = v.uv;
#endif
				o.uv1.xy = v.uv1;
				o.uv1.zw = v.uv2;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTangent = UnityObjectToWorldDir(v.tangent);
				o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);
				//环境光与灯光计算
				half nl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb*0.5 * (1 - _Brightness) + _Brightness;	//灯光校正弱化皮肤明暗
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));

				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (VertexOutput i) : SV_Target
			{
				half4 fragColor = half4(1,1,1,1);
#if ARFACE_BAKE_MAT
				_WorldSpaceCameraPos.xyz = _SetCamera.xyz;
#endif
#if ARFACE
				fixed4 mainTex = tex2D(_MainTex, i.uvMainTex) * _Color;
#else
				fixed4 mainTex = tex2D(_MainTex, i.uv) * _Color;
#endif
				fixed4 t1 = tex2D(_Tex1, i.uv1.xy + fixed2(0, _Offset2Y));
				fixed4 t2 = tex2D(_Tex2, i.uv1.zw);
				fixed4 t3 = tex2D(_Tex3, i.uv + fixed2(_Offset3X, _Offset3Y));
				fixed4 t4 = tex2D(_Tex4, i.uv);
				//fixed4 t5 = tex2D(_Tex5, i.uv);
				fixed4 tex1 = TexHSV(t1, _Hue1, _Saturation1, _Alpha1);
				fixed4 tex2 = TexHSV(t2, _Hue2, _Saturation2, _Alpha2);
				fixed4 tex3 = TexHSV(t3, _Hue3, _Saturation3, _Alpha3);
				fixed4 tex4 = TexHSV(t4, _Hue4, _Saturation4, _Alpha4);
				//fixed4 tex5 = TexHSV(t5, _Hue8, _Saturation8, _Alpha5);
				mainTex.rgb = lerp(mainTex.rgb, tex1.rgb, tex1.a);
				mainTex.rgb = lerp(mainTex.rgb, tex2.rgb, tex2.a);
				mainTex.rgb = lerp(mainTex.rgb, tex3.rgb, tex3.a);
				mainTex.rgb = lerp(mainTex.rgb, tex4.rgb, tex4.a);
				//mainTex.rgb = lerp(mainTex.rgb, tex5.rgb, tex5.a);

				fixed4 m4 =tex2D(_Mask4, i.uv);
				m4.g = pow(m4.g, 1.5);
#if ARFACE
				fixed4 maskTex = tex2D(_MaskTex, i.uvMainTex);
#else
				fixed4 maskTex = tex2D(_MaskTex, i.uv);
#endif
				maskTex.rgb = lerp(maskTex.rgb, m4.rgb, tex4.a);
				//ao
				mainTex.rgb = lerp(mainTex.rgb, mainTex.rgb * maskTex.r, _Occlusion);
				// 法线
#if ARFACE
				fixed3 normalMap = UnpackNormal(tex2D(_NormalMap,i.uvMainTex));	
#else
				fixed3 normalMap = UnpackNormal(tex2D(_NormalMap,i.uv));	
#endif
				fixed3 normal2 = UnpackNormal(tex2D(_Normal2,i.uv1.zw));
				//normal2.xyz = normal2.xyz * 2 - 1;	//自定义采样法线蒙版贴图法线部分
				normal2.xy *= _Normal2Scale;	
				//normal2 *= normal2.a;
				//fixed3 normal3 = UnpackNormal(tex2D(_Normal3, i.uv));
				//normal3.xy *= _Normal3Scale;
				//normalMap = lerp(normalMap, normal3, tex5.a);
				normalMap += normal2;
				fixed3 normal = normalMap;
				normal.xy *= _NormalAmount;
				//normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));	
				i.worldNormal = normalize(i.worldNormal);
				float3x3 tangentTransform = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
				fixed3 normalDirection = normalize(mul(normal, tangentTransform));
				//灯光方向 实现方法 半角向量
				//float3 worldLightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
				//非标准化半角向量
				fixed3 h = (viewDir + worldLightDir);
				fixed3 halfDir = normalize(h);
				//粗糙度 金属度
				float metalness = _Metalness * maskTex.b;
				float roughness = _Roughness * maskTex.g;
				//环境反射
				half3 reflectDirection = normalize(reflect(-viewDir, normalDirection));
				half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDirection);
				half3 skyColor = DecodeHDR(skyData,unity_SpecCube0_HDR) ;
				//Lambert光照模型
				half lightingModel = max(0, dot(normalDirection, worldLightDir));
				//灯光 视线 法线 方向计算
				fixed NdotH = max(0, dot(normalDirection, halfDir));
				fixed NdotL = max(0, dot(normalDirection, worldLightDir));
				fixed NdotV = max(0, dot(normalDirection, viewDir));
				fixed LdotH = max(0, dot(worldLightDir, halfDir));
				fixed VdotH = max(0, dot(viewDir, halfDir));
				//SSSLUT
				fixed4 sssLUT = tex2D(_SSSLUT, float2(VdotH * 0.5, _Cureve));
				//Fresnel 皮肤参数
				half3 F0 = lerp(half3(0.028, 0.028, 0.028), mainTex.rgb, metalness);
				half3 fresnel = FresnelSchlick(LdotH, F0);
				half F90 = 0.5 + 2 * roughness * pow(LdotH, 2);
				half3 KD = (1 + (F90 -1) * pow((1 - LdotH), 5) * (1 + (F90 - 1) * pow(1 - NdotV, 5)));
				KD *= 1 - metalness;
				//阴影计算
				fixed shadow = SHADOW_ATTENUATION(i)*_LightColor0;
				//NDF
				half SpecularDistribution = CookTorranceGGXNDF(NdotH, roughness)*lightingModel;
				//GSF
				half GeometricShadow = GeometrySmith(NdotV, NdotL, roughness);
				//高光
				half3 specularity = (SpecularDistribution * fresnel * GeometricShadow) / (4 * NdotV * NdotL + 0.0001);
				specularity *= (1 - roughness) * 10;	//粗糙度高光风格化

				fixed4 shiningmask = tex2D(_ShiningMask, i.uv);
				fixed4 shiningcolr = TexHSV(fixed4(1,0,0,1), _Hue5, _Saturation5, 1) * _Brightness1 * lightingModel;
				fixed4 shiningtexr = tex2D(_ShiningTex, i.uv * _ShiningScale1) * shiningcolr * 10;
				fixed4 shiningcolg = TexHSV(fixed4(1,0,0,1), _Hue6, _Saturation6, 1) * _Brightness2 * lightingModel;
				fixed4 shiningtexg = tex2D(_ShiningTex, i.uv * _ShiningScale2) * shiningcolg * 10;
				fixed4 shiningcolb = TexHSV(fixed4(1,0,0,1), _Hue7, _Saturation7, 1) * _Brightness3 * lightingModel;
				fixed4 shiningtexb = tex2D(_ShiningTex, i.uv * _ShiningScale3) * shiningcolb * 10;
				fixed4 shiningtex = shiningtexr * shiningmask.r + shiningtexg * shiningmask.g + shiningtexb * shiningmask.b;
				specularity += shiningtex;

				//高亮显示部分
				float4 wiretex = tex2D(_WireTex,i.uv);
				float3 wirer = float3(wiretex.b, wiretex.b, wiretex.b);
				float3 wirea = float3(wiretex.a, wiretex.a, wiretex.a);
				float3 wire = float3(lerp(wirer, wirea, _Swicth));
				float4 wirecol = float4(1,1,1,1);
				wirecol.r = smoothstep( 0,abs(wiretex.r*255-_Area1)+0.0001, 2);
				wirecol.g = smoothstep( 0,abs(wiretex.g*255-_Area2)+0.0001, 2);
				fixed c = lerp(wirecol.r, wirecol.g, _Swicth);
				c = step(_Clip - c, 0.001);
				fixed3 wirecolor = float3(c,c,c);
				wirecolor *= wire * _WireBrightness;
				//灯光校正弱化皮肤明暗
				fragColor.rgb =  (specularity*lightingModel+ KD * mainTex.rgb) * (sssLUT.rgb + lightingModel * shadow *0.5 * (1 - _Brightness) + _Brightness + i.ambient);
				//叠加高亮显示
				fragColor.rgb += wirecolor;

				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
