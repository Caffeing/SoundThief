Shader "SoundThief/JinHo/PlanarMirror"
{
    Properties { _MirrorTex("Reflection",2D)="black"{} }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MirrorTex);SAMPLER(sampler_MirrorTex);
            CBUFFER_START(UnityPerMaterial) float4x4 _MirrorVP; CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float4 screen:TEXCOORD0;};
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.screen=mul(_MirrorVP,float4(TransformObjectToWorld(v.positionOS.xyz),1));return o;}
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.screen.xy/i.screen.w*.5+.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y=1-uv.y;
                #endif
                return half4(SAMPLE_TEXTURE2D(_MirrorTex,sampler_MirrorTex,uv).rgb,1);
            }
            ENDHLSL
        }
    }
}
