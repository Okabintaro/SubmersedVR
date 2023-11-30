// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_COMMON_INCLUDED
#define AMPLIFY_AO_COMMON_INCLUDED

#pragma multi_compile_instancing

#include "UnityCG.cginc"


#if !defined( UNITY_DECLARE_DEPTH_TEXTURE )
#define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
#endif

#if !defined( UNITY_DECLARE_SCREENSPACE_TEXTURE )
#define UNITY_DECLARE_SCREENSPACE_TEXTURE(tex) sampler2D tex;
#endif

#if !defined( UNITY_SAMPLE_SCREENSPACE_TEXTURE )
#define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) tex2D(tex, uv)
#endif

#if !defined( UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX )
#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(x)
#endif


inline float2 AO_ComputeScreenPos( float4 aVertex )
{
	return ComputeScreenPos( aVertex ).xy;
}


#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
UNITY_DECLARE_SCREENSPACE_TEXTURE( _CameraMotionVectorsTexture );
#else
sampler2D_half _CameraMotionVectorsTexture;
#endif

float4	_CameraMotionVectorsTexture_TexelSize;

inline half2 FetchMotion( const half2 aUV )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _CameraMotionVectorsTexture, UnityStereoTransformScreenSpaceTex( aUV) ).rg;
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_CurrMotionIntensity );
float4	_AO_CurrMotionIntensity_TexelSize;

inline half FetchMotionIntensity( const half2 aUV )
{
	return  UNITY_SAMPLE_SCREENSPACE_TEXTURE( _AO_CurrMotionIntensity, UnityStereoTransformScreenSpaceTex( aUV ) ).r;
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_CurrOcclusionDepth );
float4	_AO_CurrOcclusionDepth_TexelSize;

inline half2 FetchOcclusionDepth( const half2 aUV )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _AO_CurrOcclusionDepth, UnityStereoTransformScreenSpaceTex( aUV ) ).rg;
}


UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
half4	_CameraDepthTexture_TexelSize;

inline half SampleDepth0( const half2 aScreenPos )
{
	return SAMPLE_DEPTH_TEXTURE_LOD( _CameraDepthTexture, half4( UnityStereoTransformScreenSpaceTex( aScreenPos ), 0, 0 ) );
}

UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_SourceDepthMipmap );
half4		_AO_SourceDepthMipmap_TexelSize;

inline half SampleDepth( const half2 aScreenPos, half aLOD )
{
	return SAMPLE_DEPTH_TEXTURE_LOD( _AO_SourceDepthMipmap, half4( UnityStereoTransformScreenSpaceTex( aScreenPos ), 0, aLOD ) );
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _CameraDepthNormalsTexture );

inline half4 FetchDepthNormals( const half2 aScreenPos )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _CameraDepthNormalsTexture, UnityStereoTransformScreenSpaceTex( aScreenPos ) );
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_TemporalAccumm );
float4	_AO_TemporalAccumm_TexelSize;

inline half4 FetchTemporal( const half2 aScreenPos )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _AO_TemporalAccumm, UnityStereoTransformScreenSpaceTex( aScreenPos ) );
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_GBufferNormals );

inline half4 FetchGBufferNormals( const half2 aScreenPos )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _AO_GBufferNormals, UnityStereoTransformScreenSpaceTex( aScreenPos ) );
}

inline half3 FetchGBufferNormalWS( const half2 aScreenPos, const bool aIsGBufferOctaEnconded  )
{
	const half4 gbuffer2 = FetchGBufferNormals( aScreenPos );

	half3 N = gbuffer2.rgb * 2.0 - 1.0;

	if ( ( aIsGBufferOctaEnconded == true ) && ( gbuffer2.a < 1 ) )
	{
		N.z = 1 - abs( N.x ) - abs( N.y );
		N.xy = ( N.z >= 0 ) ? N.xy : ( ( 1 - abs( N.yx ) ) * sign( N.xy ) );
	}

	return N;
}


UNITY_DECLARE_SCREENSPACE_TEXTURE( _AO_CurrDepthSource );
half4		_AO_CurrDepthSource_TexelSize;;

inline half FetchCurrDepthSource( const half2 aScreenPos )
{
	return UNITY_SAMPLE_SCREENSPACE_TEXTURE( _AO_CurrDepthSource, UnityStereoTransformScreenSpaceTex( aScreenPos ) );
}


#include "CommonFunctions.cginc"


v2f_out vert( appdata v )
{
	v2f_out o;
	UNITY_SETUP_INSTANCE_ID( v );
	UNITY_TRANSFER_INSTANCE_ID( v, o );
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

	float4 vertex = float4( v.vertex.xy * 2.0 - 1.0, 0.0, 1.0 );

#ifdef UNITY_HALF_TEXEL_OFFSET
	vertex.xy += ( 1.0 / _AO_Target_TexelSize.zw ) * float2( -1, 1 );
#endif

	o.pos = vertex;

#ifdef UNITY_SINGLE_PASS_STEREO
	#if UNITY_UV_STARTS_AT_TOP
		o.uv = float2( v.uv.x, 1.0 - v.uv.y );
	#else
		o.uv = v.uv;
	#endif
#else
	o.uv = AO_ComputeScreenPos( vertex );
#endif

	return o;
}


#endif
