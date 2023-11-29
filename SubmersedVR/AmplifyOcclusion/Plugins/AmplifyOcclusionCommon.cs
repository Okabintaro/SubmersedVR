// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using Valve.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AmplifyOcclusion
{

public static class AmplifyOcclusionCommon
{
	public static readonly int PerPixelNormalSourceCount = 4;
	public static readonly float[] m_temporalRotations = { 60.0f, 300.0f, 180.0f, 240.0f, 120.0f, 0.0f };
	public static readonly float[] m_spatialOffsets = { 0.0f, 0.5f, 0.25f, 0.75f };

	public static void CommandBuffer_TemporalFilterDirectionsOffsets( CommandBuffer cb, uint aSampleStep )
	{
		float temporalRotation = AmplifyOcclusionCommon.m_temporalRotations[ aSampleStep % 6 ];
		float temporalOffset = AmplifyOcclusionCommon.m_spatialOffsets[ ( aSampleStep / 6 ) % 4 ];

		cb.SetGlobalFloat( PropertyID._AO_TemporalDirections, temporalRotation / 360.0f );
		cb.SetGlobalFloat( PropertyID._AO_TemporalOffsets, temporalOffset );
	}

	public static Material CreateMaterialWithShaderName( string aShaderName, bool aThroughErrorMsg )
	{
		var shader = ShaderLoader.GetShader(aShaderName);

		if( shader == null )
		{
			if( aThroughErrorMsg == true )
			{
				Debug.LogErrorFormat( "[AmplifyOcclusion] Cannot find shader: \"{0}\"" +
										" Please contact support@amplify.pt", aShaderName );
			}

			return null;
		}

		return new Material( shader ) { hideFlags = HideFlags.DontSave };
	}

	public static int SafeAllocateTemporaryRT( CommandBuffer cb, string propertyName,
												int width, int height,
												RenderTextureFormat format = RenderTextureFormat.Default,
												RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
												FilterMode filterMode = FilterMode.Point )
	{
		int id = Shader.PropertyToID( propertyName );

		cb.GetTemporaryRT( id, width, height, 0, filterMode, format, readWrite );

		return id;
	}


	public static void SafeReleaseTemporaryRT( CommandBuffer cb, int id )
	{
		cb.ReleaseTemporaryRT( id );
	}


	public static RenderTexture SafeAllocateRT(	string name,
												int width, int height,
												RenderTextureFormat format,
												RenderTextureReadWrite readWrite,
												FilterMode filterMode = FilterMode.Point,
												int antiAliasing = 1,
												bool aUseMipMap = false )
	{
		width = Mathf.Clamp( width, 1, 65536 );
		height = Mathf.Clamp( height, 1, 65536 );

		RenderTexture rt = new RenderTexture( width, height, 0, format, readWrite ) { hideFlags = HideFlags.DontSave };

		rt.name = name;
		rt.filterMode = filterMode;
		rt.wrapMode = TextureWrapMode.Clamp;
		rt.antiAliasing = Mathf.Max( antiAliasing, 1 );
		rt.useMipMap = aUseMipMap;
		rt.Create();

		return rt;
	}


	public static void SafeReleaseRT( ref RenderTexture rt )
	{
		if( rt != null )
		{
			RenderTexture.active = null;

			rt.Release();
			RenderTexture.DestroyImmediate( rt );

			rt = null;
		}
	}


	public static bool IsStereoSinglePassEnabled( Camera aCamera )
	{
	#if UNITY_EDITOR
		return aCamera.stereoEnabled && ( PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass );
	#else

		#if UNITY_2017_2_OR_NEWER && !UNITY_SWITCH && !UNITY_XBOXONE && !UNITY_PS4
			return	aCamera.stereoEnabled && ( UnityEngine.XR.XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes );
		#else
			return	false;
		#endif

	#endif
	}

	public static bool IsStereoMultiPassEnabled( Camera aCamera )
	{
		return true;
	}

	public static void UpdateGlobalShaderConstants( CommandBuffer cb, ref TargetDesc aTarget, Camera aCamera, bool isDownsample, bool isFilterDownsample )
	{
	#if UNITY_2017_2_OR_NEWER && !UNITY_SWITCH && !UNITY_XBOXONE && !UNITY_PS4
		if( UnityEngine.XR.XRSettings.enabled == true )
		{
			aTarget.fullWidth = (int)( UnityEngine.XR.XRSettings.eyeTextureDesc.width * UnityEngine.XR.XRSettings.eyeTextureResolutionScale );
			aTarget.fullHeight = (int)( UnityEngine.XR.XRSettings.eyeTextureDesc.height * UnityEngine.XR.XRSettings.eyeTextureResolutionScale );
		}
		else
		{
			aTarget.fullWidth = aCamera.pixelWidth;
			aTarget.fullHeight = aCamera.pixelHeight;
		}
	#else
		aTarget.fullWidth = aCamera.pixelWidth;
		aTarget.fullHeight = aCamera.pixelHeight;
	#endif

		if( isFilterDownsample == true )
		{
			aTarget.width = aTarget.fullWidth / 2;
			aTarget.height = aTarget.fullHeight / 2;
		}
		else
		{
			aTarget.width = aTarget.fullWidth;
			aTarget.height = aTarget.fullHeight;
		}

		aTarget.oneOverWidth = 1.0f / (float)aTarget.width;
		aTarget.oneOverHeight = 1.0f / (float)aTarget.height;

		float fovRad = aCamera.fieldOfView * Mathf.Deg2Rad;

		float invHalfTanFov = 1.0f / Mathf.Tan( fovRad * 0.5f );

		Vector2 focalLen = new Vector2( invHalfTanFov * ( aTarget.height / (float)aTarget.width ),
										invHalfTanFov );

		Vector2 invFocalLen = new Vector2( 1.0f / focalLen.x, 1.0f / focalLen.y );

		// Aspect Ratio
		cb.SetGlobalVector( PropertyID._AO_UVToView, new Vector4( +2.0f * invFocalLen.x,
																  +2.0f * invFocalLen.y,
																  -1.0f * invFocalLen.x,
																  -1.0f * invFocalLen.y ) );

		float projScale;

		if( aCamera.orthographic )
			projScale = ( (float)aTarget.fullHeight ) / aCamera.orthographicSize;
		else
			projScale = ( (float)aTarget.fullHeight ) / ( Mathf.Tan( fovRad * 0.5f ) * 2.0f );

		if( ( isDownsample == true ) || ( isFilterDownsample == true ) )
		{
			projScale = projScale * 0.5f * 0.5f;
		}
		else
		{
			projScale = projScale * 0.5f;
		}

		cb.SetGlobalFloat( PropertyID._AO_HalfProjScale, projScale );
	}
}


public class AmplifyOcclusionViewProjMatrix
{
	private Matrix4x4 m_prevViewProjMatrixLeft = Matrix4x4.identity;
	private Matrix4x4 m_prevInvViewProjMatrixLeft = Matrix4x4.identity;
	private Matrix4x4 m_prevViewProjMatrixRight = Matrix4x4.identity;
	private Matrix4x4 m_prevInvViewProjMatrixRight = Matrix4x4.identity;

	public void UpdateGlobalShaderConstants_Matrices( CommandBuffer cb, Camera aCamera, bool isUsingTemporalFilter )
	{
		// Camera matrixes
		if( AmplifyOcclusionCommon.IsStereoSinglePassEnabled( aCamera ) == true )
		{
			Matrix4x4 viewLeft = aCamera.GetStereoViewMatrix( Camera.StereoscopicEye.Left );
			Matrix4x4 viewRight = aCamera.GetStereoViewMatrix( Camera.StereoscopicEye.Right );

			cb.SetGlobalMatrix( PropertyID._AO_CameraViewLeft, viewLeft );
			cb.SetGlobalMatrix( PropertyID._AO_CameraViewRight, viewRight );

			Matrix4x4 projectionMatrixLeft = aCamera.GetStereoProjectionMatrix( Camera.StereoscopicEye.Left );
			Matrix4x4 projectionMatrixRight = aCamera.GetStereoProjectionMatrix( Camera.StereoscopicEye.Right );

			Matrix4x4 projLeft = GL.GetGPUProjectionMatrix( projectionMatrixLeft, false );
			Matrix4x4 projRight = GL.GetGPUProjectionMatrix( projectionMatrixRight, false );

			cb.SetGlobalMatrix( PropertyID._AO_ProjMatrixLeft, projLeft );
			cb.SetGlobalMatrix( PropertyID._AO_ProjMatrixRight, projRight );

			if( isUsingTemporalFilter )
			{
				Matrix4x4 ViewProjMatrixLeft = projLeft * viewLeft;
				Matrix4x4 ViewProjMatrixRight = projRight * viewRight;

				Matrix4x4 InvViewProjMatrixLeft = Matrix4x4.Inverse( ViewProjMatrixLeft );
				Matrix4x4 InvViewProjMatrixRight = Matrix4x4.Inverse( ViewProjMatrixRight );

				cb.SetGlobalMatrix( PropertyID._AO_InvViewProjMatrixLeft, InvViewProjMatrixLeft );
				cb.SetGlobalMatrix( PropertyID._AO_PrevViewProjMatrixLeft, m_prevViewProjMatrixLeft );
				cb.SetGlobalMatrix( PropertyID._AO_PrevInvViewProjMatrixLeft, m_prevInvViewProjMatrixLeft );

				cb.SetGlobalMatrix( PropertyID._AO_InvViewProjMatrixRight, InvViewProjMatrixRight );
				cb.SetGlobalMatrix( PropertyID._AO_PrevViewProjMatrixRight, m_prevViewProjMatrixRight );
				cb.SetGlobalMatrix( PropertyID._AO_PrevInvViewProjMatrixRight, m_prevInvViewProjMatrixRight );

				m_prevViewProjMatrixLeft = ViewProjMatrixLeft;
				m_prevInvViewProjMatrixLeft = InvViewProjMatrixLeft;

				m_prevViewProjMatrixRight = ViewProjMatrixRight;
				m_prevInvViewProjMatrixRight = InvViewProjMatrixRight;
			}
		}
		else
		{
			Matrix4x4 view = aCamera.worldToCameraMatrix;

			cb.SetGlobalMatrix( PropertyID._AO_CameraViewLeft, view );

			if( isUsingTemporalFilter )
			{
				Matrix4x4 proj = GL.GetGPUProjectionMatrix( aCamera.projectionMatrix, false );

				Matrix4x4 ViewProjMatrix = proj * view;
				Matrix4x4 InvViewProjMatrix = Matrix4x4.Inverse( ViewProjMatrix );

				cb.SetGlobalMatrix( PropertyID._AO_InvViewProjMatrixLeft, InvViewProjMatrix );
				cb.SetGlobalMatrix( PropertyID._AO_PrevViewProjMatrixLeft, m_prevViewProjMatrixLeft );
				cb.SetGlobalMatrix( PropertyID._AO_PrevInvViewProjMatrixLeft, m_prevInvViewProjMatrixLeft );

				m_prevViewProjMatrixLeft = ViewProjMatrix;
				m_prevInvViewProjMatrixLeft = InvViewProjMatrix;
			}
		}
	}

}

public enum SampleCountLevel
{
	Low = 0,
	Medium,
	High,
	VeryHigh
}

public struct TargetDesc
{
	public int fullWidth;
	public int fullHeight;
	public int width;
	public int height;
	public float oneOverWidth;
	public float oneOverHeight;
}

public static class ShaderPass
{
	public const int OcclusionLow_None_UseDynamicDepthMips = 16;
	public const int CombineDownsampledOcclusionDepth = 32;
	public const int NeighborMotionIntensity = 33;
	public const int ClearTemporal = 34;
	public const int ScaleDownCloserDepthEven = 35;
	public const int ScaleDownCloserDepthEven_CameraDepthTexture = 36;
	public const int Temporal = 37;

	// Blur
	public const int BlurHorizontal1 = 0;
	public const int BlurVertical1 = 1;
	public const int BlurHorizontal2 = 2;
	public const int BlurVertical2 = 3;
	public const int BlurHorizontal3 = 4;
	public const int BlurVertical3 = 5;
	public const int BlurHorizontal4 = 6;
	public const int BlurVertical4 = 7;
	public const int BlurHorizontalIntensity = 8;
	public const int BlurVerticalIntensity = 9;

	// Apply Occlusion
	public const int ApplyDebug = 0;
	public const int ApplyDebugTemporal = 1;
	public const int ApplyDeferred = 3;
	public const int ApplyDeferredTemporal = 4;
	public const int ApplyDeferredLog = 6;
	public const int ApplyDeferredLogTemporal = 7;
	public const int ApplyPostEffect = 9;
	public const int ApplyPostEffectTemporal = 10;
	public const int ApplyPostEffectTemporalMultiply = 12;
	public const int ApplyDeferredTemporalMultiply = 13;
	public const int ApplyDebugCombineFromTemporal = 14;
	public const int ApplyCombineFromTemporal = 15;
	public const int ApplyDeferredCombineFromTemporal = 16;
	public const int ApplyDeferredLogCombineFromTemporal = 17;

	// Occlusion Normal Targets
	public const int OcclusionLow_None = 0;
	public const int OcclusionLow_Camera = 1;
	public const int OcclusionLow_GBuffer = 2;
	public const int OcclusionLow_GBufferOctaEncoded = 3;
}

public static class PropertyID
{
	public static readonly int _MainTex = Shader.PropertyToID( "_MainTex" );
	public static readonly int _AO_Radius = Shader.PropertyToID( "_AO_Radius" );
	public static readonly int _AO_PowExponent = Shader.PropertyToID( "_AO_PowExponent" );
	public static readonly int _AO_Bias = Shader.PropertyToID( "_AO_Bias" );
	public static readonly int _AO_Levels = Shader.PropertyToID( "_AO_Levels" );
	public static readonly int _AO_ThicknessDecay = Shader.PropertyToID( "_AO_ThicknessDecay" );
	public static readonly int _AO_BlurSharpness = Shader.PropertyToID( "_AO_BlurSharpness" );
	public static readonly int _AO_BufDepthToLinearEye = Shader.PropertyToID( "_AO_BufDepthToLinearEye" );
	public static readonly int _AO_CameraViewLeft = Shader.PropertyToID( "_AO_CameraViewLeft" );
	public static readonly int _AO_CameraViewRight = Shader.PropertyToID( "_AO_CameraViewRight" );
	public static readonly int _AO_ProjMatrixLeft = Shader.PropertyToID( "_AO_ProjMatrixLeft" );
	public static readonly int _AO_ProjMatrixRight = Shader.PropertyToID( "_AO_ProjMatrixRight" );
	public static readonly int _AO_InvViewProjMatrixLeft = Shader.PropertyToID( "_AO_InvViewProjMatrixLeft" );
	public static readonly int _AO_PrevViewProjMatrixLeft = Shader.PropertyToID( "_AO_PrevViewProjMatrixLeft" );
	public static readonly int _AO_PrevInvViewProjMatrixLeft = Shader.PropertyToID( "_AO_PrevInvViewProjMatrixLeft" );
	public static readonly int _AO_InvViewProjMatrixRight = Shader.PropertyToID( "_AO_InvViewProjMatrixRight" );
	public static readonly int _AO_PrevViewProjMatrixRight = Shader.PropertyToID( "_AO_PrevViewProjMatrixRight" );
	public static readonly int _AO_PrevInvViewProjMatrixRight = Shader.PropertyToID( "_AO_PrevInvViewProjMatrixRight" );
	public static readonly int _AO_GBufferNormals = Shader.PropertyToID( "_AO_GBufferNormals" );
	public static readonly int _AO_Target_TexelSize = Shader.PropertyToID( "_AO_Target_TexelSize" );
	public static readonly int _AO_TemporalCurveAdj = Shader.PropertyToID( "_AO_TemporalCurveAdj" );
	public static readonly int _AO_TemporalMotionSensibility = Shader.PropertyToID( "_AO_TemporalMotionSensibility" );
	public static readonly int _AO_CurrOcclusionDepth = Shader.PropertyToID( "_AO_CurrOcclusionDepth" );
	public static readonly int _AO_CurrOcclusionDepth_TexelSize = Shader.PropertyToID( "_AO_CurrOcclusionDepth_TexelSize" );
	public static readonly int _AO_TemporalAccumm = Shader.PropertyToID( "_AO_TemporalAccumm" );
	public static readonly int _AO_TemporalDirections = Shader.PropertyToID( "_AO_TemporalDirections" );
	public static readonly int _AO_TemporalOffsets = Shader.PropertyToID( "_AO_TemporalOffsets" );
	public static readonly int _AO_GBufferAlbedo = Shader.PropertyToID( "_AO_GBufferAlbedo" );
	public static readonly int _AO_GBufferEmission = Shader.PropertyToID( "_AO_GBufferEmission" );
	public static readonly int _AO_UVToView = Shader.PropertyToID( "_AO_UVToView" );
	public static readonly int _AO_HalfProjScale = Shader.PropertyToID( "_AO_HalfProjScale" );
	public static readonly int _AO_FadeParams = Shader.PropertyToID( "_AO_FadeParams" );
	public static readonly int _AO_FadeValues = Shader.PropertyToID( "_AO_FadeValues" );
	public static readonly int _AO_FadeToTint = Shader.PropertyToID( "_AO_FadeToTint" );
	public static readonly int _AO_CurrMotionIntensity = Shader.PropertyToID( "_AO_CurrMotionIntensity" );
	public static readonly int _AO_CurrDepthSource_TexelSize = Shader.PropertyToID( "_AO_CurrDepthSource_TexelSize" );
	public static readonly int _AO_CurrDepthSource = Shader.PropertyToID( "_AO_CurrDepthSource" );
	public static readonly int _AO_CurrMotionIntensity_TexelSize = Shader.PropertyToID( "_AO_CurrMotionIntensity_TexelSize" );
	public static readonly int _AO_SourceDepthMipmap = Shader.PropertyToID( "_AO_SourceDepthMipmap" );
	public static readonly int _AO_Source_TexelSize = Shader.PropertyToID( "_AO_Source_TexelSize" );
}

}