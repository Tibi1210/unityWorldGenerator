Shader "_Tibi/Terrain_LOD"{

    Properties{
        _TangentTex ("Tangent", 2D) = "" {}
        _NormalStrength ("Normal Strength", Range(0.0, 3.0)) = 0.0
    }

    SubShader{
        Tags{
            "RenderType" = "Opaque"
            "Queue" = "Geometry+3000"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass{

            Tags{
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex vert
            #pragma hull tessHull
            #pragma domain tessDomain
            #pragma fragment frag

            #define EDGE_LEN 8
            #define PI 3.14159265358979323846

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            /**
            * @brief Ellenőrzi, hogy egy háromszög teljesen a kamera vágósík alatt helyezkedik-e el
            * @param p0 Az első csúcspont pozíciója a világtérben
            * @param p1 A második csúcspont pozíciója a világtérben
            * @param p2 A harmadik csúcspont pozíciója a világtérben
            * @param planeIndex A vizsgálandó vágósík indexe (0-5)
            * @param bias Eltolási érték a kivágási küszöbérték beállításához
            * @return Igaz, ha a háromszög teljesen a megadott vágósík alatt helyezkedik el
            */
            bool TriangleIsBelowClipPlane(float3 p0, float3 p1, float3 p2, int planeIndex, float bias){
                float4 plane = unity_CameraWorldClipPlanes[planeIndex];
                return dot(float4(p0, 1), plane) < 
                       bias && dot(float4(p1, 1), plane) < 
                       bias && dot(float4(p2, 1), plane) < 
                       bias;
            }

            /**
            * @brief Láthatósági gúla alapú kivágást végez egy háromszögre
            * @param p0 Az első csúcspont pozíciója a világtérben
            * @param p1 A második csúcspont pozíciója a világtérben
            * @param p2 A harmadik csúcspont pozíciója a világtérben
            * @param bias Eltolási érték a kivágási küszöbérték beállításához
            * @return Igaz, ha a háromszöget ki kell vágni (láthatósági gúlán kívül esik)
            */
            bool cullTriangle(float3 p0, float3 p1, float3 p2, float bias){
                return TriangleIsBelowClipPlane(p0, p1, p2, 0, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 1, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 2, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 3, bias);
            }

            struct VertexData{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                float3 normal: TEXCOORD2;
                float4 tangent: TEXCOORD3;
            };

            struct TessellationControlPoint{
                float4 positionOS : INTERNALTESSPOS;
                float2 uv : TEXCOORD0;
            };

            struct TessellationFactors{
                float edge[3] : SV_TESSFACTOR;
                float inside : SV_INSIDETESSFACTOR;
            };

            /**
            * @brief Kiszámítja a tesszelációs faktort az él hossza és a nézeti távolság alapján
            * @param cp0 Első kontrollpont a világtérben
            * @param cp1 Második kontrollpont a világtérben
            * @return Tesszelációs faktor a cp0 és cp1 közötti élre
            */
            float TessellationHeuristic(float3 cp0, float3 cp1){
                float edgeLength = distance(cp0, cp1);
                float3 edgeCenter = (cp0 + cp1) * 0.5;
                float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

                return edgeLength * _ScreenParams.y / (EDGE_LEN * (pow(viewDistance * 0.5, 1.2)));
            }


            TEXTURE2D(_AlbedoTex);
            SAMPLER(sampler_AlbedoTex);
			TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);
			TEXTURE2D(_TangentTex);
            SAMPLER(sampler_TangentTex);
            TEXTURE2D_ARRAY(_HeightTex);
            SAMPLER(sampler_HeightTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _HeightTex_ST, _AlbedoTex_ST, _NormalTex_ST, _TangentTex_ST;
                float4 _TopColor, _BotColor;
                float _NormalStrength, _UV, _Roughness, _Metallic, _Subsurface, _Specular, _SpecularTint, _Anisotropic, _Sheen, _SheenTint, _ClearCoat, _ClearCoatGloss;
                int _isNormal;
            CBUFFER_END

            /**
            * @brief Vertex shader függvény a tesszelációhoz
            * @param input Vertex bemeneti adatok
            * @return Tesszelációs kontrollpont
            */
            TessellationControlPoint vert(VertexData input){
                TessellationControlPoint output;
                output.positionOS = input.positionOS;
                output.uv = input.uv;
                return output;
            }

            /**
            * @brief Vertex adatok feldolgozása a tesszeláció után
            * @param input Vertex bemeneti adatok
            * @return Feldolgozott vertex adatok
            */
            v2f tessVert(VertexData input){
                v2f output;
                
                float2 uv = input.uv * 5.0;
                output.uv = TRANSFORM_TEX(uv, _HeightTex);

                //float4 displacement = noised(float3(uv, 1.0));
                float4 displacement = SAMPLE_TEXTURE2D_ARRAY_LOD(_HeightTex, sampler_HeightTex, input.uv, 0, 0);

                float4 p = input.positionOS;
                p.y = displacement.x * 100.0; 
  
                VertexPositionInputs vertexInput = GetVertexPositionInputs(p);

                //float4 positionWS = float4(vertexInput.positionWS,1);
                //positionWS.y += displacement.x * 100; 
 
                // Calculate normal from derivatives
                float3 normal = float3(-displacement.y, 1.0, -displacement.w);
                normal = normalize(normal);

                output.positionWS = vertexInput.positionWS;
                output.positionCS = mul(UNITY_MATRIX_VP, float4(vertexInput.positionWS, 1.0));

                VertexNormalInputs normalInput = GetVertexNormalInputs(normal);
                output.normal = normalInput.normalWS;
                output.tangent = float4(normalInput.tangentWS, 1.0);

                return output;
            }

            /**
            * @brief Kiszámítja a tesszelációs faktorokat egy primitívhez
            * @param patch Primitívhez tartozó kontrollpontok
            * @return Tesszelációs faktorok a primitívhez
            */
            TessellationFactors PatchFunction(InputPatch<TessellationControlPoint, 3> patch){
                VertexPositionInputs p0_input = GetVertexPositionInputs(patch[0].positionOS);
                VertexPositionInputs p1_input = GetVertexPositionInputs(patch[1].positionOS);
                VertexPositionInputs p2_input = GetVertexPositionInputs(patch[2].positionOS);
                float3 p0 = p0_input.positionWS;
                float3 p1 = p1_input.positionWS;
                float3 p2 = p2_input.positionWS;

                TessellationFactors factors;
                float bias = -0.5 * 100;
                //if (cullTriangle(p0, p1, p2, bias)){
                //    factors.edge[0] = factors.edge[1] = factors.edge[2] = factors.inside = 0;
                //} else{
                    factors.edge[0] = TessellationHeuristic(p1, p2);
                    factors.edge[1] = TessellationHeuristic(p2, p0);
                    factors.edge[2] = TessellationHeuristic(p0, p1);
                    factors.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p1, p2)) * (1 / 3.0);
                //}
                return factors;
            }

            /**
            * @brief Hull shader a tesszelációhoz
            * @param patch Primitívhez tartozó kontrollpontok
            * @param id Kontrollpont azonosító
            * @return Kontrollpont a megadott azonosítóhoz
            */
            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("PatchFunction")]
            TessellationControlPoint tessHull(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID){
                return patch[id];
            }

            /**
            * @brief Domain shader a tesszelációhoz
            * @param factors Tesszelációs faktorok
            * @param patch Primitívhez tartozó kontrollpontok
            * @param bcCoords Baricentrikus koordináták
            * @return Feldolgozott vertex adatok a tesszelált ponthoz
            */
            [domain("tri")]
            v2f tessDomain(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 bcCoords : SV_DOMAINLOCATION){
                VertexData data;
                data.positionOS = patch[0].positionOS * bcCoords.x + patch[1].positionOS * bcCoords.y + patch[2].positionOS * bcCoords.z;
                data.uv = patch[0].uv * bcCoords.x + patch[1].uv * bcCoords.y + patch[2].uv * bcCoords.z;
                return tessVert(data);
            }

            half DotClamped(half3 a, half3 b){
                return saturate(dot(a, b));
            }
            ///////////////////////////////////////////BRDF///////////////////////////////////////////
            // Forrás: https://github.com/wdas/brdf/blob/main/src/brdfs/disney.brdf
            
            float luminance(float3 color){
                return dot(color, float3(0.299f, 0.587f, 0.114f));
            }

            float sqr(float x){ 
                return x * x; 
            }

            float SchlickFresnel(float x){
                x = saturate(1.0 - x);
                float x2 = x * x;
    
                return x2 * x2 * x;
            }

             // Anisotropic Generalized Trowbridge Reitz
             float AnisotropicGTR2(float ndoth, float hdotx, float hdoty, float ax, float ay){
                return rcp(PI * ax * ay * sqr(sqr(hdotx / ax) + sqr(hdoty / ay) + sqr(ndoth)));
            }

            // Anisotropic Geometric Attenuation Function for GGX
            float AnisotropicSmithGGX(float ndots, float sdotx, float sdoty, float ax, float ay){
                return rcp(ndots + sqrt(sqr(sdotx * ax) + sqr(sdoty * ay) + sqr(ndots)));
            }

            // Isotropic Generalized Trowbridge Reitz
            float GTR1(float ndoth, float a){
                float a2 = a * a;
                float t = 1.0f + (a2 - 1.0f) * ndoth * ndoth;
                return (a2 - 1.0f) / (PI * log(a2) * t);
            }

            // Isotropic Geometric Attenuation Function for GGX
            float SmithGGX(float alphaSquared, float ndotl, float ndotv){
                float a = ndotv * sqrt(alphaSquared + ndotl * (ndotl - alphaSquared * ndotl));
                float b = ndotl * sqrt(alphaSquared + ndotv * (ndotv - alphaSquared * ndotv));
    
                return 0.5f / (a + b);
            }

            struct BRDFResults{
                float3 diffuse;
                float3 specular;
                float3 clearcoat;
            };
    
            BRDFResults BRDF(float3 baseColor, float3 L, float3 V, float3 N, float3 X, float3 Y){
                BRDFResults output;
                output.diffuse = 0.0;
                output.specular = 0.0;
                output.clearcoat = 0.0;
    
                float3 H = normalize(L + V);
                float ndotl = DotClamped(N, L);
                float ndotv = DotClamped(N, V);
                float ndoth = DotClamped(N, H);
                float ldoth = DotClamped(L, H);
    
                float3 surfaceColor = baseColor * baseColor;
    
                float Cdlum = luminance(surfaceColor);
    
                float3 Ctint = Cdlum > 0.0 ? surfaceColor / Cdlum : 1.0;
                float3 Cspec0 = lerp(_Specular * 0.08 * lerp(1.0, Ctint, _SpecularTint), surfaceColor, _Metallic);
                float3 Csheen = lerp(1.0, Ctint, _SheenTint);
    
    
                // Disney Diffuse
                float FL = SchlickFresnel(ndotl);
                float FV = SchlickFresnel(ndotv);
    
                float Fss90 = ldoth * ldoth * _Roughness;
                float Fd90 = 0.5 + 2.0 * Fss90;
    
                float Fd = lerp(1.0f, Fd90, FL) * lerp(1.0f, Fd90, FV);
    
                // Subsurface Diffuse
                float Fss = lerp(1.0f, Fss90, FL) * lerp(1.0f, Fss90, FV);
                float ss = 1.25 * (Fss * (rcp(ndotl + ndotv) - 0.5f) + 0.5f);
    
                // Specular
                float alpha = _Roughness;
                float alphaSquared = alpha * alpha;
    
                // Anisotropic Microfacet Normal Distribution
                float aspectRatio = sqrt(1.0 - _Anisotropic * 0.9f);
                float alphaX = max(0.001f, alphaSquared / aspectRatio);
                float alphaY = max(0.001f, alphaSquared * aspectRatio);
                float Ds = AnisotropicGTR2(ndoth, dot(H, X), dot(H, Y), alphaX, alphaY);
    
                // Geometric Attenuation
                float GalphaSquared = sqr(0.5 + _Roughness * 0.5f);
                float GalphaX = max(0.001f, GalphaSquared / aspectRatio);
                float GalphaY = max(0.001f, GalphaSquared * aspectRatio);
                float G = AnisotropicSmithGGX(ndotl, dot(L, X), dot(L, Y), GalphaX, GalphaY);
                G *= AnisotropicSmithGGX(ndotv, dot(V, X), dot (V, Y), GalphaX, GalphaY);
    
                // Fresnel Reflectance
                float FH = SchlickFresnel(ldoth);
                float3 F = lerp(Cspec0, 1.0f, FH);
    
                // Sheen
                float3 Fsheen = FH * _Sheen * Csheen;
    
                // Clearcoat
                float Dr = GTR1(ndoth, lerp(0.1f, 0.001f, _ClearCoatGloss));
                float Fr = lerp(0.04, 1.0f, FH);
                float Gr = SmithGGX(ndotl, ndotv, 0.25f);
    
                
                output.diffuse = (1.0 / PI) * (lerp(Fd, ss, _Subsurface) * surfaceColor + Fsheen) * (1 - _Metallic);
                output.specular = Ds * F * G;
                output.clearcoat = 0.25 * _ClearCoat * Gr * Fr * Dr;
    
                return output;
            }
            
            /**
            * @brief Fragmens shader függvény
            * @param input Fragmens bemeneti adatok
            * @return Fragmenthez végső színe
            */
            float4 frag(v2f input) : SV_TARGET{

                float2 uv = input.uv;
                
                float3 unnormalizedNormalWS = input.normal;
                float renormFactor = 1.0f / length(unnormalizedNormalWS);

                float3x3 worldToTangent;
                float3 bitangent = cross(unnormalizedNormalWS, input.tangent.xyz) * input.tangent.w;
                worldToTangent[0] = input.tangent.xyz * renormFactor;
                worldToTangent[1] = bitangent * renormFactor;
                worldToTangent[2] = unnormalizedNormalWS * renormFactor;

				float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv*_UV);
                packedNormal.w *= packedNormal.x;

                float3 N;
                N.xy = packedNormal.wy * 2.0f - 1.0f;
                N.xy *= _NormalStrength;
                N.z = sqrt(1.0f - saturate(dot(N.xy, N.xy)));
                N = mul(N, worldToTangent);

                float3 T;
				
                T.xy = SAMPLE_TEXTURE2D(_TangentTex, sampler_TangentTex, uv*_UV).wy * 2 - 1;
                T.z = sqrt(1 - saturate(dot(T.xy, T.xy)));

                T = mul(lerp(float3(1.0f, 0.0f, 0.0f), T, saturate(_NormalStrength)), worldToTangent);
                
                float3 albedo = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, uv*_UV).rgb;
				
				float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                float3 lightDir = light.direction;
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float3 L = normalize(light.direction);
                float3 V = normalize(viewDir);
                float3 X = normalize(T);
                float3 Y = normalize(cross(N, T) * input.tangent.w);

                BRDFResults reflection = BRDF(albedo, L, V, N, X, Y);

                float3 output = light.color * (reflection.diffuse + reflection.specular + reflection.clearcoat);
                output *= DotClamped(N, L);

                return float4(max(0.0f, output), 1.0f);


  
            }

            ENDHLSL
        }

        Pass{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}
			ZWrite On
			ColorMask 0
			HLSLPROGRAM
				#pragma vertex DepthOnlyVertex
				#pragma fragment DepthOnlyFragment
				#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
				#pragma multi_compile_instancing
				#pragma multi_compile _ DOTS_INSTANCING_ON
			ENDHLSL
		}
		UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

        Fallback Off
}