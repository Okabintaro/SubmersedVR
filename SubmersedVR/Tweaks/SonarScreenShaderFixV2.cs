using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Stereo fix for the on-screen sonar grid ("legacy (3D fixed)").
    //
    // Diagnosis: the game's "Image Effects/Sonar" screen effect reconstructs
    // every pixel's world position from the per-eye depth buffer with the
    // CENTER camera matrices, so the grid's vanishing point ends up between
    // the two eyes. The fix sends the shader the per-eye projection terms
    // (p00, p11, p02, p12 of GetStereoProjectionMatrix) + the per-eye pose
    // (the depth linearization is kept exactly as the game's - near/far are
    // shared between the eyes). See Shaders/SonarScreenStereoV2.shader.
    //
    // Bundle lifecycle: loaded once, kept for the whole session, unloaded
    // only on application quit.
    static class SonarScreenShaderFixV2
    {
        static SonarScreenFX trackedFx;
        static Material originalMaterial;
        static Material stereoMaterial;
        static Shader sonarShader;
        static bool v2InitFailed;
        static bool swapLogged;
        static bool eyeFallbackLogged;
        static int lastFrame;
        static int callsThisFrame;

        static bool IsActive()
        {
            return Settings.SonarModEnabled
                && SonarEffectOptions.IsFix(Settings.SonarScreenEffect);
        }

        // Called on selection change: if the stereo fix is no longer the
        // selected effect, restore the game's material (the next frame would
        // do it, but this keeps the state tidy for the other module)
        public static void ApplyState()
        {
            if (IsActive())
            {
                return;
            }
            if (trackedFx != null && trackedFx._material == stereoMaterial)
            {
                trackedFx._material = originalMaterial;
            }
            if (stereoMaterial != null)
            {
                Object.Destroy(stereoMaterial);
                stereoMaterial = null;
            }
            originalMaterial = null;
            trackedFx = null;
            v2InitFailed = false;
            swapLogged = false;
        }

        // Quit: restore + material destroy (the shared bundle is unloaded by
        // the SonarScreenDriver)
        public static void OnQuit()
        {
            ApplyState();
            sonarShader = null;
        }

        // Called by the prefix patch before the game's own blit runs
        public static void BeforeOnRenderImage(SonarScreenFX fx)
        {
            if (!IsActive() || Mod.quitting)
            {
                // Safety: if no longer active but still swapped, restore
                if (trackedFx != null && trackedFx._material == stereoMaterial)
                {
                    trackedFx._material = originalMaterial;
                }
                return;
            }
            if (!EnsureShader())
            {
                return;
            }
            var orig = fx._material;
            if (orig == null)
            {
                // First frame: the effect's own CheckResources creates its
                // material during this call; the swap starts next frame
                return;
            }
            if (fx._material != stereoMaterial)
            {
                if (stereoMaterial != null && fx._material != originalMaterial)
                {
                    // The game recreated its material (scene reload /
                    // re-enable): re-capture the new original
                    Object.Destroy(stereoMaterial);
                    stereoMaterial = null;
                    Mod.logger.LogInfo("[SonarFX] v2: game material changed, re-capturing original");
                }
                originalMaterial = orig;
                // Clone the game's material (keywords, render queue,
                // properties) then swap the shader
                stereoMaterial = new Material(orig);
                stereoMaterial.name = "SubmersedVR Sonar Stereo V2";
                stereoMaterial.shader = sonarShader;
                if (!swapLogged)
                {
                    swapLogged = true;
                    Mod.logger.LogInfo($"[SonarFX] v2: screen sonar swapped (orig={orig.name})");
                }
                fx._material = stereoMaterial;
            }
            UpdateUniforms(stereoMaterial);
        }

        static bool EnsureShader()
        {
            if (sonarShader != null)
            {
                return true;
            }
            if (v2InitFailed)
            {
                return false;
            }
            sonarShader = SonarBundle.GetShader(SonarEffectOptions.V2ShaderName);
            if (sonarShader == null)
            {
                v2InitFailed = true;
                Mod.logger.LogInfo($"[SonarFX] screen sonar shader not found in sonar_resources bundle ({SonarEffectOptions.V2ShaderName}) - option inert, original effect kept");
                return false;
            }
            Mod.logger.LogInfo($"[SonarFX] screen sonar shader loaded ({sonarShader.name})");
            return true;
        }

        static void ResolveEye(Camera cam, out Camera.StereoscopicEye eye)
        {
            if (cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            {
                eye = Camera.StereoscopicEye.Left;
                return;
            }
            if (cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
            {
                eye = Camera.StereoscopicEye.Right;
                return;
            }
            // Mono/unknown at this pipeline point: fall back to the call
            // order (first call of the frame = left eye, second = right)
            if (!eyeFallbackLogged)
            {
                eyeFallbackLogged = true;
                Mod.logger.LogInfo($"[SonarFX] v2: stereoActiveEye={cam.stereoActiveEye} in OnRenderImage, using call order as eye source");
            }
            eye = callsThisFrame >= 2 ? Camera.StereoscopicEye.Right : Camera.StereoscopicEye.Left;
        }

        static void UpdateUniforms(Material m)
        {
            var cam = Camera.current;
            var root = SNCameraRoot.main;
            if (cam == null && root != null)
            {
                cam = root.mainCam;
            }
            if (cam == null)
            {
                return;
            }

            int f = Time.frameCount;
            if (f != lastFrame)
            {
                lastFrame = f;
                callsThisFrame = 0;
            }
            callsThisFrame++;

            Camera.StereoscopicEye eye;
            ResolveEye(cam, out eye);

            // Baked sign fix (logs v37/v38/v39, confirmed in-game): the game's
            // own per-eye matrices (SNCameraRoot.matrixLeftEye/RightEye) point
            // the opposite way from Unity's GetStereoViewMatrix convention, so
            // every per-eye correction uses the OPPOSITE eye. flip=True was
            // confirmed correct (left eye aligned), so it's now always applied
            // (no longer a setting).
            Camera.StereoscopicEye effEye = eye == Camera.StereoscopicEye.Left
                ? Camera.StereoscopicEye.Right
                : Camera.StereoscopicEye.Left;

            m.SetMatrix("_EyeC2W", cam.GetStereoViewMatrix(effEye).inverse);
            var p = cam.GetStereoProjectionMatrix(effEye);
            m.SetVector("_EyeProjTerms", new Vector4(p.m00, p.m11, p.m02, p.m12));
        }
    }

    // Swaps the effect's material before the game's own blit runs
    [HarmonyPatch(typeof(SonarScreenFX), nameof(SonarScreenFX.OnRenderImage))]
    static class SonarScreenShaderFixV2Patch
    {
        [HarmonyPrefix]
        static void Prefix(SonarScreenFX __instance)
        {
            SonarScreenShaderFixV2.BeforeOnRenderImage(__instance);
        }
    }

    // Restores all screen-effect modules' materials and unloads the shared
    // bundle on quit
    class SonarScreenDriver : MonoBehaviour
    {
        void OnApplicationQuit()
        {
            SonarScreenShaderFixV2.OnQuit();
            SonarScreenWave.OnQuit();
            SonarHoloMap.OnQuit();
            SonarBundle.OnQuit();
        }
    }

    [HarmonyPatch(typeof(VRCameraRig), nameof(VRCameraRig.SetupControllers))]
    static class AttachSonarScreenDriver
    {
        [HarmonyPostfix]
        static void Postfix(VRCameraRig __instance)
        {
            __instance.gameObject.GetOrAddComponent<SonarScreenDriver>();
            SonarEffectOptions.OnStartup();
        }
    }
}
