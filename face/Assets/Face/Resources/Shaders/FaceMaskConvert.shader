Shader "Unlit/FaceMaskConvert"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MaskTex("Mask Texture", 2D) = "white" {}
	}
    
     CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float2 uv4 : TEXCOORD3;
       
    }; 

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float2 uv4 : TEXCOORD3;
        float4 vertex : SV_POSITION;
    };
    
  

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    sampler2D _MaskTex;
    
    
    
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.uv4 = v.uv4;
        return o;
    }
    //和FaceMaskConvert1不同的地方，这个是取_MaskTex的Alpha
    fixed4 frag (v2f i) : COLOR
    {
       
    
        float4 col = tex2D(_MainTex, i.uv);
       
        //TODO dynamic mask 
        float4 msk = tex2D(_MaskTex, i.uv4);

        return float4(col.rgb, msk.a);
    }
    
      
                    
    ENDCG
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
       
        Cull Off ZWrite Off 
        
        
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDCG
        }
       
    }
	
}
