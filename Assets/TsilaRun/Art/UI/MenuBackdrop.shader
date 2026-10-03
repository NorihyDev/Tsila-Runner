Shader "TsilaRun/MenuBackdrop"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input v) { Output o; o.position=TransformObjectToHClip(v.position.xyz); o.uv=v.uv; return o; }
            half4 frag(Output v) : SV_Target
            {
                float2 p=(v.uv-float2(.5,.51))*float2(1.2,1);
                float halo=exp(-dot(p,p)*55);
                half3 navy=lerp(half3(.001,.002,.006),half3(.004,.01,.024),v.uv.y);
                return half4(navy+halo*half3(.003,.05,.018),1);
            }
            ENDHLSL
        }
    }
}
