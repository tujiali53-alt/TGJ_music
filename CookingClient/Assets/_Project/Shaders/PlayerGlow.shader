Shader "Cooking/PlayerGlow"
{
    Properties
    {
        _GoldColor ("Gold", Color) = (1, 0.78, 0.28, 0.9)
        _PlayerPos ("Player", Vector) = (0, 0, 0, 0)
        _BedPos ("Bed", Vector) = (0, 0, 0, 0)
        _Radii ("Edge Width X", Vector) = (0.4, 2.5, 2.2, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "PlayerGlow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GoldColor;
                float4 _PlayerPos;
                float4 _BedPos;
                float4 _Radii;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                return output;
            }

            void Hole(float2 worldXY, float2 center, float radius, float sigma, out float darkness, out float gold)
            {
                float dist = distance(worldXY, center);
                float fromEdge = dist - radius;
                float denom = max(2.0 * sigma * sigma, 1e-5);
                float along = fromEdge * fromEdge;
                float outside = max(fromEdge, 0.0);
                darkness = 1.0 - exp(-(outside * outside) / denom);
                gold = fromEdge <= 0.0 ? exp(-along / denom) : 0.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float sigma = max(_Radii.x, 0.001);
                float playerDark, playerGold, bedDark, bedGold;
                Hole(input.positionWS.xy, _PlayerPos.xy, _Radii.y, sigma, playerDark, playerGold);
                Hole(input.positionWS.xy, _BedPos.xy, _Radii.z, sigma, bedDark, bedGold);

                float darkness = playerDark * bedDark;
                float gold = saturate(playerGold + bedGold);
                float3 rgb = _GoldColor.rgb * gold;
                float alpha = saturate(max(darkness, gold * _GoldColor.a));
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
