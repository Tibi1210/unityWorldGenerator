Shader "_Tibi/PostProcess/Fog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags{
			"RenderType"="Opaque"
			"RenderPipeline"="UniversalPipeline"
        }

        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct VertexData{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD2;
                float4 positionSS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _alpha, _RenderDistance;
                float4 _color, _color2;
            CBUFFER_END

            /**
            * @brief Vertex shader függvény
            * @param input Vertex bemeneti adatok
            * @return v2f struktúra a fragmens shader bemenete
            */
            v2f vert (VertexData input)
            {
                v2f output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = mul(UNITY_MATRIX_VP, float4(vertexInput.positionWS, 1.0));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = vertexInput.positionWS;
                float3 viewVector = mul(unity_CameraInvProjection, float4(input.uv * 2 - 1, 0, -1)).xyz;
                output.positionSS = ComputeScreenPos(output.positionCS);
                return output;
            }


            /**
            * @brief Fragmens shader függvény
            * @param input Fragmens bemeneti adatok
            * @return Fragmenthez végső színe
            */
            float4 frag (v2f input) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                float3 lightDir = light.direction;
                float3 lightCol = light.color;
                float4 light_intensity = float4(lightDir*lightCol,1);

                float4 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.uv, 0);

                float2 screenspaceUVs = input.positionSS.xy / input.positionSS.w;

                float rawDepth = SampleSceneDepth(screenspaceUVs);
				float depth = Linear01Depth(rawDepth, _ZBufferParams);

                float viewDistance = depth * _ProjectionParams.z;

                float fogFactor = (_alpha / sqrt(log(2))) * max(0.0f, viewDistance - _RenderDistance);
                fogFactor = exp2(-fogFactor * fogFactor);

                float4 fogOutput = lerp(lerp(_color2, _color ,saturate(fogFactor)), col, saturate(fogFactor));

                return fogOutput;
            }
            ENDHLSL
        }
    }
}
