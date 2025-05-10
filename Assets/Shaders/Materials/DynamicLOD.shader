Shader "_Tibi/DynamicLOD"{
    SubShader{
        Tags{
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
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

            #define _TessellationEdgeLength 10
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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
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

                return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * (pow(viewDistance * 0.5, 1.2)));
            }

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
                input.uv = 0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
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
                if (cullTriangle(p0, p1, p2, bias)){
                    factors.edge[0] = factors.edge[1] = factors.edge[2] = factors.inside = 0;
                } else{
                    factors.edge[0] = TessellationHeuristic(p1, p2);
                    factors.edge[1] = TessellationHeuristic(p2, p0);
                    factors.edge[2] = TessellationHeuristic(p0, p1);
                    factors.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p1, p2)) * (1 / 3.0);
                }
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
            
            /**
            * @brief Fragmens shader függvény
            * @param input Fragmens bemeneti adatok
            * @return Fragmenthez végső színe
            */
            float4 frag(v2f input) : SV_TARGET{
                return float4(0.5, 0.5, 0.5, 1.0);
            }

            ENDHLSL
        }


        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
        Fallback Off
}