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

        public static bool IsExosuitSnapTurningEnabled;
        public static event BooleanChanged IsExosuitSnapTurningEnabledChanged;
        public static float ExosuitSnapTurningAngle = 45.0f;
        public static event FloatChanged ExosuitSnapTurningAngleChanged;
        public static bool IsSnowBikeSnapTurningEnabled;
        public static event BooleanChanged IsSnowBikeSnapTurningEnabledChanged;
        public static float SnowBikeSnapTurningAngle = 45.0f;
        public static event FloatChanged SnowBikeSnapTurningAngleChanged;

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

        public static bool FullBody;
        public static event BooleanChanged FullBodyChanged;

        public static float SeaTruckZOffset = 0.0f;
        public static float SeaTruckYOffset = 0.0f;
        public static float SnowBikeZOffset = 0.0f;
        public static float SnowBikeYOffset = 0.0f;
        public static float ExosuitZOffset = 0.0f;
        public static float ExosuitYOffset = 0.0f;
        // public static float HudDistance = 1.0f;
        // public static event FloatChanged HudDistanceChanged;
        public static float PlayerScale = 1.0f;

        public static bool EnableGameHaptics = false;
        public static bool EnableUIHaptics = false;

        public static bool HandBasedTurning = false;
        public static bool ArticulatedHands = false;
        public static bool PhysicalDriving = false;
        public static bool PhysicalLockedGrips = false;


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
            panel.AddChoiceOption<string>(tab, "Movement Mode", new string[] {"Head Based", "Hand Based"}, HandBasedTurning ? "Hand Based" : "Head Based", (value) => {
                HandBasedTurning = value == "Hand Based";
            });
            panel.AddToggleOption(tab, "Enable Player Snap Turning", IsSnapTurningEnabled, (value) => { IsSnapTurningEnabled = value;
                if (IsSnapTurningEnabledChanged != null) {
                    IsSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "Player Snap Turning Angle(°)", new float[] {22.5f, 30f, 45f, 90f}, SnapTurningAngle, (value) => {
                SnapTurningAngle = value;
                if (SnapTurningAngleChanged != null) {
                    SnapTurningAngleChanged(value);
                }
            });
            panel.AddToggleOption(tab, "Enable Prawn Suit Snap Turning", IsExosuitSnapTurningEnabled, (value) => { IsExosuitSnapTurningEnabled = value;
                if (IsExosuitSnapTurningEnabledChanged != null) {
                    IsExosuitSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "Prawn Suit Snap Turning Angle(°)", new float[] {22.5f, 30f, 45f, 90f}, ExosuitSnapTurningAngle, (value) => {
                ExosuitSnapTurningAngle = value;
                if (ExosuitSnapTurningAngleChanged != null) {
                    ExosuitSnapTurningAngleChanged(value);
                }
            });
           panel.AddToggleOption(tab, "Enable Snowfox Snap Turning", IsSnowBikeSnapTurningEnabled, (value) => { IsSnowBikeSnapTurningEnabled = value;
                if (IsSnowBikeSnapTurningEnabledChanged != null) {
                    IsSnowBikeSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "Snowfox Snap Turning Angle(°)", new float[] {22.5f, 30f, 45f, 90f}, SnowBikeSnapTurningAngle, (value) => {
                SnowBikeSnapTurningAngle = value;
                if (SnowBikeSnapTurningAngleChanged != null) {
                    SnowBikeSnapTurningAngleChanged(value);
                }
            });

            panel.AddHeading(tab, "Comfort");
            panel.AddSliderOption(tab, "SeaTruck Pilot Position Offset", SeaTruckZOffset, -0.4f, 0.4f, SeaTruckZOffset, 0.01f, (value) => { SeaTruckZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "SeaTruck Pilot Height Offset", SeaTruckYOffset, -0.4f, 0.4f, SeaTruckYOffset, 0.01f, (value) => { SeaTruckYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Position Offset", ExosuitZOffset, -0.4f, 0.4f, ExosuitZOffset, 0.01f, (value) => { ExosuitZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Height Offset", ExosuitYOffset, -0.4f, 0.4f, ExosuitYOffset, 0.01f, (value) => { ExosuitYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Snowbike Position Offset", SnowBikeZOffset, -0.4f, 0.4f, SnowBikeZOffset, 0.01f, (value) => { SnowBikeZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Snowbike Height Offset", SnowBikeYOffset, -0.2f, 0.4f, SnowBikeYOffset, 0.01f, (value) => { SnowBikeYOffset = value; }, SliderLabelMode.Float, "0.00");
        
            panel.AddHeading(tab, "Experimental");
            panel.AddToggleOption(tab, "Articulated Hands", ArticulatedHands, (value) => { ArticulatedHands = value;  }, "Hands animate based on the movement of your physical hands.");
            panel.AddToggleOption(tab, "Physical Driving", PhysicalDriving, (value) => { PhysicalDriving = value;  }, "Grip Vehicle controls to steer.");
            panel.AddToggleOption(tab, "Locked Steering Grips", PhysicalLockedGrips, (value) => { PhysicalLockedGrips = value;  }, "Gripping the steering control locks your hands to the steering so you dont have to constantly grip. Grip again to unlock.");
            panel.AddToggleOption(tab, "Full Body", FullBody, (value) => { FullBody = value; FullBodyChanged(value); }, "See the full body instead of just the hands and feet.");
            panel.AddSliderOption(tab, "Body Scale", PlayerScale, 0.8f, 1.2f, PlayerScale, 0.01f, (value) => { PlayerScale = value; }, SliderLabelMode.Float, "0.00");
            panel.AddToggleOption(tab, "Enable Game Haptics", EnableGameHaptics, (value) => { EnableGameHaptics = value; }, "Enable controller vibration while interacting with world objects.");
            panel.AddToggleOption(tab, "Enable UI Haptics", EnableUIHaptics, (value) => { EnableUIHaptics = value; }, "Enable controller vibration while interacting with the User Interface.");
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

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.Update))]
    static class AlwaysEnableBackButton
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            __instance.UpdateButtonState(__instance.backButton, true);
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

    // The next two function add back in the ability to toggle fullscreen on the flatscreen display.
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGeneralTab))]
    static class ReAddFullscreenOption
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
			__instance.AddToggleOption(__instance.tabs.Count - 1, "Fullscreen", Screen.fullScreen, new UnityAction<bool>(__instance.OnFullscreenChanged), null);
        }
    }

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.OnScreenChanged))]
    static class OnScreenChangedFixer
    {
        public static bool Prefix(uGUI_OptionsPanel __instance)
        {
            if (__instance.AreDisplayOptionsEnabled() && __instance.resolutionOption)
            {
                __instance.resolutionOption.value = uGUI_OptionsPanel.GetCurrentResolutionIndex(__instance.resolutions);
                __instance.toApply.Remove(uGUI_OptionsPanel.Change.Resolution);
            }
            if(__instance.hFovSlider != null)
            {
                __instance.OnVFovChanged(MiscSettings.fieldOfView);  
            } 

            return false;    
        }
    }


    // GameOptions.GetVRAnimationMode returns true whenever we want to play the simplified VR Animations instead of the desktop ones
    [HarmonyPatch(typeof(VRGameOptions), nameof(VRGameOptions.GetVrAnimationMode))]
    class EnableCinematicsIfSet
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;//!VROptions.enableCinematics;
            return false;
        }
    }

    //XRSettings sets most of the graphics settings correctly for comfort but it still allows Ambient Occlusion to be set
    //AO appears to only be updating in one eye at the moment so I am disabling it here until we can either
    //default it to off in new installations or fix the issue with only one eye rendering
    [HarmonyPatch(typeof(UwePostProcessingManager), nameof(UwePostProcessingManager.ApplySettingsToProfile))]
    public static class FixGraphicsForVR
    {
        public static void Postfix(UwePostProcessingManager __instance)
        {
            __instance.SetAO(0);
            //__instance.SetDof(0);
            //__instance.SetSSR(0);
            //__instance.SetMotionBlur(0);
             MiscSettings.cameraBobbing = VROptions.enableCinematics; 

        }
    }


    #endregion
}
