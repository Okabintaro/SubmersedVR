using System;
using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Sonar ping state, mirrored from the game's own sonar button (shared
    // Seamoth/Cyclops entry point + Cyclops button). The game's screen
    // effect is left 100% untouched: the "legacy" selection shows it as-is,
    // and the replacement screen effects (blue wave / 3D fixed) drive their
    // timing and origin from the last ping.
    static class SonarWorldPing
    {
        // Ping state
        static bool hasPinged;
        static float pingStartTime;
        static float duration;
        internal static float waveDuration = 5f;
        static Vector3 origin;

        // True while the game's ping wave is running (the screen effects
        // animate for `duration` seconds after a ping)
        internal static bool IsPinging
        {
            get
            {
                return hasPinged && Time.time - pingStartTime < duration;
            }
        }

        // Read by the screen-effect modules (the blue wave drives its timing
        // and origin from the last ping)
        internal static bool HasPinged
        {
            get { return hasPinged; }
        }

        internal static Vector3 LastOrigin
        {
            get { return origin; }
        }

        internal static float LastPingTime
        {
            get { return pingStartTime; }
        }

        public static void Trigger()
        {
            // Isolated: a throw here (a fake-null in CollectPieces, a
            // destroyed chunk mid-dispose...) would abort the game's
            // SonarPing chain exactly like the trace NRE did (log .46)
            try
            {
                if (Mod.quitting) return;
                if (!Settings.SonarModEnabled) return;
                var root = SNCameraRoot.main;
                if (root == null || root.mainCam == null) return;
                var camTransform = root.mainCam.transform;
                if (camTransform == null) return;
                if (waveDuration <= 0f) return;

                // Both ping entry points fire on the same ping: ignore the
                // second
                if (hasPinged && Time.time - pingStartTime < 0.5f) return;

                hasPinged = true;
                pingStartTime = Time.time;
                duration = waveDuration;
                origin = camTransform.position;

                // The hologram map (3D) starts on the same ping (the game's
                // hologram shader on overlays of the real terrain) - gated
                // on the 'hologram map (3D)' selection
                SonarHoloMap.OnPing(origin);

                Mod.logger.LogInfo($"[SonarWorld] ping at origin={origin}");
            }
            catch (Exception ex)
            {
                Mod.logger.LogError($"[SonarWorld] Trigger failed: {ex}");
            }
        }
    }

    #region Patches

    // Sonar ping entry points (shared Seamoth/Cyclops + Cyclops button); both
    // fire on the same ping, Trigger has a cooldown guard
    [HarmonyPatch(typeof(SNCameraRoot), nameof(SNCameraRoot.SonarPing))]
    static class SonarWorldPingSNCameraRoot
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            SonarWorldPing.Trigger();
        }
    }

    [HarmonyPatch(typeof(CyclopsSonarButton), nameof(CyclopsSonarButton.SonarPing))]
    static class SonarWorldPingCyclopsButton
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            SonarWorldPing.Trigger();
        }
    }

    // Capture the wave duration from the screen FX ping
    [HarmonyPatch(typeof(SonarScreenFX), nameof(SonarScreenFX.Ping))]
    static class SonarWorldPingWaveDuration
    {
        [HarmonyPostfix]
        static void Postfix(SonarScreenFX __instance)
        {
            if (!Settings.SonarModEnabled)
            {
                return;
            }
            if (__instance.waveDuration > 0f)
            {
                SonarWorldPing.waveDuration = __instance.waveDuration;
            }
        }
    }

    #endregion
}
