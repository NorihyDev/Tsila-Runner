Shader "TsilaRun/Sand"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 position : POSITION; };
            struct Output { float4 position : SV_POSITION; float3 world : TEXCOORD0; };
            Output vert(Input v) { Output o; o.world=TransformObjectToWorld(v.position.xyz); o.position=TransformWorldToHClip(o.world); return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            half4 frag(Output v) : SV_Target
            {
                float ripple=noise(v.world.xz*float2(.3,1.7));
                float grain=noise(v.world.xz*45);
                float fade=saturate(1-length(fwidth(v.world.xz))*45);
                half3 sand=lerp(half3(.45,.31,.17),half3(.66,.5,.29),ripple*.35+.4);
                sand*=1+(grain-.5)*.07*fade;
                Light sun=GetMainLight(); sand*=.65+.35*saturate(sun.direction.y);
                return half4(sand,1);
            }
            ENDHLSL
        }
    }
}
