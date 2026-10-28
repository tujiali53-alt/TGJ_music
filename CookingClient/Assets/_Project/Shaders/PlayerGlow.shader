Shader "Cooking/PlayerGlow"
{
    Properties
    {
        _InnerColor ("内层", Color) = (1, 0.96, 0.82, 0.22)
        _GoldColor ("金边", Color) = (1, 0.86, 0.58, 0.47)
        _OuterColor ("外层", Color) = (1, 0.55, 0.16, 0.5)
        _EmberColor ("最外层", Color) = (0.62, 0.16, 0.04, 0.4)
        _PlayerPos ("Player", Vector) = (0, 0, 0, 0)
        _BedPos ("Bed", Vector) = (0, 0, 0, 0)
        _Radii ("Edge Width X", Vector) = (0.4, 2.5, 2.2, 0)
        _LayerWidths ("层宽 内金外末", Vector) = (1.45, 0.85, 1.05, 1.25)
        _InnerSpan ("内层起止", Vector) = (0.42, 0.78, 0, 0)
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
                float4 _InnerColor;
                float4 _GoldColor;
                float4 _OuterColor;
                float4 _EmberColor;
                float4 _PlayerPos;
                float4 _BedPos;
                float4 _Radii;
                float4 _LayerWidths;
                float4 _InnerSpan;
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

            float Band(float fromEdge, float center, float width)
            {
                float delta = fromEdge - center;
                float denom = max(2.0 * width * width, 1e-5);
                return exp(-(delta * delta) / denom);
            }

            void Hole(float2 worldXY, float2 center, float radius, float sigma, out float darkness, out float glowAlpha, out float3 glowColor)
            {
                float dist = distance(worldXY, center);
                float fromEdge = dist - radius;
                float s = max(sigma, 0.001);

                float4 width = max(_LayerWidths, 0.05);
                float inner = Band(fromEdge, -1.2 * s, width.x * s);
                float gold = Band(fromEdge, -0.2 * s, width.y * s);
                float outer = Band(fromEdge, 0.7 * s, width.z * s);
                float ember = Band(fromEdge, 1.7 * s, width.w * s);

                float innerA = inner * _InnerColor.a;
                float goldA = gold * _GoldColor.a;
                float outerA = outer * _OuterColor.a;
                float emberA = ember * _EmberColor.a;
                float weight = innerA + goldA + outerA + emberA;

                glowAlpha = saturate(1.0 - (1.0 - saturate(innerA)) * (1.0 - saturate(goldA)) * (1.0 - saturate(outerA)) * (1.0 - saturate(emberA)));
                glowColor = weight > 0.001
                    ? ( _InnerColor.rgb * innerA + _GoldColor.rgb * goldA + _OuterColor.rgb * outerA + _EmberColor.rgb * emberA) / weight
                    : float3(0.0, 0.0, 0.0);

                // X is where the inner color starts, Y is where it becomes full. Both are fractions of the radius.
                float spanStart = min(_InnerSpan.x, _InnerSpan.y);
                float spanEnd = max(_InnerSpan.x, _InnerSpan.y);
                glowAlpha *= smoothstep(radius * spanStart, radius * spanEnd, dist);

                float outside = max(fromEdge - 0.45 * s, 0.0);
                float darkSigma = max(2.4 * s, 0.05);
                darkness = 1.0 - exp(-(outside * outside) / max(2.0 * darkSigma * darkSigma, 1e-5));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float sigma = max(_Radii.x, 0.001);
                float playerDark, playerGlow, bedDark, bedGlow;
                float3 playerColor, bedColor;
                Hole(input.positionWS.xy, _PlayerPos.xy, _Radii.y, sigma, playerDark, playerGlow, playerColor);
                Hole(input.positionWS.xy, _BedPos.xy, _Radii.z, sigma, bedDark, bedGlow, bedColor);

                float darkness = playerDark * bedDark;
                float glowAlpha = saturate(1.0 - (1.0 - playerGlow) * (1.0 - bedGlow));
                float3 glowColor = (playerColor * playerGlow + bedColor * bedGlow) / max(playerGlow + bedGlow, 1e-4);

                float alpha = saturate(glowAlpha + darkness * (1.0 - glowAlpha));
                float3 rgb = alpha > 0.001 ? (glowColor * glowAlpha) / alpha : float3(0.0, 0.0, 0.0);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
