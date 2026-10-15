using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace SubmersedVR
{
    public class Settings
    {
        public delegate void BooleanChanged(bool newValue);
        public delegate void FloatChanged(float newValue);
        public delegate void VoidChanged();

        public static bool IsSnapTurningEnabled;
        public static event BooleanChanged IsSnapTurningEnabledChanged;
        public static float SnapTurningAngle = 45.0f;
        public static event FloatChanged SnapTurningAngleChanged;

        public static bool IsDebugEnabled;
        public static event BooleanChanged IsDebugChanged;

        public static bool InvertYAxis;
        public static event BooleanChanged InvertYAxisChanged;

        public static bool AlwaysShowControllers;
        public static event BooleanChanged AlwaysShowControllersChanged;

        public static bool FullBody = false;

        public static string ShowLaserPointer = "Default";
        //public static bool AlwaysShowLaserPointer;
        //public static event BooleanChanged AlwaysShowLaserPointerChanged;

        public static bool PutHandReticleOnLaserPointer;
        public static event BooleanChanged PutHandReticleOnLaserPointerChanged;

        public static bool PutBarsOnWrist;
        public static event BooleanChanged PutBarsOnWristChanged;

        public static bool AreGameHapticsEnabled = false;
        public static bool AreUIHapticsEnabled = false;
        public static bool ArticulatedHands = false;
        public static bool HandBasedTurning = false;
        public static bool LeftHandBasedTurning = false;

        // VR sonar (kept in the code, always on - the old app toggle was
        // removed; the "legacy" selection = 100% original game behavior)
        public static bool SonarModEnabled = true;
        // Which screen effect the sonar draws: legacy / legacy (3D fixed) /
        // blue wave / hologram map (3D)
        public static string SonarScreenEffect = SonarEffectOptions.FixPerEyeMatrices;
        // --- Sonar Ping (SHARED: drives BOTH the screen blue wave and the
        // holo floor, so the front stays synced by construction). The sweep
        // (2.5 s), the front curves (Linear) and all the look parameters
        // (wave trail/relief/attenuation, floor relief/scan/wireframe) are
        // FROZEN in the code (SonarScreenWave / SonarHoloMap) ---
        public static float SonarPingRange = 450f;   // m, max range
        // The shared sonar glow color (preset, see SonarPresetColor) -
        // drives BOTH the floor hologram and the screen wave
        public static string SonarColor = "Lagoon";

        // Preset sonar glow colors (no color picker in the options panel - a
        // fixed set of tuned sonar hues). One shared preset drives both the
        // floor hologram and the screen wave
        public static Color32 SonarPresetColor(string name)
        {
            switch (name)
            {
                case "Lagoon": return new Color32(0, 229, 255, 255);
                case "Cyan":   return new Color32(0, 255, 255, 255);
                case "Teal":   return new Color32(0, 204, 153, 255);
                case "Green":  return new Color32(51, 255, 102, 255);
                case "White":  return new Color32(255, 255, 255, 255);
                case "Blue":
                default:       return new Color32(58, 145, 218, 255);  // the game holo material's blue
            }
        }

        //Ambient Occlusion Settings
        public static bool AOEnabled = true;
        public static string AOMethod = "Post Effect";
        public static string AOSampleCount = "Medium";
        public static string AOPerPixelNormals = "Camera";
        public static float AOIntensity = 1.0f;
        public static float AORadius = 2.0f;
        public static float AOPowerExponent = 1.8f;
        public static float AOBias = 0.05f;
        public static float AOThickness = 1.0f;
        public static bool AODownSample = true;
        public static bool AOCacheAware = true;
        public static bool AOTemporalFilterEnabled = true;
        public static bool AOTemporalFilterDownsampleEnabled = true;
        public static float AOTemporalFilterBlending = 0.8f;
        public static float AOTemporalFilterResponse = 0.5f;
        public static event VoidChanged AmbientOcclusionSettingsChanged;

        // public static float HudDistance = 1.0f;
        // public static event FloatChanged HudDistanceChanged;

        // Saves or loads all public static properties as settings using the given serializer
        internal static void Serialize(GameSettings.ISerializer serializer)
        {
            string ns = nameof(SubmersedVR);
            foreach (var p in typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                string name = p.Name;
                var value = p.GetValue(null);
                switch (value)
                {
                    case bool val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case int val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case float val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case string val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case Color32 val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    default:
                        Mod.logger.LogError($"Can't save/load setting {name} with type {value.GetType()}");
                        break;
                }
            }
        }

        internal static void AddMenu(uGUI_OptionsPanel panel)
        {
            // Migrate a persisted sonar selection that is no longer offered,
            // BEFORE the choice rows are built (the panel is created at menu
            // load, before the VR startup hook)
            SonarEffectOptions.Sanitize();
            int tab = panel.AddTab("Submersed VR");

            panel.AddHeading(tab, "Controls");
            panel.AddChoiceOption<string>(tab, "Movement Mode", new string[] { "Head Based", "Right Hand Based", "Left Hand Based" }, HandBasedTurning ? (LeftHandBasedTurning ? "Left Hand Based" : "Right Hand Based") : "Head Based", (value) =>
            {
                HandBasedTurning = value == "Right Hand Based" || value == "Left Hand Based";
                LeftHandBasedTurning = value == "Left Hand Based";
            });
            panel.AddToggleOption(tab, "Enable Snap Turning", IsSnapTurningEnabled, (value) =>
            {
                IsSnapTurningEnabled = value;
                if (IsSnapTurningEnabledChanged != null)
                {
                    IsSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "Snap Turning Angle(°)", new float[] { 22.5f, 30f, 45f, 90f }, SnapTurningAngle, (value) =>
            {
                SnapTurningAngle = value;
                if (SnapTurningAngleChanged != null)
                {
                    SnapTurningAngleChanged(value);
                }
            });

            panel.AddHeading(tab, "Immersion");
            panel.AddToggleOption(tab, "Put survival meter on left wrist", PutBarsOnWrist, (value) => { PutBarsOnWrist = value; PutBarsOnWristChanged(value); });
            panel.AddToggleOption(tab, "Articulated Hands", ArticulatedHands, (value) => { ArticulatedHands = value; }, "Hands animate based on the movement of your physical hands.");
            panel.AddToggleOption(tab, "Enable Game Haptics(WIP)", AreGameHapticsEnabled, (value) => { AreGameHapticsEnabled = value; }, "Enable controller vibration while interacting with world objects.");
            panel.AddToggleOption(tab, "Enable UI Haptics(WIP)", AreUIHapticsEnabled, (value) => { AreUIHapticsEnabled = value; }, "Enable controller vibration while interacting with the User Interface.");
            panel.AddChoiceOption<string>(tab, "Show Laser Pointer", new string[] { "Always", "Default", "Never" }, ShowLaserPointer, (value) =>
            {
                ShowLaserPointer = value;
            });

            panel.AddHeading(tab, "Sonar");
            panel.AddChoiceOption<string>(tab, "Sonar screen effect", SonarEffectOptions.Visible, SonarScreenEffect, (value) => { SonarScreenEffect = value; SonarEffectOptions.ApplySelection(); }, "What the sonar draws. legacy = the game's own effect, shown only during a ping (both eyes) - the 100% original look. legacy (3D fixed) = the game's grid, stereo-corrected per eye (no more double grid in VR). blue wave = a bright lagoon-blue sonar wave (additive glow) that sweeps the terrain and leaves a fading trail, revealing relief. hologram map (3D) = the blue wave on the whole view (objects/monsters glow) PLUS the game's own sonar hologram (the Seaglide / map room map look) rendered on the REAL terrain, revealed by the ping front, then fading out, synced to the screen wave. The fixed/wave/hologram options need the sonar_resources bundle in StreamingAssets for their screen layer; without it the screen layer does nothing (the hologram floor is a game material and still shows).");
            panel.AddSliderOption(tab, "Ping Range (m)", SonarPingRange, 200f, 450f, SonarPingRange, 10f, (value) => { SonarPingRange = value; }, SliderLabelMode.Float, "0", "Max scan radius around the ping (m). Drives both the screen blue wave and the holo floor so the fronts reach the same radius. Terrain streaming limits the effective range (~350 m).");
            panel.AddChoiceOption<string>(tab, "Sonar Color", new string[] { "Lagoon", "Blue", "Cyan", "Teal", "Green", "White" }, SonarColor, (value) => { SonarColor = value; }, "Glow color of the sonar (the on-screen blue wave and, in hologram map mode, the floor).");

            panel.AddHeading(tab, "Experimental");
            panel.AddToggleOption(tab, "Put hand reticle on laserpointer end", PutHandReticleOnLaserPointer, (value) => { PutHandReticleOnLaserPointer = value; PutHandReticleOnLaserPointerChanged(value); });
            panel.AddToggleOption(tab, "Invert Y Axis in Seamoth/Cameras", InvertYAxis, (value) => { InvertYAxis = value; InvertYAxisChanged(value); }, "Enables Y axis inversion for Seamoth and Cameras.");
            //panel.AddToggleOption(tab, "Enable Particle Fix", EnableParticleFix, (value) => { EnableParticleFix = value; }, "Enables Particle Optimizations.");

            panel.AddHeading(tab, "Hidden/Advanced VR Settings(Those can cause motion sickness!)");
            panel.AddToggleOption(tab, "Enable pitching(Looking Up/Down) while diving", !VROptions.disableInputPitch, (value) => { VROptions.disableInputPitch = !value; }, "This allows you to pitch up and down using the right thumbstick when diving. Can be very disorienting! I recommend to keep this disabled!");
            panel.AddToggleOption(tab, "Enable desktop cinematics", VROptions.enableCinematics, (value) => { VROptions.enableCinematics = value; }, "Enables the games cinematics. Warning! Those move around your head and can cause motion sickness!");
            panel.AddToggleOption(tab, "Skip intro", VROptions.skipIntro, (value) => { VROptions.skipIntro = value; }, "Skip the intro when starting a new game.");

            panel.AddHeading(tab, "Debug Options");
            panel.AddToggleOption(tab, "Debug Overlays", IsDebugEnabled, (value) => { IsDebugEnabled = value; IsDebugChanged(value); }, "Enables Debug Overlays and Logs.");
            panel.AddToggleOption(tab, "Always show controllers", AlwaysShowControllers, (value) => { AlwaysShowControllers = value; AlwaysShowControllersChanged(value); }, "Shows the controllers at all times.");
            //panel.AddToggleOption(tab, "Always show laserpointer", AlwaysShowLaserPointer, (value) => { AlwaysShowLaserPointer = value; AlwaysShowLaserPointerChanged(value); }, "Show the laserpointer at all times.");

#if false
            tab = panel.AddTab("Vehicles VR");
            panel.AddHeading(tab, "Comfort");
            panel.AddSliderOption(tab, "Seamoth Pilot Position Offset", SeamothZOffset, -0.4f, 0.4f, SeamothZOffset, 0.01f, (value) => { SeamothZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Seamoth Pilot Height Offset", SeamothYOffset, -0.4f, 0.4f, SeamothYOffset, 0.01f, (value) => { SeamothYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Position Offset", ExosuitZOffset, -0.4f, 0.4f, ExosuitZOffset, 0.01f, (value) => { ExosuitZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Height Offset", ExosuitYOffset, -0.4f, 0.4f, ExosuitYOffset, 0.01f, (value) => { ExosuitYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Cyclops Pilot Position Offset", CyclopsZOffset, -0.4f, 0.4f, CyclopsZOffset, 0.01f, (value) => { CyclopsZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Cyclops Pilot Height Offset", CyclopsYOffset, -0.4f, 0.4f, CyclopsYOffset, 0.01f, (value) => { CyclopsYOffset = value; }, SliderLabelMode.Float, "0.00");

            panel.AddHeading(tab, "Options");
            panel.AddToggleOption(tab, "Physical Driving", PhysicalDriving, (value) => { PhysicalDriving = value; }, "Grip Vehicle controls to steer.");
            panel.AddToggleOption(tab, "Locked Steering Grips", PhysicalLockedGrips, (value) => { PhysicalLockedGrips = value; }, "Gripping the steering control locks your hands to the steering so you dont have to constantly grip. Grip again to unlock.");

            panel.AddHeading(tab, "SeaMoth Left Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", SeamothLeftHorizontalCenterAngle, -10f, 10f, SeamothLeftHorizontalCenterAngle, 1f, (value) => { SeamothLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", SeamothLeftVerticleCenterAngle, -10f, 10f, SeamothLeftVerticleCenterAngle, 1f, (value) => { SeamothLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", SeamothLeftDeadZone, 1f, 10f, SeamothLeftDeadZone, 1f, (value) => { SeamothLeftDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "SeaMoth Right Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", SeamothRightHorizontalCenterAngle, -10f, 10f, SeamothRightHorizontalCenterAngle, 1f, (value) => { SeamothRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", SeamothRightVerticleCenterAngle, -10f, 10f, SeamothRightVerticleCenterAngle, 1f, (value) => { SeamothRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", SeamothRightDeadZone, 1f, 10f, SeamothRightDeadZone, 1f, (value) => { SeamothRightDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "Prawn Suit Left Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", ExosuitLeftHorizontalCenterAngle, -10f, 10f, ExosuitLeftHorizontalCenterAngle, 1f, (value) => { ExosuitLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", ExosuitLeftVerticleCenterAngle, -10f, 10f, ExosuitLeftVerticleCenterAngle, 1f, (value) => { ExosuitLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", ExosuitLeftDeadZone, 1f, 10f, ExosuitLeftDeadZone, 1f, (value) => { ExosuitLeftDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "Prawn Suit Right Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", ExosuitRightHorizontalCenterAngle, -10f, 10f, ExosuitRightHorizontalCenterAngle, 1f, (value) => { ExosuitRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", ExosuitRightVerticleCenterAngle, -10f, 10f, ExosuitRightVerticleCenterAngle, 1f, (value) => { ExosuitRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", ExosuitRightDeadZone, 1f, 10f, ExosuitRightDeadZone, 1f, (value) => { ExosuitRightDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "Cyclops Left Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", CyclopsLeftHorizontalCenterAngle, -10f, 10f, CyclopsLeftHorizontalCenterAngle, 1f, (value) => { CyclopsLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", CyclopsLeftVerticleCenterAngle, -10f, 10f, CyclopsLeftVerticleCenterAngle, 1f, (value) => { CyclopsLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", CyclopsLeftDeadZone, 1f, 10f, CyclopsLeftDeadZone, 1f, (value) => { CyclopsLeftDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "Cyclops Right Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", CyclopsRightHorizontalCenterAngle, -10f, 10f, CyclopsRightHorizontalCenterAngle, 1f, (value) => { CyclopsRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", CyclopsRightVerticleCenterAngle, -10f, 10f, CyclopsRightVerticleCenterAngle, 1f, (value) => { CyclopsRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", CyclopsRightDeadZone, 1f, 10f, CyclopsRightDeadZone, 1f, (value) => { CyclopsRightDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");
#endif
        }

        internal static void AddToGraphicsOptions(uGUI_OptionsPanel panel)
        {
            int tab = panel.tabs.Count - 1;

            string space = "   ";
            panel.AddHeading(tab, "Ambient Occlusion");
            panel.AddToggleOption(tab, space + "Enable", AOEnabled, (value) => { AOEnabled = AmbientOcclusionVR.enabled = value; AmbientOcclusionSettingsChanged(); }, "Use ambient occlusion. Increases demand on GPU.");
            panel.AddChoiceOption<string>(tab, space + "Method", new string[] { "Post Effect", "Deferred", "Debug" }, AOMethod, (value) =>
            {
                AOMethod = value;
                if (AmbientOcclusionSettingsChanged != null)
                {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "Sample Count", new string[] { "Low", "Medium", "High", "Very High" }, AOSampleCount, (value) =>
            {
                AOSampleCount = value;
                if (AmbientOcclusionSettingsChanged != null)
                {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "Per Pixel Normals", new string[] { "None", "Camera", "GBuffer", "Octa" }, AOPerPixelNormals, (value) =>
            {
                AOPerPixelNormals = value;
                if (AmbientOcclusionSettingsChanged != null)
                {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddSliderOption(tab, space + "Intensity", AOIntensity, 0f, 1.0f, AOIntensity, 0.02f, (value) => { AOIntensity = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "Radius", AORadius, 0f, 10.0f, AORadius, 0.1f, (value) => { AORadius = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.0");
            panel.AddSliderOption(tab, space + "Power Exponent", AOPowerExponent, 0f, 16f, AOPowerExponent, 0.1f, (value) => { AOPowerExponent = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.0");
            panel.AddSliderOption(tab, space + "Bias", AOBias, 0f, 0.99f, AOBias, 0.02f, (value) => { AOBias = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "Thickness", AOThickness, 0f, 1.0f, AOThickness, 0.02f, (value) => { AOThickness = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddToggleOption(tab, space + "Downsample", AODownSample, (value) => { AODownSample = value; AmbientOcclusionSettingsChanged(); }, "Compute the Occlusion and Blur at half of the resolution.");
            panel.AddToggleOption(tab, space + "Cache Aware", AOCacheAware, (value) => { AOCacheAware = value; AmbientOcclusionSettingsChanged(); }, "Cache optimization for best performance / quality tradeoff.");
            panel.AddToggleOption(tab, space + "Enable Temporal Filter", AOTemporalFilterEnabled, (value) => { AOTemporalFilterEnabled = value; AmbientOcclusionSettingsChanged(); }, "Accumulates the effect over the time.");
            panel.AddToggleOption(tab, space + "Temporal Filter Downsample", AOTemporalFilterDownsampleEnabled, (value) => { AOTemporalFilterDownsampleEnabled = value; AmbientOcclusionSettingsChanged(); }, "Effect at half of the resolution.");
            panel.AddSliderOption(tab, space + "Temporal Filter Blending", AOTemporalFilterBlending, 0f, 1.0f, AOTemporalFilterBlending, 0.02f, (value) => { AOTemporalFilterBlending = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "Temporal Filter Response", AOTemporalFilterResponse, 0f, 1.0f, AOTemporalFilterResponse, 0.02f, (value) => { AOTemporalFilterResponse = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
        }

    }

    #region Patches

    // This enables the mod to save and load settings, by serializing our settings from the class above.
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeSettings))]
    static class SerializeModSettings
    {
        public static void Postfix(GameSettings.ISerializer serializer)
        {
            Settings.Serialize(serializer);
            // Migrate a persisted sonar selection that is no longer offered,
            // right after the values are loaded
            SonarEffectOptions.Sanitize();
        }
    }

    // Save the advanced VR Settings
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeVRSettings))]
    static class SerializeAdvancedVRSettings
    {
        public static void Postfix(GameSettings.ISerializer serializer)
        {
            VROptions.enableCinematics = serializer.Serialize($"VR/{nameof(VROptions.enableCinematics)}", VROptions.enableCinematics);
            VROptions.disableInputPitch = serializer.Serialize($"VR/{nameof(VROptions.disableInputPitch)}", VROptions.disableInputPitch);
            VROptions.skipIntro = serializer.Serialize($"VR/{nameof(VROptions.skipIntro)}", VROptions.skipIntro);
        }
    }

    // This hooks into the tab creation to create the options menu.
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
    static class CreateOptionsTab
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            Settings.AddMenu(__instance);
        }
    }

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGraphicsTab))]
    static class UpdateGraphicsOptions
    {
        //Get rid of the default Ambient Occlusion Option
        public static bool Prefix(uGUI_OptionsPanel __instance)
        {
            int tabIndex = __instance.AddTab("Graphics");
            __instance.AddSliderOption(tabIndex, "Gamma", GammaCorrection.gamma, 0.1f, 2.8f, 1f, 0.01f, delegate (float value)
            {
                GammaCorrection.gamma = value;
            }, SliderLabelMode.Float, "0.00", null);
            int qualityPresetIndex = __instance.GetQualityPresetIndex();
            __instance.qualityPresetOption = __instance.AddChoiceOption(tabIndex, "Preset", uGUI_OptionsPanel.presetOptions, qualityPresetIndex, new UnityAction<int>(__instance.OnQualityPresetChanged), null);
            __instance.ApplyQualityPreset(qualityPresetIndex);
            int currentIndex;
            string[] items = uGUI_OptionsPanel.GetColorGradingOptions(out currentIndex);
            __instance.AddChoiceOption(tabIndex, "ColorGrading", items, currentIndex, new UnityAction<int>(__instance.OnColorGradingChanged), null);
            __instance.AddHeading(tabIndex, "Advanced");
            if (uGUI_MainMenu.main)
            {
                int currentIndex2;
                string[] detailOptions = uGUI_OptionsPanel.GetDetailOptions(out currentIndex2);
                __instance.detailOption = __instance.AddChoiceOption(tabIndex, "Detail", detailOptions, currentIndex2, new UnityAction<int>(__instance.OnDetailChanged), null);
            }
            __instance.waterQualityOption = __instance.AddChoiceOption<WaterSurface.Quality>(tabIndex, "WaterQuality", WaterSurface.GetQualityOptions(), WaterSurface.GetQuality(), new UnityAction<WaterSurface.Quality>(__instance.OnWaterQualityChanged), null);
            int currentIndex3;
            string[] antiAliasingOptions = uGUI_OptionsPanel.GetAntiAliasingOptions(out currentIndex3);
            __instance.aaModeOption = __instance.AddChoiceOption(tabIndex, "Antialiasing", antiAliasingOptions, currentIndex3, new UnityAction<int>(__instance.OnAAmodeChanged), null);
            __instance.aaQualityOption = __instance.AddChoiceOption(tabIndex, "AntialiasingQuality", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAaQuality(), new UnityAction<int>(__instance.OnAAqualityChanged), null);
            __instance.bloomOption = __instance.AddToggleOption(tabIndex, "Bloom", UwePostProcessingManager.GetBloomEnabled(), new UnityAction<bool>(__instance.OnBloomChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.lensDirtOption = __instance.AddToggleOption(tabIndex, "LensDirt", UwePostProcessingManager.GetBloomLensDirtEnabled(), new UnityAction<bool>(__instance.OnBloomLensDirtChanged), null);
                __instance.dofOption = __instance.AddToggleOption(tabIndex, "DepthOfField", UwePostProcessingManager.GetDofEnabled(), new UnityAction<bool>(__instance.OnDofChanged), null);
                __instance.motionBlurQualityOption = __instance.AddChoiceOption(tabIndex, "MotionBlurQuality", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetMotionBlurQuality(), new UnityAction<int>(__instance.OnMotionBlurQualityChanged), null);
            }
            //__instance.aoQualityOption = __instance.AddChoiceOption(tabIndex, "AmbientOcclusion", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAoQuality(), new UnityAction<int>(__instance.OnAOqualityChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.ssrQualityOption = __instance.AddChoiceOption(tabIndex, "ScreenSpaceReflections", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetSsrQuality(), new UnityAction<int>(__instance.OnSSRqualityChanged), null);
                __instance.ditheringOption = __instance.AddToggleOption(tabIndex, "Dithering", UwePostProcessingManager.GetDitheringEnabled(), new UnityAction<bool>(__instance.OnDitheringChanged), null);
            }
            return false;
        }

        //Add in our own Ambient Occlusion Option
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            Settings.AddToGraphicsOptions(__instance);
        }
    }

    // GameOptions.GetVRAnimationMode returns true whenever we want to play the simplified VR Animations instead of the desktop ones
    [HarmonyPatch(typeof(GameOptions), nameof(GameOptions.GetVrAnimationMode))]
    class EnableCinematicsIfSet
    {
        static bool Prefix(ref bool __result)
        {
            //If The into is playing then do not disable animations
            if (uGUI.isIntro)
            {
                __result = false;
            }
            else
            {
                __result = !VROptions.enableCinematics;
            }
            return false;
        }
    }

    // This function add back in the ability to toggle fullscreen on the flatscreen display.
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGeneralTab))]
    static class ReAddFullscreenOption
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            __instance.AddToggleOption(__instance.tabs.Count - 1, "Fullscreen", Screen.fullScreen, new UnityAction<bool>(__instance.OnFullscreenChanged), null);
            string[] resolutionOptions = uGUI_OptionsPanel.GetResolutionOptions(out __instance.resolutions);
            int currentResolutionIndex = uGUI_OptionsPanel.GetCurrentResolutionIndex(__instance.resolutions);
            __instance.resolutionOption = __instance.AddChoiceOption(__instance.tabs.Count - 1, "Resolution", resolutionOptions, currentResolutionIndex, new UnityAction<int>(__instance.OnResolutionChanged), null);
        }
    }

    //Turn off built in AO and use Amplify Occlusion instead
    [HarmonyPatch(typeof(UwePostProcessingManager), nameof(UwePostProcessingManager.ApplySettingsToProfile))]
    public static class FixGraphicsForVR
    {
        public static void Postfix(UwePostProcessingManager __instance)
        {
            __instance.SetAO(0);
        }
    }

    #endregion
}
