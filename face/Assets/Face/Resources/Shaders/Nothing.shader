Shader "Hidden/Nothing"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass {
            ZWrite Off
            ColorMask 0
        }
    }
}
