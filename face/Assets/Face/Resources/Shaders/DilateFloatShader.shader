Shader "Hidden/DilateFloat"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
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
            float4 _MainTex_TexelSize;

            float4 frag (v2f i) : SV_Target
            {
                //min and max Vector
                 float2 _min = float2(0,0);
                 float2 _max = float2(1,1);
                 float _PixelOffset = _MainTex_TexelSize.x;

                 fixed4 finalColor = 0;
                 fixed4 curColor = tex2D(_MainTex, i.uv);
                 if(curColor.a == 0)
                 {
                    //get the color of 8 neighbour pixel
                    fixed4 U = tex2D(_MainTex,clamp(i.uv + float2(0,_PixelOffset),_min,_max));
                    fixed4 UR = tex2D(_MainTex,clamp(i.uv + float2(_PixelOffset,_PixelOffset),_min,_max));
                    fixed4 R = tex2D(_MainTex,clamp(i.uv + float2(_PixelOffset,0),_min,_max));
                    fixed4 DR = tex2D(_MainTex,clamp(i.uv + float2(_PixelOffset,-_PixelOffset),_min,_max));
                    fixed4 D = tex2D(_MainTex,clamp(i.uv + float2(0,-_PixelOffset),_min,_max));
                    fixed4 DL = tex2D(_MainTex,clamp(i.uv + float2(-_PixelOffset,-_PixelOffset),_min,_max));
                    fixed4 L = tex2D(_MainTex,clamp(i.uv + float2(-_PixelOffset,0),_min,_max));
                    fixed4 UL = tex2D(_MainTex,clamp(i.uv + float2(-_PixelOffset,_PixelOffset),_min,_max));

                    float count = 0;
                    if( U.a > 0) {count++; finalColor += U;}
                    if(UR.a > 0) {count++; finalColor += UR;}
                    if( R.a > 0) {count++; finalColor +=  R;}
                    if(DR.a > 0) {count++; finalColor += DR;}
                    if( D.a > 0) {count++; finalColor +=  D;}
                    if(DL.a > 0) {count++; finalColor += DL;}
                    if( L.a > 0) {count++; finalColor +=  L;}
                    if(UL.a > 0) {count++; finalColor += UL;}

                    //add all colors up to one final color
                    if(count > 0) finalColor /= count;
                 }
                 if(finalColor.a == 0)
                 {
                     finalColor = curColor;
                 }
                
                 //return final color
                 return finalColor;
            }
            ENDCG
        }
    }
}
