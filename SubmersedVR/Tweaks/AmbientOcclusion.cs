using System.IO;
using UnityEngine;

namespace SubmersedVR
{

    static class AmbientOcclusionVR
    {
        private static readonly string AMPLIFY_SHADERS = "amplify_resources";
        private static bool initialized = false;
        public static bool enabled = false;
	    public static AmplifyOcclusionEffect.ApplicationMethod ApplyMethod = AmplifyOcclusionEffect.ApplicationMethod.PostEffect;
	    public static bool FilterEnabled = true;
	    public static bool FilterDownsample = true;

        public static void Init()
        {
            //Mod.logger.LogInfo($"Shader Path: {Path.Combine(Application.streamingAssetsPath, AMPLIFY_SHADERS)}");
            Valve.VR.ShaderLoader.Initialize(Path.Combine(Application.streamingAssetsPath, AMPLIFY_SHADERS));
            initialized = true;
        }
 
        public static void OnAmbientOcclusionSettingsChanged(Camera camera)
        {
            AmplifyOcclusionEffect effect = camera?.gameObject.GetComponent<AmplifyOcclusionEffect>();
            if (effect != null)
            {
                effect.enabled = Settings.AOEnabled; //enabled;
                effect.SampleCount = Settings.AOMethod == "Low" ? AmplifyOcclusion.SampleCountLevel.Low : (Settings.AOMethod == "High" ? AmplifyOcclusion.SampleCountLevel.High : (Settings.AOMethod == "Very High" ? AmplifyOcclusion.SampleCountLevel.VeryHigh : AmplifyOcclusion.SampleCountLevel.Medium));
                effect.ApplyMethod = Settings.AOMethod == "Deferred" ? AmplifyOcclusionEffect.ApplicationMethod.Deferred : (Settings.AOMethod == "Debug" ? AmplifyOcclusionEffect.ApplicationMethod.Debug : AmplifyOcclusionEffect.ApplicationMethod.PostEffect) ;
                effect.PerPixelNormals = Settings.AOPerPixelNormals == "None" ? AmplifyOcclusionEffect.PerPixelNormalSource.None : (Settings.AOPerPixelNormals == "GBuffer" ? AmplifyOcclusionEffect.PerPixelNormalSource.GBuffer : (Settings.AOPerPixelNormals == "Octa" ? AmplifyOcclusionEffect.PerPixelNormalSource.GBufferOctaEncoded : AmplifyOcclusionEffect.PerPixelNormalSource.Camera)) ;
                effect.Intensity = Settings.AOIntensity;
                effect.Radius = Settings.AORadius;
                effect.PowerExponent = Settings.AOPowerExponent;
                effect.Bias = Settings.AOBias;
                effect.Thickness = Settings.AOThickness;
                effect.Downsample = Settings.AODownSample;
                effect.CacheAware = Settings.AOCacheAware;
                effect.FilterEnabled = Settings.AOTemporalFilterEnabled; //FilterEnabled;
                effect.FilterDownsample = Settings.AOTemporalFilterDownsampleEnabled; //FilterEnabled;
                effect.FilterBlending = Settings.AOTemporalFilterBlending; //FilterEnabled;
                effect.FilterResponse = Settings.AOTemporalFilterResponse; //FilterEnabled;

                Mod.logger.LogInfo($"OnUseAmbientOcclusionChanged:\nenabled:{effect.enabled}\nfilter enabled:{effect.FilterEnabled}\nsample count: {effect.SampleCount}\nmethod:{effect.ApplyMethod}\nperpixelnormals:{effect.PerPixelNormals}\nintensity:{effect.Intensity}");
            }

        }

        public static void AddOcclusionEffect(Camera camera)
        {
            if(!initialized)
            {
                Init();
            }
            AmplifyOcclusionEffect effect = camera?.gameObject.GetComponent<AmplifyOcclusionEffect>();
            if (effect != null)
            {
                return;
            }
            camera.gameObject.AddComponent<AmplifyOcclusionEffect>();
            OnAmbientOcclusionSettingsChanged(camera);
        }

    }



}