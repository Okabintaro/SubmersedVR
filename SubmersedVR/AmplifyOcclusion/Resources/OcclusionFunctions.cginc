// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_OCCLUSIONFUNCTIONS
#define AMPLIFY_AO_OCCLUSIONFUNCTIONS


half4 GTAO( const v2f_in ifrag, const bool useDynamicDepthMips, const int directionCount, const int sampleCount, const int normalSource )
{
	UNITY_SETUP_INSTANCE_ID( ifrag );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( ifrag );

	half outDepth;
	half4 outRGBA;

	GetGTAO( ifrag, useDynamicDepthMips, directionCount, sampleCount / 2, normalSource, outDepth, outRGBA );

	return half4( outRGBA.a, outDepth, 0, 0 );
}


inline half4 ComputeCombineDownsampledOcclusionDepth( const half2 aScreenPos, const half aDepthSample )
{
	const half referenceDepth = LinearEyeDepth( aDepthSample );

	const half intensity = lerp( _AO_Levels.a, _AO_FadeValues.x, ComputeDistanceFade( referenceDepth ) );

	//UNITY_BRANCH
	#if defined(UNITY_REVERSED_Z)
	if( ( aDepthSample <= DEPTH_EPSILON ) || ( intensity < INTENSITY_THRESHOLD ) )
	#else
	if( ( aDepthSample >= ( 1.0 - DEPTH_EPSILON ) ) || ( intensity < INTENSITY_THRESHOLD ) )
	#endif
	{
		return half4( 1.0, HALF_MAX, 0, 0 );
	}

	const half2 screenPosPixels = aScreenPos * _AO_CurrOcclusionDepth_TexelSize.zw;
	const half2 screenPosPixelsFloor = floor( screenPosPixels );
	const half2 screenPosPixelsDelta = screenPosPixels - screenPosPixelsFloor;

	const half2 sPosAdjusted = screenPosPixelsFloor * _AO_CurrOcclusionDepth_TexelSize.xy + half2( 0.5, 0.5 ) * _AO_CurrOcclusionDepth_TexelSize.xy;
	const half s = ( screenPosPixelsDelta.y < 0.5 )?-1.0:1.0;

	half2 odC = FetchOcclusionDepth( sPosAdjusted );

	half2 odL = FetchOcclusionDepth( sPosAdjusted + half2( -1.0, 0.0 ) * _AO_CurrOcclusionDepth_TexelSize.xy );
	half2 odR = FetchOcclusionDepth( sPosAdjusted + half2( +1.0, 0.0 ) * _AO_CurrOcclusionDepth_TexelSize.xy );
	half2 odM = FetchOcclusionDepth( sPosAdjusted + half2(  0.0,   s ) * _AO_CurrOcclusionDepth_TexelSize.xy );

	const half4 o0123 = half4( odC.x, odL.x, odR.x, odM.x );
	const half4 d0123 = half4( odC.y, odL.y, odR.y, odM.y );

	half4 depthWeight0123 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d0123 * ONE_OVER_DEPTH_SCALE ) - ( aDepthSample ).xxxx ) * 32768 + 0.95 ) );

	const half4 pixelDeltaWeight = half4( screenPosPixelsDelta.x * screenPosPixelsDelta.y + 0.5,
											1.0 - screenPosPixelsDelta.x,
											screenPosPixelsDelta.x,
											0.80 );

	depthWeight0123 = depthWeight0123 * depthWeight0123 * pixelDeltaWeight;

	half weightOcclusion = dot( o0123, depthWeight0123 );

	const half outOcclusion = saturate( weightOcclusion / dot( ( 1 ).xxxx, depthWeight0123 ) );

	const half linearDepth01 = Linear01Depth( aDepthSample );

	return half4( outOcclusion, DEPTH_SCALE * linearDepth01, 0, 0 );
}


half4 CombineDownsampledOcclusionDepth( const v2f_in ifrag )
{
	UNITY_SETUP_INSTANCE_ID( ifrag );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( ifrag );

	const half2 screenPos = ifrag.uv.xy;

	const half depthSample = SampleDepth0( screenPos );

	return ComputeCombineDownsampledOcclusionDepth( screenPos, depthSample );
}


