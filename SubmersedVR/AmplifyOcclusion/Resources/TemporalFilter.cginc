// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_TEMPORALFILTER
#define AMPLIFY_AO_TEMPORALFILTER


//#define NEIGHBOR_SAMPLE_4TAP
#define NEIGHBOR_SAMPLE_4TAP_CROSS
//#define NEIGHBOR_SAMPLE_8TAP

#define CLAMP_MINMAX

#define DEPTH_WEIGHTING ( _AO_TemporalMotionSensibility * 1000 + 200.0 )
#define DEPTH_WEIGHTING_OFFSET ( 0.10 )

//#define MINMAX_DEVIATION
#define VARIANCE_CLIPPING ( 7.0 )
#define VARIANCE_CLIPPING_OFFSET ( 1.5 )

#define SIGMA_SCALE_MAX ( 0.75 )
#define SIGMA_SCALE_MIN ( 1.00 )

#define MOTION_ATTEN
#define DISOCCLUSION_ATTEN
#define DISOCCLUSION_NEIGHBOR
#define OUTOFRANGE_COMPENSATION
#define OUTOFRANGE_ENDCOMPENSATION


float4x4	_AO_InvViewProjMatrixLeft;
float4x4	_AO_PrevViewProjMatrixLeft;
float4x4	_AO_PrevInvViewProjMatrixLeft;

float4x4	_AO_InvViewProjMatrixRight;
float4x4	_AO_PrevViewProjMatrixRight;
float4x4	_AO_PrevInvViewProjMatrixRight;

float		_AO_TemporalCurveAdj;

inline half CalcDisocclusion( const half aDepth, const half aPrevDepth, const half aSensibility )
{
	const half depthDiff = abs( aDepth - aPrevDepth );
	return saturate( aSensibility * depthDiff - 0.02 );
}


inline half2 FetchPrevAO_Depth( const float2 aUV )
{
	float2 uv = aUV;
/*#if UNITY_UV_STARTS_AT_TOP
	uv.y = ( _ProjectionParams.x > 0 ) ? 1 - aUV.y : aUV.y;
#endif*/
	const half4 temporalAccummEnc = FetchTemporal( uv );

	return half2( DecAO( temporalAccummEnc.x ), DecDepth( temporalAccummEnc.yz ) );
}


inline half3 FetchPrevAO_Depth_N( const float2 aUV )
{
	float2 uv = aUV;
#if UNITY_UV_STARTS_AT_TOP
	uv.y = ( _ProjectionParams.x > 0 ) ? 1 - aUV.y : aUV.y;
#endif
	const half4 temporalAccummEnc = FetchTemporal( uv );

	return half3( DecAO( temporalAccummEnc.x ), DecDepth( temporalAccummEnc.yz ), temporalAccummEnc.w );
}


inline half FetchPrevN( const float2 aUV )
{
	float2 uv = aUV;
#if UNITY_UV_STARTS_AT_TOP
	uv.y = ( _ProjectionParams.x > 0 ) ? 1 - aUV.y : aUV.y;
#endif
	const half4 temporalAccummEnc = FetchTemporal( uv );

	return temporalAccummEnc.w;
}


#if defined( VARIANCE_CLIPPING )

inline half ComputeSigma( const half aInSigma, const half aMotionNeighborIntensity, const half aDisocclusion, const half aPrevN )
{
	// "Larger gammas produced more temporally stable results at the cost of increased ghosting." - msalvi_temporal_supersampling.pdf - page 24
	half outSigma = VARIANCE_CLIPPING * ( VARIANCE_CLIPPING_OFFSET - _AO_TemporalMotionSensibility ) * aInSigma;

	#if defined( MOTION_ATTEN )
	outSigma = outSigma * ( 1.0 - aMotionNeighborIntensity );
	#endif

	#if defined( DISOCCLUSION_ATTEN )
	outSigma = outSigma * ( 1.0 - aDisocclusion );
	#endif

	outSigma = outSigma * aPrevN;

	return max( outSigma, 0.0 );
}

#else

#if defined( MINMAX_DEVIATION )

