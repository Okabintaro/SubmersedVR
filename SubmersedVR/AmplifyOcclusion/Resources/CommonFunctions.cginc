// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_COMMON_FUNCTIONS_INCLUDED
#define AMPLIFY_AO_COMMON_FUNCTIONS_INCLUDED

#define PIXEL_RADIUS_LIMIT ( 512 )

#define HALF_MAX        65504.0 // (2 - 2^-10) * 2^15
#define HALF_MAX_MINUS1 65472.0 // (2 - 2^-9) * 2^15
#define DEPTH_EPSILON	1e-6
#define INTENSITY_THRESHOLD 1e-4

#if defined( SHADER_API_MOBILE )
#define DEPTH_SCALE 16376.0
#else
#define DEPTH_SCALE 65504.0
#endif

#define ONE_OVER_DEPTH_SCALE ( 1.0 / DEPTH_SCALE )

float		_AO_TemporalMotionSensibility;
half		_AO_TemporalDirections;
half		_AO_TemporalOffsets;
half		_AO_HalfProjScale;
half		_AO_Radius;
float		_AO_BufDepthToLinearEye;

inline half DecAO( const half ao )
{
	return sqrt( ao );
}

inline half EncAO( const half ao )
{
	return saturate( ao * ao );
}

inline half DecDepth( const float2 aEncodedDepth )
{
	return DecodeFloatRG( aEncodedDepth );
}

inline half2 EncDepth( const half aDepth )
{
	return EncodeFloatRG( aDepth );
}

float2		_AO_FadeParams;
float4		_AO_FadeValues;
half4		_AO_Levels;
half4		_AO_FadeToTint;
half		_AO_PowExponent;

inline half ComputeDistanceFade( const half distance )
{
	return saturate( max( 0.0, distance - _AO_FadeParams.x ) * _AO_FadeParams.y );
}

inline half4 CalcOcclusion( const half aOcclusion, const half aLinearDepth )
{
	const half distanceFade = ComputeDistanceFade( aLinearDepth );

	const half exponent = lerp( _AO_PowExponent, _AO_FadeValues.z, distanceFade );

	const half occlusion = pow( max( aOcclusion, 0.0 ), exponent );

	half3 tintedOcclusion = lerp( _AO_Levels.rgb, _AO_FadeToTint.rgb, distanceFade );

	tintedOcclusion = lerp( tintedOcclusion, ( 1 ).xxx, occlusion.xxx );

	const half intensity = lerp( _AO_Levels.a, _AO_FadeValues.x, distanceFade );

	return lerp( ( 1 ).xxxx, half4( tintedOcclusion.rgb, occlusion ), intensity );
}

inline float LinearEyeToSampledDepth( float linearEyeDepth )
{
	return ( 1.0 - linearEyeDepth * _ZBufferParams.w ) / ( linearEyeDepth * _ZBufferParams.z );
}

inline float2 LinearEyeToSampledDepth( float2 linearEyeDepth )
{
	return ( (1.0).xx - linearEyeDepth * ( _ZBufferParams.w ).xx ) / ( linearEyeDepth * ( _ZBufferParams.z ).xx );
}

inline float3 LinearEyeToSampledDepth( float3 linearEyeDepth )
{
	return ( (1.0).xxx - linearEyeDepth * ( _ZBufferParams.w ).xxx ) / ( linearEyeDepth * ( _ZBufferParams.z ).xxx );
}

inline float4 LinearEyeToSampledDepth( float4 linearEyeDepth )
{
	return ( (1.0).xxxx - linearEyeDepth * ( _ZBufferParams.w ).xxxx ) / ( linearEyeDepth * ( _ZBufferParams.z ).xxxx );
}

inline float Linear01ToSampledDepth( float linear01Depth )
{
	return ( 1.0 - linear01Depth * _ZBufferParams.y ) / ( linear01Depth * _ZBufferParams.x );
}

inline float2 Linear01ToSampledDepth( float2 linear01Depth )
{
	return ( (1.0).xx - linear01Depth * _ZBufferParams.y ) / ( linear01Depth * ( _ZBufferParams.x ).xx );
}

