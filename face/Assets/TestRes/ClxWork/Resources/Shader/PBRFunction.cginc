#ifndef PBRFunction_INCLUDE
#define PBRFunction_INCLUDE
			//Cook - Torrance GGX NDF
			inline float CookTorranceGGXNDF(half3 NdotH, half roughness)
			{
				half roughnessSqr = roughness * roughness;
				half NdotHSqr = NdotH * NdotH;
				half denom = (NdotHSqr * (roughnessSqr - 1) + 1);
				denom = 3.1415926535 * denom * denom;
				return roughnessSqr / denom;
			}
			//高斯NDF
			inline float GaussianNDF(float NdotH, float roughness)
			{
				float roughnessSqr = roughness * roughness;
				float r2 = roughnessSqr * roughnessSqr;
				float thetaH = acos(NdotH);
				return exp(-thetaH * thetaH / roughness);
			}
			//SchlickGGX GSF
			inline half GeometrySchlickGGX(half NdotV, half roughness)
			{
				half r = (roughness + 1.0);
				half k = (r * r) / 8.0;
				half nom = NdotV;
				half denom = NdotV * (1 - k) + k;
				return nom / denom;
			}
			//GSF
			inline half GeometrySmith(half3 V, half3 L, half roughness)
			{
				half ggx2 = GeometrySchlickGGX(V, roughness);
				half ggx1 = GeometrySchlickGGX(L, roughness);
				return ggx2 * ggx1;
			}		
			//PHBeckmannGSF
			float PHBeckmann(float ndoth, float m)
			{
				float alpha = acos(ndoth);
				float ta = tan(alpha);
				float val = 1.0 / (m*m*pow(ndoth, 4.0))*exp(-(ta*ta) / (m*m));
				return val;
			}
			// Render a screen-aligned quad to precompute a 512x512 texture.
			float KSTextureCompute(float2 tex)
			{
				// Scale the value to fit within [0,1] – invert upon lookup.
				return 0.5 * pow(PHBeckmann(tex.x, tex.y), 0.1);
			}

			//float GetF0 (float NdotL, float NdotV, float LdotH, float roughness)
			//{
			//	float FresnelLight = pow(NdotL, 5); 
			//	float FresnelView = pow(NdotV, 5);
			//	float fd90 = 0.5 + 2.0 * LdotH*LdotH * roughness;
			//    return lerp(fd90, 1, FresnelLight) * lerp(fd90, 1, FresnelView);
			//}
			//Schlick Fresnel
			half3 FresnelSchlick(float cosTheta, half3 F0)
			{
				return F0 + (1 - F0) * pow(1 - cosTheta, 5);
			}
			//fresnelReflectance
			float fresnelReflectance(float3 H, float3 V, float F0) 
			{
				float base = 1.0 - dot(V, H);
				float exponential = pow(base, 5.0);
				return exponential + F0 * (1.0 - exponential);
			}
			//各向异性函数
			inline float AnisoFunction(half HdotA , half AnisoOffset, half roughness)
			{
				float aniso = max(0, sin(radians((HdotA  + AnisoOffset) * 180)));	
				return saturate(pow(aniso, roughness * 128));
			}
			//RGB转HSV
			inline half3 RGBConvertToHSV(float3 c)
			{
				fixed4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				fixed4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
				fixed4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

				fixed d = q.x - min(q.w, q.y);
				fixed e = 1.0e-10;
				return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);	
			}
			//HSV转RGB
			inline half3 HSVConvertToRGB(float3 c)
			{
				fixed4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);	
			}
			//HSV去色函数
			inline half4 TexHSV(float4 c, float h, float s, float a)
			{
				c.rgb = RGBConvertToHSV(c.rgb);
				c.r += h;
				c.g *= s * 3;
				c.rgb = HSVConvertToRGB(c.rgb);
				c.a *= a;		
				return c;
			}
			//生成随机数
			inline float2 random(float2 st){
				float2 k = float2(0.3183099, 0.3678794);
				st = st * k + k.yx;
				return -1 + 2 * frac(16 * k * frac(st.x * st.y * (st.x + st.y)));
			}
            //perlin noise
            inline float noise (float2 st) {
                float2 i = floor(st);
                float2 f = frac(st);

                float2 u = f*f*(3.0-2.0*f);

                return lerp( lerp( dot( random(i), f),
                                 dot( random(i + float2(1.0,0.0) ), f - float2(1.0,0.0) ), u.x),
                            lerp( dot( random(i + float2(0.0,1.0) ), f - float2(0.0,1.0) ),
                                 dot( random(i + float2(1.0,1.0) ), f - float2(1.0,1.0) ), u.x), u.y);
            }
			//旋转cubemap角度
			inline half3 RotateAroundYInDegrees(half3 dir, half deg)
			{
				half rad = deg * UNITY_PI / 180.0;
				half sina, cosa;
				sincos(rad, sina, cosa);
				half2x2 m = half2x2(cosa, -sina, sina, cosa);
				return half3(mul(m, dir.xz), dir.y).xzy;
			}
			//随机梯度
			inline float2 hash22(float2 p)
			{
				p = float2(dot(p, float2(127.1, 311.7)),
					dot(p, float2(269.5, 183.3)));
 
				return -1.0 + 2.0 * frac( sin(p) * 43758.5453123);
			}
			//perlin noise
			inline float perlin_noise(float2 p)
			{
				float2 i = floor(p.xy);
				float2 f = frac(p.xy);
 
				////早期平滑曲线函数 
				//vec2 u = f*f*(3.0-2.0*f);  
				float2 u = f*f*f*(6.0*f*f - 15.0*f + 10.0);
 
				return lerp(lerp(dot(hash22(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
					dot(hash22(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
					lerp(dot(hash22(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
						dot(hash22(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
			}
#endif