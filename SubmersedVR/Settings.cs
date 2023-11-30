﻿using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using Yangrc.VolumeCloud;
using UnityEngine.XR;
using rail;

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

        //Ambient Occlusion Settings
        public static bool AOEnabled = false;
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
        //

        public static bool AlwaysShowControllers;
        public static event BooleanChanged AlwaysShowControllersChanged;

        public static string ShowLaserPointer = "Default";
        //public static event BooleanChanged AlwaysShowLaserPointerChanged;

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

        public static bool EnableGameHaptics = true;
        public static bool EnableUIHaptics = true;

        

        public static bool HandBasedTurning = false;
        public static bool LeftHandBasedTurning = false;
        public static bool ArticulatedHands = true;
        public static bool PhysicalDriving = false;
        public static bool PhysicalLockedGrips = false;

        //Keep Seamoth and Cyclops defaults so VRPhysicalPiloting will compile
        public static float SeamothLeftHorizontalCenterAngle = 0.0f;
        public static float SeamothLeftVerticleCenterAngle = 0.0f;
        public static float SeamothLeftDeadZone = 5.0f;
        public static float SeamothLeftSensitivity = 0.0f;

        public static float SeamothRightHorizontalCenterAngle = 0.0f;
        public static float SeamothRightVerticleCenterAngle = 0.0f;
        public static float SeamothRightDeadZone = 5.0f;

        public static float CyclopsLeftHorizontalCenterAngle = 0.0f;
        public static float CyclopsLeftVerticleCenterAngle = 0.0f;
        public static float CyclopsLeftDeadZone = 5.0f;

        public static float CyclopsRightHorizontalCenterAngle = 0.0f;
        public static float CyclopsRightVerticleCenterAngle = 0.0f;
        public static float CyclopsRightDeadZone = 5.0f;

        public static float SeatruckLeftHorizontalCenterAngle = 0.0f;
        public static float SeatruckLeftVerticleCenterAngle = 0.0f;
        public static float SeatruckLeftDeadZone = 5.0f;
        public static float SeatruckLeftSensitivity = 0.0f;

        public static float SeatruckRightHorizontalCenterAngle = 0.0f;
        public static float SeatruckRightVerticleCenterAngle = 0.0f;
        public static float SeatruckRightDeadZone = 5.0f;
        public static bool SeatruckAltLeftGrip = false;

        public static float ExosuitLeftHorizontalCenterAngle = 0.0f;
        public static float ExosuitLeftVerticleCenterAngle = 0.0f;
        public static float ExosuitLeftDeadZone = 5.0f;

        public static float ExosuitRightHorizontalCenterAngle = 0.0f;
        public static float ExosuitRightVerticleCenterAngle = 0.0f;
        public static float ExosuitRightDeadZone = 5.0f;

        public static float SnowbikeLeftHorizontalCenterAngle = 0.0f;
        public static float SnowbikeLeftVerticleCenterAngle = 0.0f;
        public static float SnowbikeLeftDeadZone = 5.0f;

        public static float SnowbikeRightHorizontalCenterAngle = 0.0f;
        public static float SnowbikeRightVerticleCenterAngle = 0.0f;
        public static float SnowbikeRightDeadZone = 5.0f;
        public static bool SnowbikeAltAccelerator = false;
 
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

        internal static void AddToGraphicsOptions(uGUI_OptionsPanel panel)
        {
            int tab = panel.tabs.Count - 1;

            string space = "   ";
            panel.AddHeading(tab, "Ambient Occlusion");
            panel.AddToggleOption(tab, space + "Enable", AOEnabled, (value) => { AOEnabled = AmbientOcclusionVR.enabled = value; AmbientOcclusionSettingsChanged(); }, "Use ambient occlusion. Increases demand on GPU.");
            panel.AddChoiceOption<string>(tab, space + "Method", new string[] {"Post Effect", "Deferred", "Debug"}, AOMethod, (value) => {
                AOMethod = value;
                if (AmbientOcclusionSettingsChanged != null) {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "Sample Count", new string[] {"Low", "Medium", "High", "Very High"}, AOSampleCount, (value) => {
                AOSampleCount = value;
                if (AmbientOcclusionSettingsChanged != null) {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "Per Pixel Normals", new string[] {"None", "Camera", "GBuffer", "Octa"}, AOPerPixelNormals, (value) => {
                AOPerPixelNormals = value;
                if (AmbientOcclusionSettingsChanged != null) {
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
            panel.AddToggleOption(tab, space + "Enable Temporal Filter", AOTemporalFilterEnabled, (value) => { AOTemporalFilterEnabled = AmbientOcclusionVR.FilterEnabled = value; AmbientOcclusionSettingsChanged(); }, "Accumulates the effect over the time.");
            panel.AddToggleOption(tab, space + "Temporal Filter Downsample", AOTemporalFilterDownsampleEnabled, (value) => { AOTemporalFilterDownsampleEnabled = value; AmbientOcclusionSettingsChanged(); }, "Effect at half of the resolution.");
            panel.AddSliderOption(tab, space + "Temporal Filter Blending", AOTemporalFilterBlending, 0f, 1.0f, AOTemporalFilterBlending, 0.02f, (value) => { AOTemporalFilterBlending = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "Temporal Filter Response", AOTemporalFilterResponse, 0f, 1.0f, AOTemporalFilterResponse, 0.02f, (value) => { AOTemporalFilterResponse = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");


        }

        internal static void AddMenu(uGUI_OptionsPanel panel)
        {
            int tab = panel.AddTab("Submersed VR");

            panel.AddHeading(tab, "Controls");
            panel.AddChoiceOption<string>(tab, "Movement Mode", new string[] {"Head Based", "Right Hand Based", "Left Hand Based"}, HandBasedTurning ? (LeftHandBasedTurning ? "Left Hand Based" : "Right Hand Based") : "Head Based", (value) => {
                HandBasedTurning = value == "Right Hand Based" || value == "Left Hand Based";
                LeftHandBasedTurning = value == "Left Hand Based";
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

            panel.AddHeading(tab, "Immersion");
            panel.AddToggleOption(tab, "Articulated Hands", ArticulatedHands, (value) => { ArticulatedHands = value;  }, "Hands animate based on the movement of your physical hands.");
            panel.AddToggleOption(tab, "Enable Game Haptics", EnableGameHaptics, (value) => { EnableGameHaptics = value; }, "Enable controller vibration while interacting with world objects.");
            panel.AddToggleOption(tab, "Enable UI Haptics", EnableUIHaptics, (value) => { EnableUIHaptics = value; }, "Enable controller vibration while interacting with the User Interface.");
            panel.AddToggleOption(tab, "Put survival meter on left wrist", PutBarsOnWrist, (value) => { PutBarsOnWrist = value; PutBarsOnWristChanged(value); });
            panel.AddChoiceOption<string>(tab, "Show Laser Pointer", new string[] {"Always", "Default", "Never"}, ShowLaserPointer , (value) => {
                ShowLaserPointer = value;
            });

            panel.AddHeading(tab, "Experimental");
            panel.AddToggleOption(tab, "Full Body", FullBody, (value) => { FullBody = value; FullBodyChanged(value); }, "See the full body instead of just the hands and feet.");
            panel.AddSliderOption(tab, "Body Scale", PlayerScale, 0.8f, 1.2f, PlayerScale, 0.01f, (value) => { PlayerScale = value; }, SliderLabelMode.Float, "0.00");
            panel.AddToggleOption(tab, "Put hand reticle on laserpointer end", PutHandReticleOnLaserPointer, (value) => { PutHandReticleOnLaserPointer = value; PutHandReticleOnLaserPointerChanged(value); });
            panel.AddToggleOption(tab, "Invert Y Axis in Seamoth/Cameras", InvertYAxis, (value) => { InvertYAxis = value; InvertYAxisChanged(value); }, "Enables Y axis inversion for Seamoth and Cameras.");

            panel.AddHeading(tab, "Hidden/Advanced VR Settings(Those can cause motion sickness!)");
            panel.AddToggleOption(tab, "Enable pitching(Looking Up/Down) while diving", !VROptions.disableInputPitch, (value) => { VROptions.disableInputPitch = !value; }, "This allows you to pitch up and down using the right thumbstick when diving. Can be very disorienting! I recommend to keep this disabled!");
            panel.AddToggleOption(tab, "Enable desktop cinematics", VROptions.enableCinematics, (value) => { VROptions.enableCinematics = value; }, "Enables the games cinematics. Warning! Those move around your head and can cause motion sickness!");
            panel.AddToggleOption(tab, "Skip intro", VROptions.skipIntro, (value) => { VROptions.skipIntro = value; }, "Skip the intro when starting a new game.");

            panel.AddHeading(tab, "Debug Options");
            panel.AddToggleOption(tab, "Debug Overlays", IsDebugEnabled, (value) => { IsDebugEnabled = value; IsDebugChanged(value); }, "Enables Debug Overlays and Logs.");
            panel.AddToggleOption(tab, "Always show controllers", AlwaysShowControllers, (value) => { AlwaysShowControllers = value; AlwaysShowControllersChanged(value); }, "Shows the controllers at all times.");
            //panel.AddToggleOption(tab, "Always show laserpointer", AlwaysShowLaserPointer, (value) => { AlwaysShowLaserPointer = value; AlwaysShowLaserPointerChanged(value); }, "Show the laserpointer at all times.");

            tab = panel.AddTab("Vehicles VR");
            panel.AddHeading(tab, "Comfort");
            panel.AddSliderOption(tab, "SeaTruck Pilot Position Offset", SeaTruckZOffset, -0.4f, 0.4f, SeaTruckZOffset, 0.01f, (value) => { SeaTruckZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "SeaTruck Pilot Height Offset", SeaTruckYOffset, -0.4f, 0.4f, SeaTruckYOffset, 0.01f, (value) => { SeaTruckYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Position Offset", ExosuitZOffset, -0.4f, 0.4f, ExosuitZOffset, 0.01f, (value) => { ExosuitZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Prawn Suit Height Offset", ExosuitYOffset, -0.4f, 0.4f, ExosuitYOffset, 0.01f, (value) => { ExosuitYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Snowbike Position Offset", SnowBikeZOffset, -0.4f, 0.4f, SnowBikeZOffset, 0.01f, (value) => { SnowBikeZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "Snowbike Height Offset", SnowBikeYOffset, -0.2f, 0.4f, SnowBikeYOffset, 0.01f, (value) => { SnowBikeYOffset = value; }, SliderLabelMode.Float, "0.00");
        

            panel.AddHeading(tab, "Options");
            panel.AddToggleOption(tab, "Physical Driving", PhysicalDriving, (value) => { PhysicalDriving = value;  }, "Grip Vehicle controls to steer.");
            panel.AddToggleOption(tab, "Locked Steering Grips", PhysicalLockedGrips, (value) => { PhysicalLockedGrips = value;  }, "Gripping the steering control locks your hands to the steering so you dont have to constantly grip. Grip again to unlock.");
            
            panel.AddHeading(tab, "SeaTruck Left Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", SeatruckLeftHorizontalCenterAngle, -10f, 10f, SeatruckLeftHorizontalCenterAngle, 1f, (value) => { SeatruckLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", SeatruckLeftVerticleCenterAngle, -10f, 10f, SeatruckLeftVerticleCenterAngle, 1f, (value) => { SeatruckLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", SeatruckLeftDeadZone, 1f, 10f, SeatruckLeftDeadZone, 1f, (value) => { SeatruckLeftDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");
            panel.AddToggleOption(tab, "Use Vertical Grip", SeatruckAltLeftGrip, (value) => { SeatruckAltLeftGrip = value;  }, "Use vertical hand grip rather than horizontal.");

            panel.AddHeading(tab, "SeaTruck Right Hand");
            panel.AddSliderOption(tab, "Center (Left/Right)", SeatruckRightHorizontalCenterAngle, -10f, 10f, SeatruckRightHorizontalCenterAngle, 1f, (value) => { SeatruckRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Up/Down)", SeatruckRightVerticleCenterAngle, -10f, 10f, SeatruckRightVerticleCenterAngle, 1f, (value) => { SeatruckRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity",SeatruckRightDeadZone, 1f, 10f, SeatruckRightDeadZone, 1f, (value) => { SeatruckRightDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
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

            panel.AddHeading(tab, "SnowFox Left Hand");
            //panel.AddSliderOption(tab, "Center (Left/Right)", SnowbikeLeftHorizontalCenterAngle, -10f, 10f, SnowbikeLeftHorizontalCenterAngle, 1f, (value) => { SnowbikeLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            //panel.AddSliderOption(tab, "Center (Up/Down)", SnowbikeLeftVerticleCenterAngle, -10f, 10f, SnowbikeLeftVerticleCenterAngle, 1f, (value) => { SnowbikeLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", SnowbikeLeftDeadZone, 1f, 10f, SnowbikeLeftDeadZone, 1f, (value) => { SnowbikeLeftDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "SnowFox Right Hand");
            //panel.AddSliderOption(tab, "Center (Left/Right)", SnowbikeRightHorizontalCenterAngle, -10f, 10f, SnowbikeRightHorizontalCenterAngle, 1f, (value) => { SnowbikeRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "Center (Accelerator)", SnowbikeRightVerticleCenterAngle, -10f, 10f, SnowbikeRightVerticleCenterAngle, 1f, (value) => { SnowbikeRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "Sensitivity", SnowbikeRightDeadZone, 1f, 10f, SnowbikeRightDeadZone, 1f, (value) => { SnowbikeRightDeadZone = value; }, SliderLabelMode.Float, "0", "Higher value means turns more quickly");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");
            panel.AddToggleOption(tab, "Invert Accelerator", SnowbikeAltAccelerator, (value) => { SnowbikeAltAccelerator = value;  }, "Twist forward to accelerate.");

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

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGraphicsTab))]
    static class UpdateGraphicsOptions
    {
        //Get rid of the default Ambient Occlusion Option
        public static bool Prefix(uGUI_OptionsPanel __instance)
        {
            int tabIndex = __instance.AddTab("Graphics");
            __instance.AddSliderOption(tabIndex, "Gamma", GammaCorrection.gamma, 0.1f, 2.8f, 1f, 0.01f, delegate(float value)
            {
                GammaCorrection.gamma = value;
            }, SliderLabelMode.Float, "0.00", null);
            int qualityPresetIndex = __instance.GetQualityPresetIndex();
            __instance.qualityPresetOption = __instance.AddChoiceOption(tabIndex, "Preset", uGUI_OptionsPanel.presetOptions, qualityPresetIndex, new UnityAction<int>(__instance.OnQualityPresetChanged), null);
            __instance.ApplyQualityPreset(qualityPresetIndex);
            __instance.AddHeading(tabIndex, "Advanced");
            if (uGUI_MainMenu.main)
            {
                int currentIndex;
                string[] detailOptions = uGUI_OptionsPanel.GetDetailOptions(out currentIndex);
                __instance.detailOption = __instance.AddChoiceOption(tabIndex, "Detail", detailOptions, currentIndex, new UnityAction<int>(__instance.OnDetailChanged), null);
            }
            __instance.waterQualityOption = __instance.AddChoiceOption<WaterSurface.Quality>(tabIndex, "WaterQuality", WaterSurface.GetQualityOptions(), WaterSurface.GetQuality(), new UnityAction<WaterSurface.Quality>(__instance.OnWaterQualityChanged), null);
            __instance.skyboxQualityOption = __instance.AddChoiceOption(tabIndex, "SkyboxQuality", uGUI_OptionsPanel.skyboxQualityOptions, VolumeCloudRenderer.GetQuality(), new UnityAction<int>(__instance.OnASkyboxqualityChanged), null);
            int currentIndex2;
            string[] antiAliasingOptions = uGUI_OptionsPanel.GetAntiAliasingOptions(out currentIndex2);
            __instance.aaModeOption = __instance.AddChoiceOption(tabIndex, "Antialiasing", antiAliasingOptions, currentIndex2, new UnityAction<int>(__instance.OnAAmodeChanged), null);
            __instance.aaQualityOption = __instance.AddChoiceOption(tabIndex, "AntialiasingQuality", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAaQuality(), new UnityAction<int>(__instance.OnAAqualityChanged), null);
            __instance.bloomOption = __instance.AddToggleOption(tabIndex, "Bloom", UwePostProcessingManager.GetBloomEnabled(), new UnityAction<bool>(__instance.OnBloomChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.lensDirtOption = __instance.AddToggleOption(tabIndex, "LensDirt", UwePostProcessingManager.GetBloomLensDirtEnabled(), new UnityAction<bool>(__instance.OnBloomLensDirtChanged), null);
                if (!GraphicsUtil.IsOpenGL())
                {
                    __instance.dofOption = __instance.AddToggleOption(tabIndex, "DepthOfField", UwePostProcessingManager.GetDofEnabled(), new UnityAction<bool>(__instance.OnDofChanged), null);
                }
                __instance.motionBlurQualityOption = __instance.AddChoiceOption(tabIndex, "MotionBlurQuality", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetMotionBlurQuality(), new UnityAction<int>(__instance.OnMotionBlurQualityChanged), null);
            }
            //__instance.aoQualityOption = __instance.AddChoiceOption(tabIndex, "AmbientOcclusion", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAoQuality(), new UnityAction<int>(this.OnAOqualityChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.ssrQualityOption = __instance.AddChoiceOption(tabIndex, "ScreenSpaceReflections", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetSsrQuality(), new UnityAction<int>(__instance.OnSSRqualityChanged), null);
                __instance.ditheringOption = __instance.AddToggleOption(tabIndex, "Dithering", UwePostProcessingManager.GetDitheringEnabled(), new UnityAction<bool>(__instance.OnDitheringChanged), null);
            }
            __instance.weatherQualityOption = __instance.AddChoiceOption(tabIndex, "WeatherQuality", uGUI_OptionsPanel.weatherQualityOptions, VFXWeatherManager.GetQuality(), new UnityAction<int>(__instance.OnAWeatherQualityChanged), null);       
        
            return false;
        }
        
        //Add in our own Ambient Occlusion Option
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
			Settings.AddToGraphicsOptions(__instance);
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
            //Mod.logger.LogInfo($"UwePostProcessingManager ApplySettingsToProfile called");
            __instance.SetAO(0);
            //__instance.SetDof(0);
            //__instance.SetSSR(0);
            //__instance.SetMotionBlur(0);
             MiscSettings.cameraBobbing = VROptions.enableCinematics; 

             //AmbientOcclusionVR.enabled = Settings.UseAmbientOcclusion;
             //AmbientOcclusionVR.FilterEnabled = Settings.AmbientOcclusionTemporalFilterEnabled;

        }
    }


    #endregion
}
