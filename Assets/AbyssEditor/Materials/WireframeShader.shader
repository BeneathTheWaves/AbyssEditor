// Wireframe Shader
// Based on https://github.com/Firnox/URP_Wireframe_Shader
//
// Bake barycentric UVs at import time (recommended):
//   Attach the BarycentricBaker.cs script (included below) to any mesh import
//   post-processor. It writes (1,0,0), (0,1,0), (0,0,1) into uv2 per triangle.
//   For quads: the diagonal vertex gets its barycentric component set to 2.0
//   so the fragment shader can suppress that internal edge.

Shader "Unlit/WireframeShader"
{
    Properties
    {
        _MainTex        ("Texture",          2D)     = "white" {}
        _WireframeColor ("Wireframe color",  Color)  = (1, 1, 1, 1)
        _WireframeScale ("Wireframe scale",  Float)  = 1.5

        [KeywordEnum(BASIC, FIXEDWIDTH, ANTIALIASING)]
        _WIREFRAME      ("Wireframe rendering type", Integer) = 1

        [Toggle]
        _QUADS          ("Show only quads",          Integer) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma shader_feature _WIREFRAME_BASIC _WIREFRAME_FIXEDWIDTH _WIREFRAME_ANTIALIASING
            #pragma shader_feature _QUADS_ON

            // Metal / Apple Silicon safe — no geometry shader
            //#pragma exclude_renderers d3d11 vulkan   // optional: restrict to Metal only

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _WireframeColor;
                float  _WireframeScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                // Barycentric coords baked into UV2 by BarycentricBaker.cs
                // x,y,z = barycentric; w = diagonal flag (1 = suppress edge)
                float4 barycentric : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 barycentric : TEXCOORD1;   // xyz = bary, w = diagonal flag
                float  fogFactor   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS  = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.barycentric = IN.barycentric;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            
            half4 frag(Varyings IN) : SV_Target
            {
                float3 bary = IN.barycentric.xyz;

                // _QUADS_ON: suppress the longest (diagonal) edge of each quad.
                // BarycentricBaker sets the w channel to 1.0 on the diagonal vertex,
                // which bumps its barycentric component above 1 so it never triggers.
                #if _QUADS_ON
                    if (IN.barycentric.w > 0.5)
                    {
                        int diagIdx = (int)(IN.barycentric.w + 0.5) - 1; // 0,1,2 — which component to suppress
                        if      (diagIdx == 0) bary.x = 1.0;
                        else if (diagIdx == 1) bary.y = 1.0;
                        else                   bary.z = 1.0;
                    }
                #endif

                float alpha = 0.0;

                #if _WIREFRAME_BASIC
                {
                    float closest = min(bary.x, min(bary.y, bary.z));
                    alpha = step(closest, _WireframeScale / 20.0);
                }
                #elif _WIREFRAME_FIXEDWIDTH
                {
                    float3 unitWidth = fwidth(bary);
                    float3 edge      = step(bary, unitWidth * _WireframeScale);
                    alpha = max(edge.x, max(edge.y, edge.z));
                }
                #elif _WIREFRAME_ANTIALIASING
                {
                    float3 unitWidth = fwidth(bary);
                    float3 aliased   = smoothstep(float3(0, 0, 0), unitWidth * _WireframeScale, bary);
                    alpha = 1.0 - min(aliased.x, min(aliased.y, aliased.z));
                }
                #endif

                half4 col = half4(_WireframeColor.rgb, alpha);  // remove the * _WireframeColor.a
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
}
