Shader "Hidden/RoundRect"
{
    Properties
    {
        _Rect ("Rect (x, y, w, h)", Vector) = (0,0,1,1)
        _Radius ("Radius", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always

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
            
            float4 _Rect;
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float udRoundBox(float2 p, float2 b, float r )
            {
              return length(max(abs(p)-b,0.0))-r;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x = _Rect.x, y = _Rect.y;
                float w = _Rect.z, h = _Rect.w;
                float mn = min(w, h);
                
                float2 p = float2(x, y);
                float2 size = float2(w, h);
                float2 extent = size/2;
                float2 center = p + extent;
                float c;
                if(all(abs(i.uv - center) > abs(p - center))){
                    float minDist = min(min(min(
                        distance(i.uv, float2(x+0,y+0)),
                        distance(i.uv, float2(x+w,y+0))),
                        distance(i.uv, float2(x+0,y+h))),
                        distance(i.uv, float2(x+w,y+h)));
                    c = 1 - saturate(minDist / _Radius.x);
                }
                else
                {
                    float2 minDist = saturate(abs(i.uv - center) - extent);
                    float2 tmp = minDist / _Radius;
                    c = 1 - saturate(max(tmp.x, tmp.y));
                }

                return c; 
            }
            ENDCG
        }
    }
}