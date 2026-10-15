using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Simple world-anchored sonar wave: a pale-blue front expands from the
    // ping origin (the vehicle position at ping time); every surface point
    // starts glowing when the front reaches it and keeps glowing for a few
    // seconds (persistence trail), revealing terrain relief through the
    // G-buffer world normals. No grid.
    //
    // Stateless: the effect is a pure function of (worldPos, t) - no
    // accumulation buffer, trivially safe in multi-pass stereo. The
    // screen->world unprojection uses the PER-EYE matrices (v55): the same
    // capture as the working stereo grid (GetStereoViewMatrix /
    // GetStereoProjectionMatrix, incl. the baked opposite-eye flip), so the
    // wave sits on the terrain in both eyes.
    static class SonarScreenWave
    {
        // Soft front band width in world meters. 10 m was too wide (in-game
        // feedback v54), 4 m a slim leading edge, v64: 1 m - a tight front
        // line (the user's preferred look)
        private const float BandWidth = 1f;

        // Frozen timing / look (the user's final calibration, log .56). The
        // only live parameter is the shared Ping Range (Settings)
        const float Sweep = 2.5f;               // s, front crossing time (speed = range / sweep)
        const float StartOffset = -0.1f;        // s, the wave starts 0.1 s before the ping
        const float AttenStartDivisor = 3.5f;   // fade start = range / 3.5
        const float TrailStart = 0.30f;         // glow level right behind the front
        const float TrailPlateau = 2.0f;        // s, trail phase 1 (start -> level)
        const float TrailLevel = 0.2f;          // level at the plateau end
        const float TrailFade = 0.5f;           // s, trail phase 2 (level -> 0)
        const float TrailCurve = 0f;            // 0 = linear
        const float ReliefMin = 0.75f;          // relief shading floor
        const float Radar = 0f;                 // 0 = classic diffuse
        const float RadarCurve = 1f;
        const float AttenEnd = 0f;              // fade level reached at max range
        const float AttenCurve = 0.35f;         // distance fade curve exponent
        const float GlobalPlateau = 0f;         // s, hold at full (holo fade)
        const float GlobalRapid = 0.5f;         // s, final rapid drop (holo fade)
        const float WaveFadeTotal = 2.5f;       // s, global fade (holo mode), = the floor's Holo Fade

        // The wave color (lagoon), sent to the shader as a REAL uniform.
        // No global const in the shader: v43-v52 proved a global const does
        // not hold its source value in this bundle-compiled pipeline.
        private static readonly Vector3 WaveColor = new Vector3(0f, 0.9f, 1f);

        static SonarScreenFX trackedFx;
        static Material originalMaterial;
        static Material waveMaterial;
        static Shader waveShader;
        static bool initFailed;
        static bool loadLogged;
        static bool swapLogged;
        // Per-frame call counter for the eye fallback (same mechanism as the
        // grid driver: first call of the frame = left eye, second = right)
        static int lastFrame;
        static int callsThisFrame;
        static bool eyeFallbackLogged;

        static bool IsActive()
        {
            return Settings.SonarModEnabled
                && SonarEffectOptions.IsScreenWave(Settings.SonarScreenEffect);
        }

        // Called on selection change: if the wave is no longer the selected
        // effect, restore the game's material (the next frame would do it,
        // but this keeps the state tidy for the other module)
        public static void ApplyState()
        {
            if (IsActive())
            {
                return;
            }
            if (trackedFx != null && trackedFx._material == waveMaterial)
            {
                trackedFx._material = originalMaterial;
            }
            if (waveMaterial != null)
            {
                Object.Destroy(waveMaterial);
                waveMaterial = null;
            }
            originalMaterial = null;
            trackedFx = null;
            initFailed = false;
            swapLogged = false;
        }

        public static void OnQuit()
        {
            ApplyState();
            waveShader = null;
        }

        // Called by the prefix patch before the game's own blit runs
        public static void BeforeOnRenderImage(SonarScreenFX fx, RenderTexture source, RenderTexture destination)
        {
            if (!IsActive() || Mod.quitting)
            {
                // Safety: if no longer active but still swapped, restore
                if (trackedFx != null && trackedFx._material == waveMaterial)
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
            if (fx._material != waveMaterial)
            {
                if (waveMaterial != null && fx._material != originalMaterial)
                {
                    // The game recreated its material (scene reload /
                    // re-enable): re-capture the new original
                    Object.Destroy(waveMaterial);
                    waveMaterial = null;
                    Mod.logger.LogInfo("[SonarFX] wave: game material changed, re-capturing original");
                }
                originalMaterial = orig;
                waveMaterial = new Material(orig);
                waveMaterial.name = "SubmersedVR Sonar Wave";
                waveMaterial.shader = waveShader;
                waveMaterial.SetFloat("_WaveBand", BandWidth);
                // The look parameters (range / speed / trail / relief / radar /
                // attenuation / color) are LIVE sliders - set every frame in
                // UpdateLookParams so a slider change takes effect immediately
                // (the material is created once and reused). Per frame only
                // _WaveTime, _WaveOrigin, the eye matrices and the look params
                // change
                if (!swapLogged)
                {
                    swapLogged = true;
                    Mod.logger.LogInfo($"[SonarFX] wave: screen sonar swapped (orig={orig.name})");
                }
                fx._material = waveMaterial;
            }
            UpdateUniforms(waveMaterial, source);
        }

        static bool EnsureShader()
        {
            if (waveShader != null)
            {
                return true;
            }
            if (initFailed)
            {
                return false;
            }
            waveShader = SonarBundle.GetShader(SonarEffectOptions.WaveShaderName);
            if (waveShader == null)
            {
                initFailed = true;
                Mod.logger.LogInfo("[SonarFX] wave: sonar wave shader not found in sonar_resources bundle - option inert, original effect kept");
                return false;
            }
            if (!loadLogged)
            {
                loadLogged = true;
                Mod.logger.LogInfo($"[SonarFX] wave: sonar wave shader loaded ({waveShader.name})");
            }
            return true;
        }

        // Per-eye camera matrices for the screen->world unprojection (v55):
        // the same capture as the working stereo grid
        // (SonarScreenShaderFixV2). The built-in matrices in this pass are
        // the CENTER eye's; the depth texture is per-eye, so the unprojection
        // must use the per-eye ones or the wave front lands at a different
        // apparent distance in each eye.
        static void UpdateEyeMatrices(Material m, RenderTexture source)
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

            // Packed side-by-side frame: the per-eye matrix capture has no
            // test configuration for that topology; fall back to the center
            // matrices (the v54 behavior) rather than a half-guessed eye
            bool packed = source != null && source.width > source.height;
            if (packed)
            {
                m.SetMatrix("_EyeC2W", cam.transform.localToWorldMatrix);
                var pc = cam.projectionMatrix;
                m.SetVector("_EyeProjTerms", new Vector4(pc.m00, pc.m11, pc.m02, pc.m12));
                return;
            }

            Camera.StereoscopicEye eye;
            if (cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            {
                eye = Camera.StereoscopicEye.Left;
            }
            else if (cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
            {
                eye = Camera.StereoscopicEye.Right;
            }
            else
            {
                if (!eyeFallbackLogged)
                {
                    eyeFallbackLogged = true;
                    Mod.logger.LogInfo($"[SonarFX] wave: stereoActiveEye={cam.stereoActiveEye} in OnRenderImage, using call order as eye source");
                }
                eye = callsThisFrame >= 2 ? Camera.StereoscopicEye.Right : Camera.StereoscopicEye.Left;
            }

            // Baked sign fix (confirmed in-game for the grid): the game's
            // per-eye matrix convention points the opposite way from Unity's
            // GetStereoViewMatrix, so the OPPOSITE eye is used
            Camera.StereoscopicEye effEye = eye == Camera.StereoscopicEye.Left
                ? Camera.StereoscopicEye.Right
                : Camera.StereoscopicEye.Left;
            m.SetMatrix("_EyeC2W", cam.GetStereoViewMatrix(effEye).inverse);
            var p = cam.GetStereoProjectionMatrix(effEye);
            m.SetVector("_EyeProjTerms", new Vector4(p.m00, p.m11, p.m02, p.m12));
        }

        static void UpdateUniforms(Material m, RenderTexture source)
        {
            UpdateEyeMatrices(m, source);
            UpdateLookParams(m);
            if (!SonarWorldPing.HasPinged)
            {
                // No ping yet: keep the wave off so the shader outputs a
                // clean pass-through
                m.SetFloat("_WaveTime", -1f);
                return;
            }
            // The start offset shifts the whole screen timeline (positive =
            // the wave starts later than the ping; negative = earlier).
            // Negative _WaveTime is safe: the shader early-returns a clean
            // pass-through for _WaveTime < 0
            m.SetVector("_WaveOrigin", SonarWorldPing.LastOrigin);
            m.SetFloat("_WaveTime", Time.time - SonarWorldPing.LastPingTime - StartOffset);
        }

        // The wave look parameters. Set every frame (the range is a live
        // option; the rest are frozen constants)
        internal static void UpdateLookParams(Material m)
        {
            float range = Settings.SonarPingRange;
            m.SetFloat("_WaveRange", range);
            m.SetFloat("_WaveSpeed", range / Sweep);
            m.SetFloat("_WaveEaseFront", 0f);
            m.SetFloat("_WaveAttenStart", range / AttenStartDivisor);
            m.SetFloat("_WaveTrailStart", TrailStart);
            m.SetFloat("_WaveTrailPlateau", TrailPlateau);
            m.SetFloat("_WaveTrailLevel", TrailLevel);
            m.SetFloat("_WaveTrailFade", TrailFade);
            m.SetFloat("_WaveTrailCurve", TrailCurve);
            m.SetFloat("_WaveReliefMin", ReliefMin);
            m.SetFloat("_WaveRadar", Radar);
            m.SetFloat("_WaveRadarCurve", RadarCurve);
            m.SetFloat("_WaveAttenEnd", AttenEnd);
            m.SetFloat("_WaveAttenCurve", AttenCurve);
            // Global fade (holo map mode only) - the whole revealed disc fades
            // together (no comet), synced with the 3D holo floor; the blue
            // wave mode keeps its comet trail (_WaveGlobalFade = 0)
            m.SetFloat("_WaveGlobalFade", SonarEffectOptions.IsHoloMap(Settings.SonarScreenEffect) ? 1f : 0f);
            m.SetFloat("_WaveFadeTotal", WaveFadeTotal);
            m.SetFloat("_WavePlateauTime", GlobalPlateau);
            m.SetFloat("_WaveRapidTime", GlobalRapid);
            Color c = Settings.SonarPresetColor(Settings.SonarColor);
            m.SetVector("_WaveColor", new Vector4(c.r, c.g, c.b, 1f));
        }
    }

    // Swaps the effect's material before the game's own blit runs
    [HarmonyPatch(typeof(SonarScreenFX), nameof(SonarScreenFX.OnRenderImage))]
    static class SonarScreenWavePatch
    {
        [HarmonyPrefix]
        static void Prefix(SonarScreenFX __instance, RenderTexture source, RenderTexture destination)
        {
            SonarScreenWave.BeforeOnRenderImage(__instance, source, destination);
        }
    }
}
