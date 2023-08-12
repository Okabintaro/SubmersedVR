//Sourced from https://github.com/tommaier123/VRTweaks/blob/master/VRTweaks/VolumeCloudPatches.cs
//Fixes issue with clouds in VR
using UnityEngine;
using HarmonyLib;

namespace SubmersedVR
{
    public static class CloudTweaks
    {
        
    }

    [HarmonyPatch(typeof(Yangrc.VolumeCloud.VolumeCloudRenderer), nameof(Yangrc.VolumeCloud.VolumeCloudRenderer.RenderFrame))]
        class RenderFrameReplacement
    {
        // Need to maintain separate versions of these variables for each eye
        public static RenderTexture[] fullBufferLeft;
        public static int fullBufferIndexLeft;
        public static RenderTexture[] fullBufferRight;
        public static int fullBufferIndexRight;
        public static int frameIndexLeft;
        public static int frameIndexRight;
        private static Matrix4x4 prevV_Left;
        private static Matrix4x4 prevV_Right;

        // On/Off switch for this patch
        private static bool enabled = true;

        private static void EnsureHaltonSequence(ref Yangrc.VolumeCloud.VolumeCloudRenderer.HaltonSequence haltonSequence)
        {
            if (haltonSequence == null)
            {
                haltonSequence = new Yangrc.VolumeCloud.VolumeCloudRenderer.HaltonSequence()
                {
                    radix = 3
                };
            }
        }

        private static void SetupHeroCloud(Yangrc.VolumeCloud.VolumeCloudRenderer instance, Material mat)
        {
            if (instance.heroCloudTransform == null)
            {
                mat.DisableKeyword("HERO_CLOUD");
            }
            else
            {
                Vector4 heroCloudPosition = new Vector4(instance.heroCloudTransform.position.x, instance.heroCloudTransform.position.z,
                        instance.heroCloudTransform.localScale.x, instance.heroCloudTransform.localScale.z);
                mat.SetVector(ShaderPropertyID._HeroCloudPos, heroCloudPosition);
                mat.SetTexture(ShaderPropertyID._HeroCloudMask, instance.heroCloudMask);
                mat.SetFloat(ShaderPropertyID._HeroCloudIntensity, instance.herocloudIntensity);
                mat.EnableKeyword("HERO_CLOUD");
            }
        }

