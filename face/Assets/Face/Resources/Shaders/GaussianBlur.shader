Shader "Hidden/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        //CGINCLUDE中的代码可被其他Pass重复调用，用于简化不必要的重复代码
        CGINCLUDE

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            half2 uv[5] : TEXCOORD0;
            float4 pos : SV_POSITION;
        };

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _BlurSize;

        //用于计算纵向模糊的纹理坐标元素
        v2f vert_v(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            half2 uv = v.uv;

            //以扩散的方式对数组进行排序，只偏移y轴，其中1和2,3和4分别位于原始点0的上下，且距离1个单位和2个像素单位
            //得到的最终偏移与模糊范围的控制参数进行乘积
            o.uv[0] = uv;
            o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y*1.0)*_BlurSize;
            o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y*1.0)*_BlurSize;
            o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y*2.0)*_BlurSize;
            o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y*2.0)*_BlurSize;

            return o;
        }        
        
        //用于计算横向模糊的纹理坐标元素
        v2f vert_h(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            half2 uv = v.uv;

            //与上面同理，只不过是x轴向的模糊偏移
            o.uv[0] = uv;
            o.uv[1] = uv + float2( _MainTex_TexelSize.x*1.0,0.0)*_BlurSize;
            o.uv[2] = uv - float2( _MainTex_TexelSize.x*1.0,0.0)*_BlurSize;
            o.uv[3] = uv + float2( _MainTex_TexelSize.x*2.0,0.0)*_BlurSize;
            o.uv[4] = uv - float2( _MainTex_TexelSize.x*2.0,0.0)*_BlurSize;

            return o;
        }

        //在片元着色器中进行最终的模糊计算，此过程在每个Pass中都会进行一次计算，但计算方式是统一的
        fixed4 frag(v2f i) : SV_Target
        {
            float weights[3] = {0.4026,0.2442,0.0545};

            fixed4 col = tex2D(_MainTex, i.uv[0]);

            fixed2 sumCol = col.rg*weights[0];

            //对采样结果进行对应纹理偏移坐标的权重计算，以得到模糊的效果
            for (int it = 1; it < 3; it++) 
            {
                sumCol += tex2D(_MainTex, i.uv[2 * it - 1]).rg*weights[it];//对应1和3，也就是原始像素的上方两像素
                sumCol += tex2D(_MainTex, i.uv[2 * it]).rg*weights[it];//对应2和4，下方两像素
            }
            return fixed4(sumCol, 0, 1.0);
        }

        ENDCG

        ZTest Always
        Cull Off
        ZWrite Off

        //纵向模糊Pass，直接用指令调用上面的函数
        Pass
        {
            NAME "GAUSSIANBLUR_V"
            CGPROGRAM
            #pragma vertex vert_v
            #pragma fragment frag

            ENDCG
        }

        //横向模糊Pass
        Pass
        {
            NAME "GAUSSIANBLUR_H"
            CGPROGRAM
            #pragma vertex vert_h
            #pragma fragment frag

            ENDCG
        }
    }
    Fallback Off
}