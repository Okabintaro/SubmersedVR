using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace SubmersedVR
{
    public class Settings
    {
        public delegate void BooleanChanged(bool newValue);
        public delegate void FloatChanged(float newValue);

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

        public static bool AlwaysShowLaserPointer;
        public static event BooleanChanged AlwaysShowLaserPointerChanged;

        public static bool PutHandReticleOnLaserPointer;
        public static event BooleanChanged PutHandReticleOnLaserPointerChanged;

        public static bool PutBarsOnWrist;
        public static event BooleanChanged PutBarsOnWristChanged;

        public static bool EnableGameHaptics = false;
        public static bool EnableUIHaptics = false;
        public static bool ArticulatedHands = false;

        public static bool PhysicalDriving = false;
        public static bool PhysicalLockedGrips = false;

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
            int tab = panel.AddTab("Submersed VR");

            panel.AddHeading(tab, "Controls");
            panel.AddToggleOption(tab, "Enable Snap Turning", IsSnapTurningEnabled, (value) => { IsSnapTurningEnabled = value;
                if (IsSnapTurningEnabledChanged != null) {
                    IsSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "Snap Turning Angle(°)", new float[] {22.5f, 45, 90}, SnapTurningAngle, (value) => {
                SnapTurningAngle = value;
                if (SnapTurningAngleChanged != null) {
                    SnapTurningAngleChanged(value);
                }
            });

            panel.AddHeading(tab, "Experimental");
            panel.AddToggleOption(tab, "Articulated Hands", ArticulatedHands, (value) => { ArticulatedHands = value;  }, "Hands animate based on the movement of your physical hands.");
            panel.AddToggleOption(tab, "Enable Game Haptics", EnableGameHaptics, (value) => { EnableGameHaptics = value; }, "Enable controller vibration while interacting with world objects.");
            panel.AddToggleOption(tab, "Enable UI Haptics", EnableUIHaptics, (value) => { EnableUIHaptics = value; }, "Enable controller vibration while interacting with the User Interface.");
            panel.AddToggleOption(tab, "Physical Driving", PhysicalDriving, (value) => { PhysicalDriving = value;  }, "Grip Vehicle controls to steer.");
            panel.AddToggleOption(tab, "Locked Steering Grips", PhysicalLockedGrips, (value) => { PhysicalLockedGrips = value;  }, "Gripping the steering control locks your hands to the steering so you dont have to constantly grip. Grip again to unlock.");
            panel.AddToggleOption(tab, "Put hand reticle on laserpointer end", PutHandReticleOnLaserPointer, (value) => { PutHandReticleOnLaserPointer = value; PutHandReticleOnLaserPointerChanged(value); });
            panel.AddToggleOption(tab, "Put survival meter on left wrist", PutBarsOnWrist, (value) => { PutBarsOnWrist = value; PutBarsOnWristChanged(value); });
            panel.AddToggleOption(tab, "Invert Y Axis in Seamoth/Cameras", InvertYAxis, (value) => { InvertYAxis = value; InvertYAxisChanged(value); }, "Enables Y axis inversion for Seamoth and Cameras.");

            panel.AddHeading(tab, "Hidden/Advanced VR Settings(Those can cause motion sickness!)");
            panel.AddToggleOption(tab, "Enable pitching(Looking Up/Down) while diving", !VROptions.disableInputPitch, (value) => { VROptions.disableInputPitch = !value; }, "This allows you to pitch up and down using the right thumbstick when diving. Can be very disorienting! I recommend to keep this disabled!");
            panel.AddToggleOption(tab, "Enable desktop cinematics", VROptions.enableCinematics, (value) => { VROptions.enableCinematics = value; }, "Enables the games cinematics. Warning! Those move around your head and can cause motion sickness!");
            panel.AddToggleOption(tab, "Skip intro", VROptions.skipIntro, (value) => { VROptions.skipIntro = value; }, "Skip the intro when starting a new game.");

            panel.AddHeading(tab, "Debug Options");
            panel.AddToggleOption(tab, "Debug Overlays", IsDebugEnabled, (value) => { IsDebugEnabled = value; IsDebugChanged(value); }, "Enables Debug Overlays and Logs.");
            panel.AddToggleOption(tab, "Always show controllers", AlwaysShowControllers, (value) => { AlwaysShowControllers = value; AlwaysShowControllersChanged(value); }, "Shows the controllers at all times.");
            panel.AddToggleOption(tab, "Always show laserpointer", AlwaysShowLaserPointer, (value) => { AlwaysShowLaserPointer = value; AlwaysShowLaserPointerChanged(value); }, "Show the laserpointer at all times.");

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


    // GameOptions.GetVRAnimationMode returns true whenever we want to play the simplified VR Animations instead of the desktop ones
    [HarmonyPatch(typeof(GameOptions), nameof(GameOptions.GetVrAnimationMode))]
    class EnableCinematicsIfSet
    {
        static bool Prefix(ref bool __result)
        {
            __result = !VROptions.enableCinematics;
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

    #endregion
}
