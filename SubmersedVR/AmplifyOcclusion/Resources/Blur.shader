// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

Shader "Hidden/Amplify Occlusion/Blur"
{
	Properties { }
	CGINCLUDE
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#pragma exclude_renderers gles d3d11_9x n3ds

		#include "Common.cginc"
		#include "BlurFunctions.cginc"
	ENDCG


	SubShader
	{
		ZTest Always Cull Off ZWrite Off

		// 0 => BLUR HORIZONTAL R:1
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_1x( IN, half2( _AO_CurrOcclusionDepth_TexelSize.x, 0 ) ); } ENDCG }

		// 1 => BLUR VERTICAL R:1
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_1x( IN, half2( 0, _AO_CurrOcclusionDepth_TexelSize.y ) ); } ENDCG }

		// 2 => BLUR HORIZONTAL R:2
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_2x( IN, half2( _AO_CurrOcclusionDepth_TexelSize.x, 0 ) ); } ENDCG }

		// 3 => BLUR VERTICAL R:2
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_2x( IN, half2( 0, _AO_CurrOcclusionDepth_TexelSize.y ) ); } ENDCG }

		// 4 => BLUR HORIZONTAL R:3
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_3x( IN, half2( _AO_CurrOcclusionDepth_TexelSize.x, 0 ) ); } ENDCG }

		// 5 => BLUR VERTICAL R:3
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_3x( IN, half2( 0, _AO_CurrOcclusionDepth_TexelSize.y ) ); } ENDCG }

		// 6 => BLUR HORIZONTAL R:4
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_4x( IN, half2( _AO_CurrOcclusionDepth_TexelSize.x, 0 ) ); } ENDCG }

		// 7 => BLUR VERTICAL R:4
		Pass { CGPROGRAM half4 frag( v2f_in IN ) : SV_Target { return blur1D_4x( IN, half2( 0, _AO_CurrOcclusionDepth_TexelSize.y ) ); } ENDCG }

		// 8 => BLUR HORIZONTAL INTENSITY
		Pass { CGPROGRAM half  frag( v2f_in IN ) : SV_Target { return blur1D_Intensity( IN, half2( _AO_CurrMotionIntensity_TexelSize.x, 0 ) ); } ENDCG }

		// 9 => BLUR VERTICAL INTENSITY
		Pass { CGPROGRAM half  frag( v2f_in IN ) : SV_Target { return blur1D_Intensity( IN, half2( 0, _AO_CurrMotionIntensity_TexelSize.y ) ); } ENDCG }
	}

	Fallback Off
}