inline half ComputeDeviation( const half aMotionNeighborIntensity, const half aDisocclusion, const half aPrevN, const half aOutOfRange )
{
	half outDeviation = 0.15 * ( ( 1.25 ) - _AO_TemporalMotionSensibility ) + aOutOfRange;

	#if defined( MOTION_ATTEN )
	outDeviation = outDeviation * ( 1.0 - aMotionNeighborIntensity );
	#endif

	#if defined( DISOCCLUSION_ATTEN )
	outDeviation = outDeviation * ( 1.0 - aDisocclusion );
	#endif

	outDeviation = outDeviation * aPrevN;

	return saturate( outDeviation );
}

#endif

#endif


#if defined( DISOCCLUSION_NEIGHBOR )

inline half FetchNeighborPrevN( const half2 aUV, const half aRadiusPixels )
{
	half noiseSpatialOffsets, noiseSpatialDirections;
	GetSpatialDirections_Offsets_JimenezNoise( aUV, _AO_TemporalAccumm_TexelSize.zw, noiseSpatialOffsets, noiseSpatialDirections );

	const half angle = ( noiseSpatialDirections + _AO_TemporalDirections ) * UNITY_PI;
	const half2 cos_sin = half2( cos( angle ), sin( angle ) );
	const half2 xy = ( aRadiusPixels * noiseSpatialOffsets + 2.0 ).xx * cos_sin;

	const half dL = FetchPrevN( half2( -xy.x, -xy.y ) * _AO_TemporalAccumm_TexelSize.xy + aUV );
	const half dR = FetchPrevN( half2( +xy.x, +xy.y ) * _AO_TemporalAccumm_TexelSize.xy + aUV );

	return min( dL, dR );
}


half NeighborPrevN( const float2 aScreenPos, const half aLinearEyeDepth )
{
	const half radius = lerp( _AO_Radius, _AO_FadeValues.y, ComputeDistanceFade( aLinearEyeDepth ) );
	const half radiusToScreen = 0.50 * radius * _AO_HalfProjScale;
	const half screenRadius = max( min( ( radiusToScreen / aLinearEyeDepth ), PIXEL_RADIUS_LIMIT ), 2 );

	return FetchNeighborPrevN( aScreenPos, screenRadius );
}

