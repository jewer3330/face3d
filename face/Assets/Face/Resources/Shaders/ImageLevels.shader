Shader "Hidden/ImageLevels"
{
    Properties
    {
		[NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset]_MaskTex("Mask", 2D) = "white" {}
        [PowerSlider(256)]_Gamma("Gamma", Range(0.0625, 16)) = 1
		_Blend("Blend", Range(0, 1)) = 1
        _Param("Param",Vector) = (0,1,0,1)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Tags {"PreviewType"="Plane"}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			sampler2D _MainTex;
			sampler2D _MaskTex;
            float _Gamma;
            float _Blend;
			float4 _Param;

            float remap(float c,float _min ,float _max)
            {
                _min = max(_min,0);
                _max = min(_max,1);
                if(_min > _max)
                   _min = _max;
                float len = _max - _min;
                return (c - _min) / len;
            }

            float cut_min(float c,float p)
            {
               //return step(p,c) * c;
               if(c < p)
                  return 0;
               else
                  return c;
            }

             float cut_max(float c,float p)
            {
               //return step(c,p) * c;
               if(c > p)
                  return 1;
               else
                  return c;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               fixed4 r = tex2D(_MainTex, i.uv);
	           fixed4 col = r;
	           fixed mask = tex2D(_MaskTex, i.uv).a * _Blend;
               col.r = cut_min(col.r,_Param.x);
               col.g = cut_min(col.g,_Param.x);
               col.b = cut_min(col.b,_Param.x);
               
               col.r = cut_max(col.r,_Param.y);
               col.g = cut_max(col.g,_Param.y);
               col.b = cut_max(col.b,_Param.y);
               
               col.r = remap(col.r ,_Param.x,_Param.y);
               col.g = remap(col.g ,_Param.x,_Param.y);
               col.b = remap(col.b ,_Param.x,_Param.y);

               col.r = lerp(_Param.z,_Param.w,col.r);
               col.g = lerp(_Param.z,_Param.w,col.g);
               col.b = lerp(_Param.z,_Param.w,col.b);
               col.rgb = pow(col, _Gamma).rgb;
               col.rgb = r.rgb * (1 - mask) + mask * col.rgb ;//lerp(col, pow(col, _Gamma).rgb, mask);
               return col;
            }
            ENDCG
        }
    }
}
