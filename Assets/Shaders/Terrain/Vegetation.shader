Shader "_Tibi/Advanced/Grass"{
	Properties{
		_MainTex("Texture", 2D) = "white" {}
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

				#pragma vertex vert
				#pragma fragment frag

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

				struct v2f{
					float4 positionCS : SV_Position;
					float2 uv : TEXCOORD0;
				};

				StructuredBuffer<float3> _Positions;
				StructuredBuffer<float2> _UVs;
				StructuredBuffer<float4x4> _TransformMatrices;

				TEXTURE2D(_MainTex);
				SAMPLER(sampler_MainTex);
				CBUFFER_START(UnityPerMaterial)
					float4 _MainTex_ST;
					float4 _BaseColor;
					float4 _TipColor;
				CBUFFER_END



				v2f vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID){
					float4x4 mat = _TransformMatrices[instanceID];
					v2f o;
					float4 pos = float4(_Positions[vertexID], 1.0);
					pos = mul(mat, pos);
					o.positionCS = mul(UNITY_MATRIX_VP, pos);
					//o.uv = _UVs[vertexID];
					o.uv = TRANSFORM_TEX(_UVs[vertexID], _MainTex);
					return o;
				}

				float4 frag (v2f i) : SV_Target{
					float4 tex = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv, 0);
					if (tex.a < 0.5) discard;
					return tex;
				}

			ENDHLSL
		}	
		
		Pass{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}
			ZWrite On
			ColorMask 0
			HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				
				struct v2f{
					float4 positionCS : SV_Position;
				};
				
				StructuredBuffer<float3> _Positions;
				StructuredBuffer<float2> _UVs;
				StructuredBuffer<float4x4> _TransformMatrices;
				
				TEXTURE2D(_MainTex);
				SAMPLER(sampler_MainTex);
				CBUFFER_START(UnityPerMaterial)
					float4 _MainTex_ST;
				CBUFFER_END
				
				v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID){
					float4x4 mat = _TransformMatrices[instanceID];
					v2f o;
					float4 pos = float4(_Positions[vertexID], 1.0);
					pos = mul(mat, pos);
					o.positionCS = mul(UNITY_MATRIX_VP, pos);
					return o;
				}
				
				float4 frag(v2f i) : SV_Target{
					return 0;
				}
			ENDHLSL
		}
		UsePass "Universal Render Pipeline/Lit/DepthNormals"
	}
	Fallback Off
}