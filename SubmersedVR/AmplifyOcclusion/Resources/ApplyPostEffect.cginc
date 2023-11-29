// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_APPLY_POSTEFFECT
#define AMPLIFY_AO_APPLY_POSTEFFECT

struct PostEffectOutputTemporal
{
	half4 occlusionColor : SV_Target0;
	half4 temporalAcc : SV_Target1;
};

half4 ApplyDebug( const v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 screenPos = IN.uv.xy;

	const half2 occlusionDepth = FetchOcclusionDepth( screenPos );

	const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

	const half4 occlusionRGBA = CalcOcclusion( occlusionDepth.x, linearEyeDepth );

	return half4( occlusionRGBA.rgb, 1 );
}


half4 ApplyDebugCombineFromTemporal( const v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 screenPos = IN.uv.xy;

	const half depthSample = SampleDepth0( screenPos );

	const half2 occlusionLinearEyeDepth = ComputeCombineDownsampledOcclusionFromTemporal( screenPos, depthSample );

	const half4 occlusionRGBA = CalcOcclusion( occlusionLinearEyeDepth.x, occlusionLinearEyeDepth.y );

	return half4( occlusionRGBA.rgb, 1 );
}


half4 ApplyCombineFromTemporal( const v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 screenPos = IN.uv.xy;

	const half depthSample = SampleDepth0( screenPos );

	const half2 occlusionLinearEyeDepth = ComputeCombineDownsampledOcclusionFromTemporal( screenPos, depthSample );

	const half4 occlusionRGBA = CalcOcclusion( occlusionLinearEyeDepth.x, occlusionLinearEyeDepth.y );

	return half4( ( occlusionLinearEyeDepth.y < HALF_MAX )?occlusionRGBA.rgb:(1).xxx, 1 );
}


PostEffectOutputTemporal ApplyDebugTemporal( const v2f_in IN, const bool aUseMotionVectors )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const float2 screenPos = IN.uv.xy;

	const half2 occlusionDepth = FetchOcclusionDepth( screenPos );

	PostEffectOutputTemporal OUT;

	if( occlusionDepth.y < HALF_MAX )
	{
		const half linear01Depth = occlusionDepth.y * ONE_OVER_DEPTH_SCALE;
		const half sampledDepth = Linear01ToSampledDepth( linear01Depth );
		const half linearEyeDepth = occlusionDepth.y * _AO_BufDepthToLinearEye;

		half occlusion;
		const half4 temporalAcc = TemporalFilter( screenPos, occlusionDepth.x, sampledDepth, linearEyeDepth, linear01Depth, aUseMotionVectors, occlusion );

		const half4 occlusionRGBA = CalcOcclusion( occlusion, linearEyeDepth );

		OUT.occlusionColor = occlusionRGBA;
		OUT.temporalAcc = temporalAcc;
	}
	else
	{
		OUT.occlusionColor = half4( (1).xxxx );
		OUT.temporalAcc = half4( (1).xxxx );
	}

	return OUT;
}


#endif
