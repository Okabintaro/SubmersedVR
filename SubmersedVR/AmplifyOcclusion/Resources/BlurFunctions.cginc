// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_BLURFUNCTIONS
#define AMPLIFY_AO_BLURFUNCTIONS


float		_AO_BlurSharpness;

inline half ComputeSharpness( half linearEyeDepth )
{
	return _AO_BlurSharpness * ( saturate( 1 - linearEyeDepth ) + 0.01 );
}

inline half ComputeFalloff( const int radius )
{
	return 2.0 / ( radius * radius );
}

inline half2 CrossBilateralWeight( const half2 r, half2 d, half d0, const half sharpness, const half falloff )
{
	half2 diff = ( d0 - d ) * sharpness;
	return exp2( -( r * r ) * falloff - diff * diff );
}

inline half4 CrossBilateralWeight( const half4 r, half4 d, half d0, const half sharpness, const half falloff )
{
	half4 diff = ( d0 - d ) * sharpness;
	return exp2( -( r * r ) * falloff - diff * diff );
}


half4 blur1D_1x( v2f_in IN, half2 deltaUV )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 occlusionDepth = FetchOcclusionDepth( IN.uv );

	const half occlusion = occlusionDepth.x;
	const half depth = occlusionDepth.y;

	const half2 offset1 = 1.55 * deltaUV;

	half4 s1;
	s1.xy = FetchOcclusionDepth( IN.uv + offset1 ).xy;
	s1.zw = FetchOcclusionDepth( IN.uv - offset1 ).xy;

	const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;
	
	const half sharpness = ComputeSharpness( linearEyeDepth );
	const half falloff = ComputeFalloff( 2 );

	const half2 w1 = CrossBilateralWeight( ( 1.0 ).xx, s1.yw, depth, sharpness, falloff );

	half ao = occlusion + dot( s1.xz, w1 );
	ao /= 1.0 + dot( ( 1.0 ).xx, w1 );

	return half4( ao, depth, 0.0, 0.0 );
}

half4 blur1D_2x( v2f_in IN, half2 deltaUV )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 occlusionDepth = FetchOcclusionDepth( IN.uv );

	const half occlusion = occlusionDepth.x;
	const half depth = occlusionDepth.y;

	const half2 offset1 = 1.2 * deltaUV;
	const half2 offset2 = 2.5 * deltaUV;

	half4 s1, s2;
	s2.zw = FetchOcclusionDepth( IN.uv - offset2 ).xy;
	s1.zw = FetchOcclusionDepth( IN.uv - offset1 ).xy;
	s1.xy = FetchOcclusionDepth( IN.uv + offset1 ).xy;
	s2.xy = FetchOcclusionDepth( IN.uv + offset2 ).xy;

	const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

	const half sharpness = ComputeSharpness( linearEyeDepth );
	const half falloff = ComputeFalloff( 4 );

	const half4 w12 = CrossBilateralWeight( half4( 1, 1, 2, 2 ), half4( s1.yw, s2.yw ), depth, sharpness, falloff );

	half ao = occlusion + dot( half4( s1.xz, s2.xz ), w12 );
	ao /= 1.0 + dot( ( 1.0 ).xxxx, w12 );

	return half4( ao, depth, 0.0, 0.0 );
}


half4 blur1D_3x( v2f_in IN, half2 deltaUV )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 occlusionDepth = FetchOcclusionDepth( IN.uv );

	const half occlusion = occlusionDepth.x;
	const half depth = occlusionDepth.y;

	const half2 offset1 = 1.0 * deltaUV;
	const half2 offset2 = 2.2 * deltaUV;
	const half2 offset3 = 3.5 * deltaUV;

	half4 s1, s2, s3;
	s3.zw = FetchOcclusionDepth( IN.uv - offset3 ).xy;
	s2.zw = FetchOcclusionDepth( IN.uv - offset2 ).xy;
	s1.zw = FetchOcclusionDepth( IN.uv - offset1 ).xy;
	s1.xy = FetchOcclusionDepth( IN.uv + offset1 ).xy;
	s2.xy = FetchOcclusionDepth( IN.uv + offset2 ).xy;
	s3.xy = FetchOcclusionDepth( IN.uv + offset3 ).xy;

	const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

	const half sharpness = ComputeSharpness( linearEyeDepth );
	const half falloff = ComputeFalloff( 6 );

	const half4 w12 = CrossBilateralWeight( half4( 1, 1, 2, 2 ), half4( s1.yw, s2.yw ), depth, sharpness, falloff );
	const half2 w3 = CrossBilateralWeight( ( 3 ).xx, s3.yw, depth, sharpness, falloff );

	half ao = occlusion + dot( half4( s1.xz, s2.xz ), w12 ) + dot( s3.xz, w3 );
	ao /= 1.0 + dot( ( 1.0 ).xxxx, w12 ) + dot( ( 1 ).xx, w3 );

	return half4( ao, depth, 0.0, 0.0 );
}


half4 blur1D_4x( v2f_in IN, half2 deltaUV )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 occlusionDepth = FetchOcclusionDepth( IN.uv );

	const half occlusion = occlusionDepth.x;
	const half depth = occlusionDepth.y;

	const half2 offset1 = 1.1 * deltaUV;
	const half2 offset2 = 2.2 * deltaUV;
	const half2 offset3 = 3.3 * deltaUV;
	const half2 offset4 = 4.5 * deltaUV;

	half4 s1, s2, s3, s4;
	s4.zw = FetchOcclusionDepth( IN.uv - offset4 ).xy;
	s3.zw = FetchOcclusionDepth( IN.uv - offset3 ).xy;
	s2.zw = FetchOcclusionDepth( IN.uv - offset2 ).xy;
	s1.zw = FetchOcclusionDepth( IN.uv - offset1 ).xy;
	s1.xy = FetchOcclusionDepth( IN.uv + offset1 ).xy;
	s2.xy = FetchOcclusionDepth( IN.uv + offset2 ).xy;
	s3.xy = FetchOcclusionDepth( IN.uv + offset3 ).xy;
	s4.xy = FetchOcclusionDepth( IN.uv + offset4 ).xy;

	const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

	const half sharpness = ComputeSharpness( linearEyeDepth );
	const half falloff = ComputeFalloff( 8 );

	const half4 w12 = CrossBilateralWeight( half4( 1, 1, 2, 2 ), half4( s1.yw, s2.yw ), depth, sharpness, falloff );
	const half4 w34 = CrossBilateralWeight( half4( 3, 3, 4, 4 ), half4( s3.yw, s4.yw ), depth, sharpness, falloff );

	half ao = occlusion + dot( half4( s1.xz, s2.xz ), w12 ) + dot( half4( s3.xz, s4.xz ), w34 );
	ao /= 1.0 + dot( ( 1.0 ).xxxx, w12 ) + dot( ( 1 ).xxxx, w34 );

	return half4( ao, depth, 0.0, 0.0 );
}


half4 blur1D_Intensity( v2f_in IN, half2 deltaUV )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	half3 c;

	c.x = FetchMotionIntensity( IN.uv );
	c.y = FetchMotionIntensity( IN.uv + deltaUV * 1.5 );
	c.z = FetchMotionIntensity( IN.uv - deltaUV * 1.5 );

	half2 c2;
	c2.x = FetchMotionIntensity( IN.uv + deltaUV * 3.3 );
	c2.y = FetchMotionIntensity( IN.uv - deltaUV * 3.3 );

	const half outV = saturate( dot( half3( 0.30, 0.25, 0.25 ), c ) + dot( (0.15).xx, c2 ) );

	return outV;
}

#endif
