// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

Shader "Hidden/Amplify Occlusion/Occlusion"
{
	CGINCLUDE
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#pragma exclude_renderers gles d3d11_9x n3ds

		#include "Common.cginc"
		#include "GTAO.cginc"
		#include "OcclusionFunctions.cginc"
		#include "TemporalFilter.cginc"
	ENDCG

	SubShader
	{
		ZTest Always
		Cull Off
		ZWrite Off

		// 0-3 => FULL OCCLUSION - LOW QUALITY                    directionCount / sampleCount
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 4, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 4, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 4, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 4, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 04-07 => FULL OCCLUSION / MEDIUM QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 6, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 6, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 6, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 2, 6, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 08-11 => FULL OCCLUSION - HIGH QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 3, 8, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 3, 8, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 3, 8, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 3, 8, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 12-15 => FULL OCCLUSION / VERYHIGH QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 4, 10, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 4, 10, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 4, 10, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, false, 4, 10, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }


		// 16-19 => FULL OCCLUSION - LOW QUALITY                    directionCount / sampleCount
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 4, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 4, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 4, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 4, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 20-23 => FULL OCCLUSION / MEDIUM QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 6, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 6, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 6, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 2, 6, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 24-27 => FULL OCCLUSION - HIGH QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 3, 8, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 3, 8, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 3, 8, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 3, 8, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 28-31 => FULL OCCLUSION / VERYHIGH QUALITY
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 4, 10, NORMALS_NONE ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 4, 10, NORMALS_CAMERA ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 4, 10, NORMALS_GBUFFER ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return GTAO( IN, true, 4, 10, NORMALS_GBUFFER_OCTA_ENCODED ); } ENDCG }

		// 32 => CombineDownsampledOcclusionDepth
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return CombineDownsampledOcclusionDepth( IN );	} ENDCG	}

		// 33 => Neighbor Motion Intensity
		Pass { CGPROGRAM half2 frag( v2f_in IN ) : SV_Target { return NeighborMotionIntensity( IN ); } ENDCG }

		// 34 => Clear Temporal
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return ClearTemporal( IN ); } ENDCG }

		// 35 => ScaleDownCloserDepthEven
		Pass { CGPROGRAM float frag( v2f_in IN ) : SV_Target { return ScaleDownCloserDepthEven( IN ); } ENDCG }

		// 36 => ScaleDownCloserDepthEven_CameraDepthTexture
		Pass { CGPROGRAM float frag( v2f_in IN ) : SV_Target { return ScaleDownCloserDepthEven_CameraDepthTexture( IN ); } ENDCG }

		// 37 => Temporal
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return Temporal( IN, false ); } ENDCG }
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return Temporal( IN, true ); } ENDCG }
	}
}

