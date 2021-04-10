Shader "Clx/character/hairanisotropic"
{
	Properties
	{
		_Color("头发颜色", Color) = (1,1,1,1)
		[NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
		_SpecularColor1("第一层高光颜色", Color) = (1,1,1,1) 
		_SpecularColor2("第二层高光颜色", Color) = (1,1,1,1) 
		_Specular1("第一层高光强度", Range(0,1)) = 1
		_Specular2("第二层高光强度", Range(0,1)) = 1
		_SpecularRange1("第一层高光偏移", Range(-1,1)) = 0
		_SpecularRange2("第二层高光偏移", Range(-1,1)) = 0.5
		[NoScaleOffset]_NormalMap("(RG)法线贴图(B)高光蒙版", 2D) = "bump" {}
		_NormalScale("法线强度", Range(0, 1)) = 1
		[NoScaleOffset]_OffsetTex("OffsetTexture", 2D) = "black" {}
		_SpecularMultiplier1("第一层高光范围", Range(0, 100)) = 0
		_SpecularMultiplier2("第二层高光范围", Range(0, 100)) = 0
		_Cutoff("边缘距离", Range(0, 1)) = 0.5	//限制最大值影响ios bloom nan错误
		//[Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除模式", Float) = 1 
	}
	SubShader
	{
		Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "Ture" "RenderType" = "AlphaTest"}
		//Pass {
		//	ColorMask 0
		//	ZWrite On
		//}
		Pass
		{
			Tags{"LightMode"="ForwardBase" }
			ZWrite On 
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase	

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			//#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			fixed4 _MainTex_ST;
			fixed _Cutoff;

			struct VertexInput {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;				
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.uv;

				return o;
			}

			fixed4 frag(VertexOutput i) : SV_Target 
			{
				fixed4 fragColor = fixed4(1,1,1,1);
				//贴图采样
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				
				fragColor.rgb = mainTex.rgb;
				fragColor.a = mainTex.a;
				clip(fragColor.a - _Cutoff+0.001);
				return fragColor;
			}
			ENDCG
		}
		
		Pass
		{
			Tags{"LightMode"="ForwardBase"}
			ZWrite Off 
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase	

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			//#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			fixed4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _SpecularColor1;
			fixed4 _SpecularColor2;
			fixed _Specular1;
			fixed _Specular2;
			fixed _SpecularRange1;
			fixed _SpecularRange2;
			sampler2D _NormalMap;
			fixed4 _NormalMap_ST;
			fixed _NormalScale;
			sampler2D _OffsetTex;
			fixed4 _OffsetTex_ST;
			fixed _SpecularMultiplier1;
			fixed _SpecularMultiplier2;

			struct VertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;				
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				fixed3 diff : COLOR0;
				float3 ambient : COLOR1;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal :TEXCOORD2;
				float3 worldTangent : TEXCOORD3;
				float3 worldBinormal : TEXCOORD4;
				UNITY_FOG_COORDS(5)
				SHADOW_COORDS(6)
			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal (v.normal);
				o.worldTangent = normalize(mul(unity_ObjectToWorld, half4(v.tangent.xyz, 0)).xyz);
				o.worldBinormal = normalize(cross(o.worldNormal, o.worldTangent) * v.tangent.w);
				//half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				//half3 worldBitangent = cross(worldNormal, worldTangent) * tangentSign;

				//o.TtoW0 = half4(worldTangent.x, worldBitangent.x, worldNormal.x, worldPos.x);
				//o.TtoW1 = half4(worldTangent.y, worldBitangent.y, worldNormal.y, worldPos.y);
				//o.TtoW2 = half4(worldTangent.z, worldBitangent.z, worldNormal.z, worldPos.z);
				//环境光与灯光计算
				half hl = max(0, dot(o.worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = hl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));	//环境光计算
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			fixed4 frag(VertexOutput i) : SV_Target 
			{
				half4 fragColor = half4(1,1,1,1);
				//贴图采样
				fixed4 mainTex = tex2D(_MainTex, i.uv);
				mainTex.rgb *= _Color.rgb;
				fixed3 shiftTex = tex2D(_OffsetTex, i.uv);	//采样偏移贴图得到切线偏移
				fixed4 normalTex = tex2D(_NormalMap,i.uv);
				fixed3 normal = UnpackNormal(normalTex);
				normal.xy *= _NormalScale;
				normal.z = sqrt(1.0 - saturate(dot(normal.xy,normal.xy)));
				//转换世界坐标系下高光法线贴图
				i.worldNormal = normalize(i.worldNormal);
				float3x3 tangentTransform = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);	//切线到世界坐标系转换矩阵
				fixed3 normalDir = normalize(mul(normal, tangentTransform));				
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed3 halfDir = normalize(worldLightDir + viewDir);
				//计算高光偏移值 shiftT = Binormal + shift * N
				fixed3 t1 = normalize(i.worldBinormal + (_SpecularRange1 + shiftTex) * normalDir);
				fixed3 t2 = normalize(i.worldBinormal + (_SpecularRange2 + shiftTex) * normalDir);
				fixed TdotH1 = dot(t1, halfDir);
				fixed TdotH2 = dot(t2, halfDir);
				half3 spec1 = smoothstep(-1, 0, TdotH1) * pow(sqrt(1 - TdotH1 * TdotH1), _SpecularMultiplier1 * 10);
				half3 spec2 = smoothstep(-1, 0, TdotH2) * pow(sqrt(1 - TdotH2 * TdotH2), _SpecularMultiplier2 * 10);
				half3 spec = spec1 * _SpecularColor1 * _Specular1 + spec2 * _SpecularColor2 * _Specular2;
				spec *= normalTex.b;	//使用法线贴图蓝通道实现高光蒙版
				//阴影计算
				fixed shadow = SHADOW_ATTENUATION(i);
				//shadow *= _LightColor0.rgb ;
				half lightingModel = max(0,dot(normalDir, worldLightDir));		//光照模型
				
				fragColor.rgb = (mainTex.rgb + spec) * (lightingModel * shadow * i.diff + i.ambient);
				fragColor.a = mainTex.a;
				UNITY_APPLY_FOG(i.fogCoord, fragColor);
				return fragColor;
			}
			ENDCG
		}
		//UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		Pass {
			Tags { "LightMode" = "ShadowCaster"}//定义shadow pass用于处理半透明阴影正确渲染
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#pragma multi_compile_shadowcaster
			
			#include "UnityCG.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			fixed4 _MainTex_ST;
			fixed _Cutoff;
			fixed4 _NoiseAmount;

			struct VertexInput
			{
				fixed4 vertex : POSITION;
				fixed4 color : COLOR;
				fixed2 texcoord : TEXCOORD0;
				fixed3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID	//gpu instance
			};

			struct VertexOutput
			{
				V2F_SHADOW_CASTER;
				fixed4 color : COLOR0;
				fixed2 uv : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID	//gpu instance
			};
			
			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.color = v.color;
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)	//TRANSFER_SHADOW_CASTER_NOPOS(o,o.pos)

				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				
				return o;
			}
			
			fixed4 frag(VertexOutput i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				fixed4 mainTex = tex2D(_MainTex, i.uv) * _Color;
				
				clip(mainTex.a - _Cutoff+0.001);//使用clip函数处理alpha切边
				
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