        public static bool Prefix(Yangrc.VolumeCloud.VolumeCloudRenderer __instance, RenderTexture source, RenderTexture destination, ref float ___updateTimer, ref int ___forceOutOfBound, ref bool ___qualityChanged,
            ref Material ___blitMat, ref RenderTexture ___lowresBuffer, ref int[,] ___offset, ref Camera ___mcam, ref Yangrc.VolumeCloud.VolumeCloudRenderer.HaltonSequence ___haltonSequence)
        {
            if (!enabled || ___mcam == null)
            {
                return true;
            }
            ___updateTimer = 0f;
            Camera.StereoscopicEye activeEye = ___mcam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ?
                Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
            Camera.StereoscopicEye inactiveEye = ___mcam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ?
               Camera.StereoscopicEye.Right : Camera.StereoscopicEye.Left;
            bool isLeftEye = (activeEye == Camera.StereoscopicEye.Left);
            if (___qualityChanged)
            {
                ___forceOutOfBound = 1;
            }
            if (!(__instance.configuration != null) || !(___blitMat != null))
            {
                Graphics.Blit(source, destination);
            }
            else
            {
                __instance.EnsureMaterial(___qualityChanged);
                //VolumeCloudRenderer_EnsureMaterial_ReversePatch.Run(__instance, ___qualityChanged);
                ___blitMat.DisableKeyword("CLOUDS_TO_SPHEREMAP");
                var width = __instance.GetWidth(); // VolumeCloudRenderer_GetWidth_ReversePatch.Run(__instance);
                var height = __instance.GetHeight(); //VolumeCloudRenderer_GetHeight_ReversePatch.Run(__instance);
                float single;
                float single1;
                if (isLeftEye)
                {
                    __instance.EnsureArray(ref fullBufferLeft, 2, null);
                    __instance.EnsureRenderTarget(ref fullBufferLeft[0], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, 0, 1);
                    __instance.EnsureRenderTarget(ref fullBufferLeft[1], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, 0, 1);
                    frameIndexLeft = (frameIndexLeft + 1) % 16;
                    fullBufferIndexLeft = (fullBufferIndexLeft + 1) % 2;
                    single = ___offset[frameIndexLeft, 0];
                    single1 = ___offset[frameIndexLeft, 1];
                } else
                {
                    __instance.EnsureArray(ref fullBufferRight, 2, null);
                    __instance.EnsureRenderTarget(ref fullBufferRight[0], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, 0, 1);
                    __instance.EnsureRenderTarget(ref fullBufferRight[1], width, height, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, 0 ,1);
                    frameIndexRight = (frameIndexRight + 1) % 16;
                    fullBufferIndexRight = (fullBufferIndexRight + 1) % 2;
                    single = ___offset[frameIndexRight, 0];
                    single1 = ___offset[frameIndexRight, 1];
                }
                __instance.EnsureRenderTarget(ref ___lowresBuffer, width / 4, height / 4, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear,0 ,1);
                __instance.configuration.ApplyToMaterial(___blitMat);
                SetupHeroCloud(__instance, ___blitMat);
                ___blitMat.SetInt(ShaderPropertyID._ForceOutOfBound, ___forceOutOfBound);
                ___forceOutOfBound = 0;
                ___blitMat.SetVector(ShaderPropertyID._CloudsProjectionExtents, ___mcam.GetProjectionExtents(activeEye,
                        single * (1 << (Yangrc.VolumeCloud.VolumeCloudRenderer.downSample & 31)), single1 * (1 << (Yangrc.VolumeCloud.VolumeCloudRenderer.downSample & 31))));
                EnsureHaltonSequence(ref ___haltonSequence);
                ___blitMat.SetFloat(ShaderPropertyID._CloudsRaymarchOffset, ___haltonSequence.Get());
                ___blitMat.SetVector("_TexelSize", ___lowresBuffer.texelSize);
                Graphics.Blit(null, ___lowresBuffer, ___blitMat, 0);
                ___blitMat.SetVector(ShaderPropertyID._CloudsJitter, new Vector2(single, single1));
                ___blitMat.SetTexture(ShaderPropertyID._LowresCloudTex, ___lowresBuffer);
                ___blitMat.SetMatrix(ShaderPropertyID._CloudsPrevVP,
                    // Need to use the inactiveEye here and swap the prevV_Right/Left as well.
                    GL.GetGPUProjectionMatrix(___mcam.GetStereoProjectionMatrix(inactiveEye), false) * (isLeftEye ? prevV_Right : prevV_Left));
                ___blitMat.SetVector(ShaderPropertyID._CloudsProjectionExtents, ___mcam.GetProjectionExtents(activeEye));
                if (isLeftEye)
                {
                    Graphics.Blit(fullBufferLeft[fullBufferIndexLeft], fullBufferLeft[fullBufferIndexLeft ^ 1], ___blitMat, 1);
                    Shader.SetGlobalTexture(ShaderPropertyID._CloudTex, fullBufferLeft[fullBufferIndexLeft ^ 1]);
                } else
                {
                    Graphics.Blit(fullBufferRight[fullBufferIndexRight], fullBufferRight[fullBufferIndexRight ^ 1], ___blitMat, 1);
                    Shader.SetGlobalTexture(ShaderPropertyID._CloudTex, fullBufferRight[fullBufferIndexRight ^ 1]);
                }
                Graphics.Blit(source, destination, ___blitMat, 2);
                if (isLeftEye)
                {
                    prevV_Left = ___mcam.worldToCameraMatrix;
                } else
                {
                    prevV_Right = ___mcam.worldToCameraMatrix;
                }
            }
            ___qualityChanged = false;
            return false;
        }

    }
}