inline half FetchNeighborMotion( const half2 aUV, const half aRadiusPixels, const half2 aCentralmv )
{
	half noiseSpatialOffsets, noiseSpatialDirections;
	GetSpatialDirections_Offsets_JimenezNoise( aUV, _CameraMotionVectorsTexture_TexelSize.zw * 0.5, noiseSpatialOffsets, noiseSpatialDirections );

	const half angle = ( noiseSpatialDirections ) * UNITY_PI;
	const half2 cos_sin = half2( cos( angle ), sin( angle ) );
	const half2 xy = ( aRadiusPixels * noiseSpatialOffsets + 8.0 ).xx * cos_sin;

	const half2 xLyT = FetchMotion( half2( -xy.x, -xy.y ) * _CameraMotionVectorsTexture_TexelSize.xy + aUV );
	const half2 xRyD = FetchMotion( half2( +xy.x, +xy.y ) * _CameraMotionVectorsTexture_TexelSize.xy + aUV );

	const half2 xLyT_v = xLyT - aCentralmv;
	const half2 xRyD_v = xRyD - aCentralmv;

	const half2 sqrLength2 = half2( dot( xLyT_v, xLyT_v ), dot( xRyD_v, xRyD_v ) );
	const half neighborMotion = max( sqrLength2.x, sqrLength2.y );
	const half oneOverPixelDeltaSquared = 1.0 / ( _CameraMotionVectorsTexture_TexelSize.x * _CameraMotionVectorsTexture_TexelSize.x );

	const half outNeighborMotion = saturate( neighborMotion * oneOverPixelDeltaSquared - 1.0 ) * _AO_TemporalMotionSensibility;

	return outNeighborMotion;
}


half NeighborMotionIntensity( v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const float2 screenPos = IN.uv.xy;

	const half depthSample = SampleDepth0( screenPos );

	if( depthSample < HALF_MAX )
	{
		half linearEyeDepth = LinearEyeDepth( depthSample );

		const half radius = lerp( _AO_Radius, _AO_FadeValues.y, ComputeDistanceFade( linearEyeDepth ) );
		const half radiusToScreen = radius * _AO_HalfProjScale;
		const half screenRadius = max( min( ( radiusToScreen / linearEyeDepth ), PIXEL_RADIUS_LIMIT ), 2 );

		const half2 cMv = FetchMotion( screenPos );

		const half motionNeighborDisoclusion = FetchNeighborMotion( screenPos, screenRadius, cMv );

		return motionNeighborDisoclusion;
	}
	else
	{
		return 0;
	}
}


half4 ClearTemporal( const v2f_in ifrag )
{
	UNITY_SETUP_INSTANCE_ID( ifrag );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( ifrag );

	return half4( EncAO( 0.99 ), 1.0, 1.0, 0.0 );
}


float ScaleDownCloserDepthEven( v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	// Snap uv to source pixel center
	const half2 screenPos = floor( ( IN.uv.xy + half2( 1.0, 1.0 ) * _AO_CurrDepthSource_TexelSize.xy ) * _AO_CurrDepthSource_TexelSize.zw ) * _AO_CurrDepthSource_TexelSize.xy - half2( 0.5, 0.5 ) * _AO_CurrDepthSource_TexelSize.xy;

	const float d0 = FetchCurrDepthSource( screenPos + half2( 0.0, 0.0 ) * _AO_CurrDepthSource_TexelSize.xy );
	const float d1 = FetchCurrDepthSource( screenPos + half2( 1.0, 0.0 ) * _AO_CurrDepthSource_TexelSize.xy );
	const float d2 = FetchCurrDepthSource( screenPos + half2( 0.0, 1.0 ) * _AO_CurrDepthSource_TexelSize.xy );
	const float d3 = FetchCurrDepthSource( screenPos + half2( 1.0, 1.0 ) * _AO_CurrDepthSource_TexelSize.xy );

	#if defined(UNITY_REVERSED_Z)
	const float closerDepth = max( d0, max( d1, max( d2, d3 ) ) );
	#else
	const float closerDepth = min( d0, min( d1, min( d2, d3 ) ) );
	#endif

	return closerDepth;
}


float ScaleDownCloserDepthEven_CameraDepthTexture( v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	// Snap uv to source pixel center
	const half2 screenPos = floor( ( IN.uv.xy + half2( 1.0, 1.0 ) * _CameraDepthTexture_TexelSize.xy ) * _CameraDepthTexture_TexelSize.zw ) * _CameraDepthTexture_TexelSize.xy - half2( 0.5, 0.5 ) * _CameraDepthTexture_TexelSize.xy;

	const float d0 = SampleDepth0( screenPos + half2( 0.0, 0.0 ) * _CameraDepthTexture_TexelSize.xy );
	const float d1 = SampleDepth0( screenPos + half2( 1.0, 0.0 ) * _CameraDepthTexture_TexelSize.xy );
	const float d2 = SampleDepth0( screenPos + half2( 0.0, 1.0 ) * _CameraDepthTexture_TexelSize.xy );
	const float d3 = SampleDepth0( screenPos + half2( 1.0, 1.0 ) * _CameraDepthTexture_TexelSize.xy );

	#if defined(UNITY_REVERSED_Z)
	const float closerDepth = max( d0, max( d1, max( d2, d3 ) ) );
	#else
	const float closerDepth = min( d0, min( d1, min( d2, d3 ) ) );
	#endif

	return closerDepth;
}

#endif