inline float4 Linear01ToSampledDepth( float4 linear01Depth )
{
	return ( (1.0).xxxx - linear01Depth * ( _ZBufferParams.y ).xxxx ) / ( linear01Depth * ( _ZBufferParams.x ).xxxx );
}


struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f_out
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};


struct v2f_in
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};


half4		_AO_Target_TexelSize;

// Jimenez's "Interleaved Gradient Noise"
inline half JimenezNoise( const half2 xyPixelPos )
{
	return frac( 52.9829189 * frac( dot( xyPixelPos, half2( 0.06711056, 0.00583715 ) ) ) );
}


inline void GetSpatialDirections_Offsets_JimenezNoise(	const half2 aScreenPos,
											const half2 aTextureSizeZW,
											out half outNoiseSpatialOffsets,
											out half outNoiseSpatialDirections )
{
#if defined( SHADER_API_D3D9 ) || defined( SHADER_API_MOBILE )
	// Spatial Offsets and Directions - s2016_pbs_activision_occlusion - Slide 93
	const half2 xyPixelPos = ceil( UnityStereoTransformScreenSpaceTex( aScreenPos ) * aTextureSizeZW );
	outNoiseSpatialOffsets = ( 1.0 / 4.0 ) * (half)( frac( ( xyPixelPos.y - xyPixelPos.x ) / 4.0 ) * 4.0 );

	outNoiseSpatialDirections = JimenezNoise( (half2)xyPixelPos );
#else
	// Spatial Offsets and Directions - s2016_pbs_activision_occlusion - Slide 93
	const int2 xyPixelPos = (int2)( UnityStereoTransformScreenSpaceTex( aScreenPos ) * aTextureSizeZW );
	outNoiseSpatialOffsets = ( 1.0 / 4.0 ) * (half)( ( xyPixelPos.y - xyPixelPos.x ) & 3 );

	outNoiseSpatialDirections = JimenezNoise( (half2)xyPixelPos );
#endif
}

inline void GetSpatialDirections_Offsets(	const half2 aScreenPos,
											const half2 aTextureSizeZW,
											out half outNoiseSpatialOffsets,
											out half outNoiseSpatialDirections )
{
#if defined( SHADER_API_D3D9 ) || defined( SHADER_API_MOBILE )
	// Spatial Offsets and Directions - s2016_pbs_activision_occlusion - Slide 93
	const half2 xyPixelPos = ceil( UnityStereoTransformScreenSpaceTex( aScreenPos ) * aTextureSizeZW );
	outNoiseSpatialOffsets = ( 1.0 / 4.0 ) * (half)( frac( ( xyPixelPos.y - xyPixelPos.x ) / 4.0 ) * 4.0 );

	// Noise Spatial Directions
	// X   0  1  2  3
	// Y0 00 05 10 15
	// Y1 04 09 14 03
	// Y2 08 13 02 07
	// Y3 12 01 06 11
	outNoiseSpatialDirections = ( 1.0 / 16.0 ) * (half)( ( ( frac( ( xyPixelPos.x + xyPixelPos.y ) / 4.0 ) * 4.0 ) * 4.0 ) + frac( xyPixelPos.x / 4.0 ) * 4.0 );
#else
	// Spatial Offsets and Directions - s2016_pbs_activision_occlusion - Slide 93
	const int2 xyPixelPos = (int2)( UnityStereoTransformScreenSpaceTex( aScreenPos ) * aTextureSizeZW );
	outNoiseSpatialOffsets = ( 1.0 / 4.0 ) * (half)( ( xyPixelPos.y - xyPixelPos.x ) & 3 );

	// Noise Spatial Directions
	// X   0  1  2  3
	// Y0 00 05 10 15
	// Y1 04 09 14 03
	// Y2 08 13 02 07
	// Y3 12 01 06 11
	outNoiseSpatialDirections = ( 1.0 / 16.0 ) * (half)( ( ( ( xyPixelPos.x + xyPixelPos.y ) & 0x3 ) << 2 ) | ( xyPixelPos.x & 0x3 ) );
#endif
}


#endif
