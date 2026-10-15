using System.IO;
using UnityEngine;

namespace SubmersedVR
{
    // The single "Sonar screen effect" selection. Four modes:
    //
    //   legacy            -> the game's own effect, ping-only, both eyes
    //   legacy (3D fixed) -> the game's grid, stereo-corrected per eye
    //                        (SonarScreenShaderFixV2, mode 2)
    //   blue wave         -> a bright lagoon-blue sonar wave, additive
    //                        (SonarScreenWave)
    //   hologram map (3D) -> the blue wave on screen PLUS the game's
    //                        hologram shader on overlays of the REAL terrain
    //                        (SonarHoloMap), revealed by the ping front
    static class SonarEffectOptions
    {
        public const string Original = "legacy";
        public const string Wave = "blue wave";
        public const string HoloMap = "hologram map (3D)";
        public const string FixPerEyeMatrices = "legacy (3D fixed)";

        // The options offered in the app
        public static readonly string[] Visible =
        {
            Original,
            Wave,
            HoloMap,
            FixPerEyeMatrices
        };

        public const string V2ShaderName = "SubmersedVR/SonarScreenStereoV2";
        public const string WaveShaderName = "SubmersedVR/SonarScreenWave";

        // The plain blue wave
        public static bool IsWave(string s)
        {
            return s == Wave;
        }

        // The hologram map (3D): the game's hologram shader on overlays of
        // the real terrain, ping-driven.
        public static bool IsHoloMap(string s)
        {
            return s == HoloMap;
        }

        // The blue-wave SCREEN effect is active for the blue-wave selections
        // AND for the hologram-map mode: the holo mode shows the blue wave on
        // the whole view (objects/monsters/terrain all glow) ON TOP of the
        // hologram floor. Separate predicate from IsWave so the log labels
        // stay untouched
        public static bool IsScreenWave(string s)
        {
            return IsWave(s) || IsHoloMap(s);
        }

        // The stereo-corrected legacy grid
        public static bool IsFix(string s)
        {
            return s == FixPerEyeMatrices;
        }

        // A replacement shader takes over the whole screen effect
        public static bool IsReplacement(string s)
        {
            return IsWave(s) || IsFix(s);
        }

        // Re-apply whichever module owns the selection (swap/restore the
        // effect's material) and report the state in the log
        public static void ApplySelection()
        {
            SonarScreenShaderFixV2.ApplyState();
            SonarScreenWave.ApplyState();
            LogStatus();
        }

        // One startup status line so the log always shows what the option
        // will do (and whether the bundle/shader is actually present)
        static bool startupLogged;

        // Migrate a PERSISTED selection that is no longer offered in the app
        // (old labels, the removed world isobaths / isobath scan / recompiled
        // / rework / diag modes) to one of the four current options, so the
        // choice UI and the drivers stay coherent
        public static void SanitizeSelection()
        {
            string sel = Settings.SonarScreenEffect;
            string migrated = null;
            if (sel == "Original (game)") migrated = Original;
            else if (sel == "Original (recompiled by us)") migrated = Original;
            else if (sel == "Blue wave + trail (experimental)") migrated = Wave;
            else if (sel == "isobath scan") migrated = Wave;
            else if (sel == "world isobaths (3D)") migrated = HoloMap;
            else if (sel == "Stereo fix: per-eye matrices") migrated = FixPerEyeMatrices;
            else if (sel == "Stereo fix: per-eye matrices (rework)") migrated = FixPerEyeMatrices;
            else if (sel == "Stereo fix: per-eye C2W only") migrated = FixPerEyeMatrices;
            else if (sel == "Stereo fix: translation (prototype)") migrated = FixPerEyeMatrices;
            if (migrated != null)
            {
                Mod.logger.LogInfo($"[SonarFX] saved screen effect '{sel}' migrated to '{migrated}'");
                Settings.SonarScreenEffect = migrated;
                return;
            }
            if (System.Array.IndexOf(Visible, sel) < 0)
            {
                Mod.logger.LogInfo($"[SonarFX] saved screen effect '{sel}' is no longer offered - falling back to '{FixPerEyeMatrices}'");
                Settings.SonarScreenEffect = FixPerEyeMatrices;
            }
        }

        // The master toggle was removed from the app - the mod is always on,
        // and the "legacy" selection IS the 100% original behavior (game
        // screen effect, no world ping ring). Force it on so a persisted
        // "off" cannot leave the mod stuck disabled with no way to
        // re-enable it from the menu
        public static void SanitizeModEnabled()
        {
            if (!Settings.SonarModEnabled)
            {
                Mod.logger.LogInfo("[SonarFX] master toggle removed - forcing the sonar mod on (the 'legacy' selection = 100% original behavior)");
                Settings.SonarModEnabled = true;
            }
        }

        // Idempotent: keeps the persisted values on the current option set.
        // Must run BEFORE the options panel builds its rows (the panel is
        // created at menu load, before the VR startup hook) - with a
        // persisted old label the sonar choice row would vanish. Also called
        // from the settings load/save postfix
        public static void Sanitize()
        {
            SanitizeModEnabled();
            SanitizeSelection();
        }

        public static void OnStartup()
        {
            if (startupLogged)
            {
                return;
            }
            startupLogged = true;
            Sanitize();
            LogStatus();
        }

        public static void LogStatus()
        {
            string sel = Settings.SonarScreenEffect;
            string state;
            if (!Settings.SonarModEnabled)
            {
                state = "master off = 100% original";
            }
            else if (IsHoloMap(sel))
            {
                state = "blue wave on the whole view + hologram map on the real terrain (ping-driven; the floor is a game material, the wave layer needs the bundle)";
            }
            else if (!IsReplacement(sel))
            {
                state = "original game screen effect (ping-only)";
            }
            else
            {
                string shaderName = IsWave(sel) ? WaveShaderName : V2ShaderName;
                state = SonarBundle.GetShader(shaderName) != null ? "shader loaded" : "inert (sonar_resources bundle or shader missing)";
            }
            Mod.logger.LogInfo($"[SonarFX] screen effect: {sel} ({state})");
        }
    }

    // Loads the sonar_resources asset bundle once and serves the shaders to
    // both screen-effect modules (the stereo fix and the wave). The bundle is
    // unloaded only on application quit.
    static class SonarBundle
    {
        private const string BundleName = "sonar_resources";

        static AssetBundle bundle;
        static bool loadFailed;
        static bool missingLogged;

        public static bool IsLoaded
        {
            get { return bundle != null; }
        }

        public static Shader GetShader(string shaderName)
        {
            if (bundle == null && !loadFailed)
            {
                string path = Path.Combine(Application.streamingAssetsPath, BundleName);
                if (!File.Exists(path))
                {
                    loadFailed = true;
                    if (!missingLogged)
                    {
                        missingLogged = true;
                        Mod.logger.LogInfo("[SonarFX] sonar_resources bundle not found in StreamingAssets - screen effect options are inert (the original game effect is kept)");
                    }
                    return null;
                }
                try
                {
                    bundle = AssetBundle.LoadFromFile(path);
                }
                catch (System.Exception e)
                {
                    loadFailed = true;
                    Mod.logger.LogError($"[SonarFX] sonar bundle load failed: {e}");
                    return null;
                }
            }
            if (bundle == null)
            {
                return null;
            }
            foreach (var a in bundle.LoadAllAssets<Shader>())
            {
                if (a != null && a.name == shaderName)
                {
                    return a;
                }
            }
            return null;
        }

        public static void OnQuit()
        {
            if (bundle != null)
            {
                bundle.Unload(false);
                bundle = null;
            }
        }
    }
}
