using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // VR fix for the precursor teleport green tunnel (TeleportScreenFX).
    //
    // The original tunnel is a 2D head-locked swirl (a full-screen
    // screen-space animation that follows the head). In VR this causes
    // eye strain (the two eyes see incoherent images). This fix replaces
    // the 2D swirl with a plain blit of the raw world + a world-locked
    // 3D green sphere enveloping the player (no full-screen head-
    // following animation - comfortable).
    //
    // MECHANISM: in the OnRenderImage PREFIX, the 2D swirl blit is
    // replaced by a plain blit of the raw world (source) and the game's
    // swirl blit is skipped (return false). The green sphere (see
    // Tweaks/TeleportSphere3D) is spawned at cycle start, parented to the
    // player (follows the snap), and its opacity mirrors the game's
    // fx.amount (so it fades in/out exactly like the game's tunnel).
    //
    // Settings.FixTeleportEffect:
    //   0 "Off": 100% original (the 2D swirl runs).
    //   1 "Fix": the plain blit + the green sphere (the fix).
    static class TeleportScreenFXVRFix
    {
        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

        // teleport-cycle tracking: fx.amount 0 -> >0 = cycle start
        static bool effectActive;
        static double activeSince;
        static int cycleCount;
        static Camera guardCam;

        // ---- OnPreRender: cycle tracking + the sphere lifecycle ----

        public static void OnPreRender(TeleportScreenFX fx)
        {
            var cam = Camera.current;
            if (cam == null)
            {
                return;
            }
            // attach the per-frame driver (the sphere update) on the FX cam
            if (guardCam != cam)
            {
                guardCam = cam;
                cam.gameObject.GetOrAddComponent<TeleportMonoGuard>();
            }
            // grab the game's swirl "nerves" texture (recalls the vortex)
            // for the sphere (idempotent: set once, then ignored)
            if (fx.mat != null)
            {
                TeleportSphere3D.SetSwirlTexture(fx.mat.GetTexture("_NervesTex"));
            }
            if (fx.amount > 0f)
            {
                if (!effectActive)
                {
                    effectActive = true;
                    activeSince = Time.time;
                    cycleCount++;
                    Mod.logger.LogInfo("[TeleportFX] cycle " + cycleCount + " started");
                    if (Settings.FixTeleportEffect)
                    {
                        TeleportSphere3D.Spawn(fx.amount);
                    }
                }
            }
            else
            {
                if (effectActive)
                {
                    Mod.logger.LogInfo("[TeleportFX] cycle " + cycleCount + " ended after " + (Time.time - activeSince).ToString("F1", Inv) + "s");
                    effectActive = false;
                    TeleportSphere3D.OnCycleEnd();
                }
            }
        }

        // Called from the sphere's safety timeout (stuck cycle): resets the
        // cycle state so the next teleport can spawn a new sphere.
        public static void ForceCycleEnd()
        {
            if (effectActive)
            {
                effectActive = false;
                Mod.logger.LogInfo("[TeleportFX] cycle " + cycleCount + " force-ended (safety)");
                TeleportSphere3D.OnCycleEnd();
            }
        }

        // ---- OnRenderImage: replace the 2D swirl with a plain blit ----

        public static bool OnRenderImage(TeleportScreenFX fx, RenderTexture source, RenderTexture destination)
        {
            if (fx.amount <= 0f)
            {
                return true;
            }
            if (Settings.FixTeleportEffect)
            {
                // Fix: blit the raw world (source) to the
                // destination, then skip the game's swirl blit.
                if (source != null && destination != null)
                {
                    Graphics.Blit(source, destination);
                }
                return false;
            }
            // mode 0 (Off): let the original swirl run
            return true;
        }

        // Persistent per-frame driver on the FX camera's object (same
        // pattern as SonarScreenShaderFixV2's SonarScreenDriver): updates
        // the sphere (opacity = fx.amount) every frame.
        class TeleportMonoGuard : MonoBehaviour
        {
            void Update()
            {
                TeleportSphere3D.UpdateSphere(GetComponent<TeleportScreenFX>());
            }

            void LateUpdate()
            {
                // re-apply the fade-out anchor after the game's managed
                // late update (the end-of-teleport snap happens there)
                TeleportSphere3D.ApplyAnchor();
            }
        }
    }

    [HarmonyPatch(typeof(TeleportScreenFX), nameof(TeleportScreenFX.OnPreRender))]
    static class TeleportScreenFXPreRenderPatch
    {
        [HarmonyPrefix]
        static void Prefix(TeleportScreenFX __instance)
        {
            TeleportScreenFXVRFix.OnPreRender(__instance);
        }
    }

    [HarmonyPatch(typeof(TeleportScreenFX), nameof(TeleportScreenFX.OnRenderImage))]
    static class TeleportScreenFXOnRenderImagePatch
    {
        [HarmonyPrefix]
        static bool Prefix(TeleportScreenFX __instance, RenderTexture source, RenderTexture destination)
        {
            return TeleportScreenFXVRFix.OnRenderImage(__instance, source, destination);
        }
    }
}