#endif
void GetTemporalFilter( const float2 aScreenPos,
						const half aCurrAO,
						const half aSampledDepth,
						const half aLinearEyeDepth,
						const half aLinear01Depth,
						const bool aUseMotionVectors,
						out half outAO,
						out half outNewN,
						out half out_temporal_AO )
{

	float2 reproj_screenPos = (0).xx;

	half2 mv = (0).xx;

	half motionNeighborIntensity = 0;

	if( aUseMotionVectors == false )
	{
		half depth01 = aSampledDepth;

		#if defined(UNITY_REVERSED_Z)
		depth01 = 1.0 - depth01;
		#endif

		#if defined( SHADER_API_OPENGL ) || defined( SHADER_API_GLES ) || defined( SHADER_API_GLES3 ) || defined( SHADER_API_GLCORE )
		const float4 vpos = float4( float3( aScreenPos, depth01 ) * 2.0 - 1.0, 1.0 );
		#else
		const float4 vpos = float4( ( aScreenPos * 2.0 - 1.0 ), ( 1.0 - depth01 ), 1.0 );
		#endif

		#ifdef UNITY_SINGLE_PASS_STEREO
		float4x4 invViewProjMatrix;

		if ( unity_StereoEyeIndex == 0 )
		{
			invViewProjMatrix = _AO_InvViewProjMatrixLeft;
		}
		else
		{
			invViewProjMatrix = _AO_InvViewProjMatrixRight;
		}

		float4 wpos = mul( invViewProjMatrix, vpos );

		#else

		float4 wpos = mul( _AO_InvViewProjMatrixLeft, vpos );

		#endif

		wpos = wpos / wpos.w;

		#ifdef UNITY_SINGLE_PASS_STEREO
		float4x4 prevViewProjMatrix;

		if ( unity_StereoEyeIndex == 0 )
		{
			prevViewProjMatrix = _AO_PrevViewProjMatrixLeft;
		}
		else
		{
			prevViewProjMatrix = _AO_PrevViewProjMatrixRight;
		}

		const float4 reproj_vpos = mul( prevViewProjMatrix, wpos );

		#else
		const float4 reproj_vpos = mul( _AO_PrevViewProjMatrixLeft, wpos );
		#endif

		reproj_screenPos = ( reproj_vpos.xy / reproj_vpos.w ) * 0.5 + 0.5;

		mv = aScreenPos - reproj_screenPos;
	}
	else
	{
		mv = FetchMotion( aScreenPos );

		motionNeighborIntensity = FetchMotionIntensity( aScreenPos );

		reproj_screenPos = aScreenPos - mv;
	}


	if( ( ( reproj_screenPos.x < 0.0 ) ||
		  ( reproj_screenPos.y < 0.0 ) ||
		  ( reproj_screenPos.x > 1.0 ) ||
		  ( reproj_screenPos.y > 1.0 ) ) == false )
	{
		const half3 prev_AO_Depth_N = FetchPrevAO_Depth_N( reproj_screenPos );

		const half cM = aCurrAO;
		const half cAcc = prev_AO_Depth_N.x;
		half prev_linear01Depth = prev_AO_Depth_N.y;
		half prev_sampledDepth = Linear01ToSampledDepth( prev_linear01Depth );
		half prev_N = prev_AO_Depth_N.z;

		const half mvLenght = length( mv );

		half disocclusion = CalcDisocclusion( aSampledDepth, prev_sampledDepth, ( _AO_TemporalMotionSensibility * 20.0 + 1.0 ) * aLinearEyeDepth );

		#if defined( DISOCCLUSION_NEIGHBOR )
		if( aUseMotionVectors == false )
		{
			prev_N = lerp( prev_N, min( prev_N , NeighborPrevN( aScreenPos, aLinearEyeDepth ) ), saturate( _AO_TemporalMotionSensibility * 0.50 + 0.50 ) );
		}
		#endif

		#if defined( NEIGHBOR_SAMPLE_4TAP )

		// "variable distance to 4 sample points, decided per-pixel"
		// "higher velocity ⇒ closer to center texel (strict on motion)" - slide 26
		// "sample offset 0.5-0.666 from texel center" - slide 26
		const half vd = lerp( 0.71, 0.50, mvLenght );

		half2 cTL = FetchOcclusionDepth( half2( -vd, -vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cTR = FetchOcclusionDepth( half2( +vd, -vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cBL = FetchOcclusionDepth( half2( -vd, +vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cBR = FetchOcclusionDepth( half2( +vd, +vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );

		half4 o0123 = half4( cTL.x, cTR.x, cBL.x, cBR.x );

		#if defined( DEPTH_WEIGHTING )
		const half4 d0123 = half4( cTL.y, cTR.y, cBL.y, cBR.y );
		half4 depthWeight0123 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d0123 * ONE_OVER_DEPTH_SCALE ) - ( aSampledDepth ).xxxx ) * DEPTH_WEIGHTING + DEPTH_WEIGHTING_OFFSET ) );
		depthWeight0123 = depthWeight0123 * depthWeight0123;
		const half depthVariance = dot( (1).xxxx, depthWeight0123 ) * 0.25;
		o0123 = lerp( (cM).xxxx, o0123, depthWeight0123 );
		cTL.x = o0123.x;
		cTR.x = o0123.y;
		cBL.x = o0123.z;
		cBR.x = o0123.w;
		#endif

		#if defined( VARIANCE_CLIPPING )
		// Salvi 2016 - msalvi_temporal_supersampling.pdf - page 23
		const half m1 = dot( (1).xxxx, o0123 );

		const half4 o0123_squared = o0123 * o0123;

		const half m2 = dot( (1).xxxx, o0123_squared );

		const half oneOverN = 1.0 / 4.0;
		const half mu = m1 * oneOverN;
		float Sigma = sqrt( max( m2 * oneOverN - mu * mu, 0.0 ) );

		Sigma = ComputeSigma( Sigma, motionNeighborIntensity, disocclusion, prev_N );

		half cMin = max( mu - Sigma * SIGMA_SCALE_MIN, 0.0 );
		half cMax = min( mu + Sigma * SIGMA_SCALE_MAX, 1.0 );
		#else
		half cMax = max( aCurrAO, max( cTL.x, max( cTR.x, max( cBL.x, cBR.x ) ) ) );
		half cMin = min( aCurrAO, min( cTL.x, min( cTR.x, min( cBL.x, cBR.x ) ) ) );
		#endif


		const half2 cAvrg = ( cTL + cTR + cBL + cBR ) * (0.25).xx;
		#else
		#if defined( NEIGHBOR_SAMPLE_4TAP_CROSS )
		const half vd = 1.0;
		half2 cT = FetchOcclusionDepth( half2(   0, -vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cL = FetchOcclusionDepth( half2( -vd,   0 ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cR = FetchOcclusionDepth( half2( +vd,   0 ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cB = FetchOcclusionDepth( half2(   0, +vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );

		half4 o0123 = half4( cT.x, cL.x, cR.x, cB.x );

		#if defined( DEPTH_WEIGHTING )
		const half4 d0123 = half4( cT.y, cL.y, cR.y, cB.y );
		half4 depthWeight0123 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d0123 * ONE_OVER_DEPTH_SCALE ) - ( aSampledDepth ).xxxx ) * DEPTH_WEIGHTING + DEPTH_WEIGHTING_OFFSET ) );
		depthWeight0123 = depthWeight0123 * depthWeight0123;
		const half depthVariance = dot( (1).xxxx, depthWeight0123 ) * 0.25;
		o0123 = lerp( (cM).xxxx, o0123, depthWeight0123 );
		cT.x = o0123.x;
		cL.x = o0123.y;
		cR.x = o0123.z;
		cB.x = o0123.w;
		#endif

		#if defined( VARIANCE_CLIPPING )
		// Salvi 2016 - msalvi_temporal_supersampling.pdf - page 23
		const half m1 = dot( (1).xxxx, o0123 );

		const half4 o0123_squared = o0123 * o0123;

		const half m2 = dot( (1).xxxx, o0123_squared );

		const half oneOverN = 1.0 / 4.0;
		const half mu = m1 * oneOverN;
		float Sigma = sqrt( max( m2 * oneOverN - mu * mu, 0.0 ) );

		Sigma = ComputeSigma( Sigma, motionNeighborIntensity, disocclusion, prev_N );

		half cMin = max( mu - Sigma * SIGMA_SCALE_MIN, 0.0 );
		half cMax = min( mu + Sigma * SIGMA_SCALE_MAX, 1.0 );
		#else
		half cMax = max( aCurrAO, max( cT.x, max( cL.x, max( cR.x, cB.x ) ) ) );
		half cMin = min( aCurrAO, min( cT.x, min( cL.x, min( cR.x, cB.x ) ) ) );
		#endif


		const half2 cAvrg = ( cT + cL + cR + cB ) * (0.25).xx;
		#else
		#if defined( NEIGHBOR_SAMPLE_8TAP )
		const half vd = 1.0;
		half2 cT = FetchOcclusionDepth( half2(   0, -vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cL = FetchOcclusionDepth( half2( -vd,   0 ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cR = FetchOcclusionDepth( half2( +vd,   0 ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cB = FetchOcclusionDepth( half2(   0, +vd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );

		const half dd = 1.55;
		half2 cTL = FetchOcclusionDepth( half2( -dd, -dd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cTR = FetchOcclusionDepth( half2( +dd, -dd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cBL = FetchOcclusionDepth( half2( -dd, +dd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );
		half2 cBR = FetchOcclusionDepth( half2( +dd, +dd ) * _AO_CurrOcclusionDepth_TexelSize.xy + aScreenPos );

		half4 o0123 = half4( cT.x, cL.x, cR.x, cB.x );
		half4 o4567 = half4( cTL.x, cTR.x, cBL.x, cBR.x );

		#if defined( DEPTH_WEIGHTING )
		const half4 d0123 = half4( cT.y, cL.y, cR.y, cB.y );
		half4 depthWeight0123 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d0123 * ONE_OVER_DEPTH_SCALE ) - ( aSampledDepth ).xxxx ) * DEPTH_WEIGHTING + DEPTH_WEIGHTING_OFFSET ) );
		depthWeight0123 = depthWeight0123 * depthWeight0123;
		o0123 = lerp( (cM).xxxx, o0123, depthWeight0123 );
		cT.x = o0123.x;
		cL.x = o0123.y;
		cR.x = o0123.z;
		cB.x = o0123.w;

		const half4 d4567 = half4( cTL.y, cTR.y, cBL.y, cBR.y );
		half4 depthWeight4567 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d4567 * ONE_OVER_DEPTH_SCALE ) - ( aSampledDepth ).xxxx ) * DEPTH_WEIGHTING + DEPTH_WEIGHTING_OFFSET ) );
		depthWeight4567 = depthWeight4567 * depthWeight4567;
		o4567 = lerp( (cM).xxxx, o4567, depthWeight4567 );
		cTL.x = o4567.x;
		cTR.x = o4567.y;
		cBL.x = o4567.z;
		cBR.x = o4567.w;

		const half depthVariance = ( dot( (1).xxxx, depthWeight0123 ) + dot( (1).xxxx, depthWeight4567 ) ) * 0.125;
		#endif

		#if defined( VARIANCE_CLIPPING )
		// Salvi 2016 - msalvi_temporal_supersampling.pdf - page 23
		const half m1 = dot( (1).xxxx, o0123 ) + dot( (1).xxxx, o4567 );

		const half4 o0123_squared = o0123 * o0123;
		const half4 o4567_squared = o4567 * o4567;

		const half m2 = dot( (1).xxxx, o0123_squared ) + dot( (1).xxxx, o4567_squared );

		const half oneOverN = 1.0 / 8.0;
		const half mu = m1 * oneOverN;
		float Sigma = sqrt( max( m2 * oneOverN - mu * mu, 0.0 ) );

		Sigma = ComputeSigma( Sigma, motionNeighborIntensity, disocclusion, prev_N );

		half cMin = max( mu - Sigma * SIGMA_SCALE_MIN, 0.0 );
		half cMax = min( mu + Sigma * SIGMA_SCALE_MAX, 1.0 );
		#else
		half cMax = max( aCurrAO, max( cTL.x, max( cTR.x, max( cBL.x, max( cBR.x, max( cT.x, max( cL.x, max( cR.x, cB.x ) ) ) ) ) ) ) );
		half cMin = min( aCurrAO, min( cTL.x, min( cTR.x, min( cBL.x, min( cBR.x, min( cT.x, min( cL.x, min( cR.x, cB.x ) ) ) ) ) ) ) );
		#endif

		const half2 cAvrg = ( cT + cL + cR + cB + cTL + cTR + cBL + cBR ) * (0.125).xx;
		#endif
		#endif
		#endif

		#if defined( OUTOFRANGE_COMPENSATION )

		#if defined( VARIANCE_CLIPPING )
		half accOutOfRange = saturate( ( max( abs( cAcc - cMin.x ), abs( cAcc - cMax.x ) ) * 3.5 - 0.03 ) );
		#else
		const half accOutOfRange = saturate( ( max( abs( cAcc - cMin.x ), abs( cAcc - cMax.x ) ) - 0.01 ) );
		#endif

		#else
		const half accOutOfRange = 0.0;
		#endif

		#if defined( MINMAX_DEVIATION )
		const half deviation = ComputeDeviation( motionNeighborIntensity, disocclusion, prev_N, accOutOfRange );
		cMax.x = min( cMax.x + deviation * SIGMA_SCALE_MAX, 1.0 );
		cMin.x = max( cMin.x - deviation * SIGMA_SCALE_MIN, 0.0 );
		#endif

		#if defined( CLAMP_MINMAX )
		half clampedAcc = clamp( cAcc, cMin.x, cMax.x );

		#if defined( VARIANCE_CLIPPING )
		#if defined( OUTOFRANGE_COMPENSATION )
		clampedAcc = lerp( clampedAcc, cAcc, accOutOfRange );
		#endif
		#endif

		#else
		half clampedAcc = cM;
		#endif

		half newN = saturate( prev_N + ( 1.0 / 6.0 ) );
		const half oneMinusDisocclusion = ( 1.0 - disocclusion );
		newN = min( newN, oneMinusDisocclusion * oneMinusDisocclusion );

		if( aUseMotionVectors == true )
		{
			newN = min( newN, 1.0 - motionNeighborIntensity );
		}

		#if defined( MOTION_ATTEN )
		const half2 mvToPixels = mv * _AO_CurrOcclusionDepth_TexelSize.zw;
		const half mvLenghtPixelsSquared = dot( mvToPixels, mvToPixels );
		const half mvWeighting = saturate( mvLenghtPixelsSquared * 0.0005 * _AO_TemporalMotionSensibility );
		const half mvMaxMotion = saturate( max( motionNeighborIntensity, mvWeighting ) );
		const half mvIntensity = ( 1.0 - mvMaxMotion );

		const half finalLerp = min( saturate( prev_N * mvIntensity - disocclusion ), 0.96 );
		#else
		const half finalLerp = min( saturate( prev_N - disocclusion ), 0.96 );
		#endif

		half new_ao = lerp( cM, lerp( clampedAcc, cAcc, _AO_TemporalCurveAdj ), finalLerp );

		#if defined( OUTOFRANGE_ENDCOMPENSATION )
		half outOfRangeCompensation = saturate( abs( new_ao - cAcc ) * ( 0.3 + _AO_TemporalMotionSensibility ) );

		new_ao = lerp( new_ao, cM, outOfRangeCompensation );
		newN = saturate( newN - min( outOfRangeCompensation * outOfRangeCompensation, 1.0 / 7.0 ) );
		#endif

		out_temporal_AO = new_ao;
		outAO = new_ao;
		outNewN = newN;
	}
	else
	{
		out_temporal_AO = aCurrAO;
		outAO = aCurrAO;
		outNewN = 0.0;
	}
}


half4 TemporalFilter( const half2 aScreenPos, const half aOcclusion, const half aSampledDepth, const half aLinearEyeDepth, const half aLinear01Depth, const bool aUseMotionVectors, out half outOcclusion )
{
	half out_ao, out_newN, out_temporal_ao;

	GetTemporalFilter(	aScreenPos,
						aOcclusion,
						aSampledDepth,
						aLinearEyeDepth,
						aLinear01Depth,
						aUseMotionVectors,
						out_ao,
						out_newN,
						out_temporal_ao );

	outOcclusion = out_ao;

	return half4( EncAO( out_temporal_ao ), EncDepth( aLinear01Depth ), out_newN );
}


half4 Temporal( v2f_in IN, const bool aUseMotionVectors )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const float2 screenPos = IN.uv.xy;

	const half2 occlusionDepth = FetchOcclusionDepth( screenPos );

	if( occlusionDepth.y < HALF_MAX )
	{
		const half linear01Depth = occlusionDepth.y * ONE_OVER_DEPTH_SCALE;
		const half sampledDepth = Linear01ToSampledDepth( linear01Depth );
		const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

		half dummy_occlusion;
		return TemporalFilter( screenPos, occlusionDepth.x, sampledDepth, linearEyeDepth, linear01Depth, aUseMotionVectors, dummy_occlusion );
	}
	else
	{
		return half4( (1).xxxx );
	}
}


inline half2 ComputeCombineDownsampledOcclusionFromTemporal( const half2 aScreenPos, const half aDepthSample )
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
		return half2( 1.0, HALF_MAX );
	}

	const half2 screenPosPixels = aScreenPos * _AO_TemporalAccumm_TexelSize.zw;
	const half2 screenPosPixelsFloor = floor( screenPosPixels );
	const half2 screenPosPixelsDelta = screenPosPixels - screenPosPixelsFloor;

	const half2 sPosAdjusted = screenPosPixelsFloor * _AO_TemporalAccumm_TexelSize.xy + half2( 0.5, 0.5 ) * _AO_TemporalAccumm_TexelSize.xy;
	const half s = ( screenPosPixelsDelta.y < 0.5 )?-1.0:1.0;

	half2 odC = FetchPrevAO_Depth( sPosAdjusted );

	half2 odL = FetchPrevAO_Depth( sPosAdjusted + half2( -1.0, 0.0 ) * _AO_TemporalAccumm_TexelSize.xy );
	half2 odR = FetchPrevAO_Depth( sPosAdjusted + half2( +1.0, 0.0 ) * _AO_TemporalAccumm_TexelSize.xy );
	half2 odM = FetchPrevAO_Depth( sPosAdjusted + half2(  0.0,   s ) * _AO_TemporalAccumm_TexelSize.xy );

	const half4 o0123 = half4( odC.x, odL.x, odR.x, odM.x );
	const half4 d0123 = half4( odC.y, odL.y, odR.y, odM.y );

	half4 depthWeight0123 = saturate( 1.0 / ( abs( Linear01ToSampledDepth( d0123 ) - ( aDepthSample ).xxxx ) * 32768 + 0.95 ) );

	const half4 pixelDeltaWeight = half4( screenPosPixelsDelta.x * screenPosPixelsDelta.y + 0.5,
											1.0 - screenPosPixelsDelta.x,
											screenPosPixelsDelta.x,
											0.80 );

	depthWeight0123 = depthWeight0123 * depthWeight0123 * pixelDeltaWeight;

	half weightOcclusion = dot( o0123, depthWeight0123 );

	const half outOcclusion = saturate( weightOcclusion / dot( ( 1 ).xxxx, depthWeight0123 ) );

	return half2( outOcclusion, referenceDepth );
}

#endif
