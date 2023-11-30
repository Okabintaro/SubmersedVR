using HarmonyLib;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace SubmersedVR
{
    // Tweaks regarding the seatruck in VR mode of the game
    static class SeaTruckVR
    {

    }

    #region Patches

     //Without this, SeaTruck lights turn on and off with every click on the PDA
    [HarmonyPatch(typeof(SeaTruckLights), nameof(SeaTruckLights.Update))]
    static class SeaTruckLights_PDA_Fixer
    {
        public static bool Prefix()
        {
            if(Player.main.GetPDA().isOpen)
            {
                return false;
            }
            return true;
        }
    }


    //Sep-2022 49371
/*     
    [HarmonyPatch(typeof(PostProcessingBehaviour), "OnEnable")]
    static class PostProcessingBehaviour_OnEnable_Patch
    {
        private static VignetteModel.Settings defaultVignetteSettings;
        static void Prefix(PostProcessingBehaviour __instance)
        {
            Mod.logger.LogInfo($"PostProcessingBehaviour.OnEnable called");
            if(__instance.profile != null)
            {
                defaultVignetteSettings = __instance.profile.vignette.settings;
            }
        }
    }

    [HarmonyPatch(typeof(PostProcessingBehaviour), "OnPreCull")]
    static class PostProcessingBehaviour_OnPreCull_Patch
    {
        static void Postfix(PostProcessingBehaviour __instance, ref VignetteComponent ___m_Vignette, ref BloomComponent ___m_Bloom, ref EyeAdaptationComponent ___m_EyeAdaptation, ref DepthOfFieldComponent ___m_DepthOfField, ref MotionBlurComponent ___m_MotionBlur, ref ColorGradingComponent ___m_ColorGrading, ref TaaComponent ___m_Taa, ref FxaaComponent ___m_Fxaa, ref AmbientOcclusionComponent ___m_AmbientOcclusion)
        {
            Mod.logger.LogInfo($"PostProcessingBehaviour.OnPreCull called enabled = {__instance.enabled} profile = {__instance.profile != null} camera = {__instance.GetComponent<Camera>() != null}");
            if (true)
            {
                
                VignetteModel.Settings vSettings = new VignetteModel.Settings
                {
                    mode = VignetteModel.Mode.Classic,
                    intensity = 0.8f,
                    color = new Color(0f, 0f, 0f, 1.0f),
                    center = new Vector2(0.5f,0.5f),
                    smoothness = 0.5f,
                    roundness = 1f,
                    rounded = true
                };
                ___m_Vignette.model.settings = vSettings;
                ___m_Vignette.model.enabled = true;
                ___m_Vignette.model.m_Enabled = true;
            }
        }
    }
*/
/*
    [HarmonyPatch(typeof(UwePostProcessingManager), nameof(UwePostProcessingManager.Update))]
    static class PPFixer
    {
        public static void Postfix(UwePostProcessingManager __instance)
        {
            Mod.logger.LogInfo($"(UwePostProcessingManager.Update called");
            __instance.behaviour.enabled = false;
            VignetteModel.Settings settings = UwePostProcessingManager.currentProfile.vignette.settings;
            settings.mode = VignetteModel.Mode.Classic;
            settings.intensity = 0.8f;
            settings.color = new Color(1f, 0f, 0f, 1.0f);
            settings.center = new Vector2(0.5f,0.5f);
            settings.smoothness = 0.5f;
            settings.roundness = 1f;
            settings.rounded = true;
           
            UwePostProcessingManager.currentProfile.vignette.settings = settings;
            UwePostProcessingManager.currentProfile.vignette.enabled = true;
            __instance.behaviour.enabled = true;
        }
    }
*/

/*
    //This correctly does snap turning but if the seatruck has many modules attached to it
    //then a snap turn near fixed geometry can cause an extreme physics collision that will throw the seatruck far away.
    //It will probably be better to do a rigid body turn with a tunnel vignette overlay but I cant
    //get vignette post processing to work
    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.Update))]
    static class SeaTruckMotorSnapTurnFixer
    {
        public static bool Prefix(SeaTruckMotor __instance)
        {
            
            // PostProcessingBehaviour component = VRCameraRig.instance.vrCamera.GetComponent<PostProcessingBehaviour>();
            // if(component != null)
            // {
            //     VignetteModel.Settings settings = component.profile.vignette.settings;
            //     settings.mode = VignetteModel.Mode.Classic;
            //     settings.intensity = 1.0f;
            //     settings.color = new Color(1f, 0f, 0f, 1.0f);
            //     settings.center = new Vector2(0.5f,0.5f);
            //     settings.smoothness = 0.5f;
            //     settings.roundness = 1f;
            //     settings.rounded = true;
            //     component.profile.vignette.settings = settings;
            //     component.profile.vignette.enabled = true;
            //     component.enabled = true;
            // }
            

            if (__instance.afterBurnerActive && Time.time > __instance.afterBurnerTime)
            {
                __instance.afterBurnerActive = false;
            }
            __instance.UpdateDrag();
            if (__instance.piloting && __instance.useRigidbody != null && !__instance.IsBusyAnimating() && !__instance.waitForDocking)
            {
                if (((__instance.truckSegment.isMainCab ? (AvatarInputHandler.main.IsEnabled() && !Player.main.GetPDA().isInUse) : __instance.inputStackDummy.activeInHierarchy) & (__instance.dockable == null || (!__instance.dockable.isDocked && !__instance.dockable.isInTransition))) && GameInput.GetButtonDown(GameInput.Button.Exit))
                {
                    __instance.StopPiloting(false, false, false, false, false, null);
                }
                else if (!__instance.truckSegment.isMainCab && GameInput.GetButtonDown(GameInput.Button.PDA))
                {
                    __instance.StopPiloting(false, false, false, false, false, null);
                    __instance.OpenPDADelayed(0.7f);
                }
                else if (!__instance.truckSegment.isMainCab && __instance.pilotPosition.position.y > -1.5f)
                {
                    __instance.StopPiloting(false, false, false, false, false, null);
                }
                else if (!__instance.truckSegment.underCreatureAttack && __instance.IsPowered())
                {
                    if (__instance.CanTurn())
                    {
                        Vector2 vector = (AvatarInputHandler.main.IsEnabled() || __instance.inputStackDummy.activeInHierarchy) ? GameInput.GetLookDelta() : Vector2.zero;
                        //vector.x = Mathf.Clamp(vector.x, -30f, 30f);
                        vector.y = Mathf.Clamp(vector.y, -30f, 30f);
                        //Mod.logger.LogInfo($"SeaTruckMotor.Update called vector = {vector} animator = {__instance.animator}");
                        Int2 joystickDirection;
                        if (vector.x > 0f)
                        {
                            joystickDirection.x = 1;
                        }
                        else if (vector.x < 0f)
                        {
                            joystickDirection.x = -1;
                        }
                        else
                        {
                            joystickDirection.x = 0;
                        }
                        if (vector.y > 0f)
                        {
                            joystickDirection.y = -1;
                        }
                        else if (vector.y < 0f)
                        {
                            joystickDirection.y = 1;
                        }
                        else
                        {
                            joystickDirection.y = 0;
                        }
                        __instance.joystickDirection = joystickDirection;
                        float d = 1f / Mathf.Max(1f, __instance.GetWeight() * 0.8f) * __instance.steeringMultiplier;
                        if(Settings.IsVehicleSnapTurningEnabled == false)
                        {
                            __instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * d, ForceMode.VelocityChange);
                        }
                        __instance.useRigidbody.AddTorque(__instance.transform.right * -vector.y * d, ForceMode.VelocityChange);
                        if(Settings.IsVehicleSnapTurningEnabled == false)
                        {
                            __instance.useRigidbody.AddTorque(__instance.transform.forward * -vector.x * d * 0.02f, ForceMode.VelocityChange);                       
                        }
                        if(Settings.IsVehicleSnapTurningEnabled == true)
                        {
                            //__instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * d, ForceMode.VelocityChange);
                            __instance.useRigidbody.transform.rotation = Quaternion.Euler( new Vector3(  __instance.useRigidbody.transform.rotation.eulerAngles.x, __instance.useRigidbody.transform.rotation.eulerAngles.y + vector.x,  __instance.useRigidbody.transform.rotation.eulerAngles.z));
                            __instance.useRigidbody.AddTorque(__instance.transform.forward * -vector.x * d * 0.02f, ForceMode.VelocityChange);                       
                        }
                        if(__instance.useRigidbody.detectCollisions)
                        if (__instance.animator)
                        {
                            __instance.smoothedJoystick = UWE.Utils.LerpVector(__instance.smoothedJoystick, joystickDirection.ToVector2(), Time.deltaTime * 2f);
                            __instance.animator.SetFloat("view_pitch", __instance.smoothedJoystick.y);
                            __instance.animator.SetFloat("view_yaw", __instance.smoothedJoystick.x);
                            __instance.animAccel = Mathf.Lerp(__instance.animAccel, (float)__instance.leverDirection.y, Time.deltaTime * 3f);
                            __instance.animator.SetFloat("move_speed_z", __instance.animAccel);
                        }                       
                    }
                    if (__instance.upgrades && GameInput.GetButtonDown(GameInput.Button.Sprint))
                    {
                        __instance.upgrades.TryActivateAfterBurner();
                    }
                }
                if (__instance.inputStackDummy.activeInHierarchy && IngameMenu.main != null)
                {
                    if (GameInput.GetButtonDown(GameInput.Button.UIMenu))
                    {
                        IngameMenu.main.Open();
                    }
                    else if (!IngameMenu.main.gameObject.activeInHierarchy)
                    {
                        UWE.Utils.lockCursor = true;
                    }
                }
            }
            if (__instance.engineSound)
            {
                if (__instance.piloting && __instance.IsPowered())
                {
                    __instance.engineSound.Play();
                    __instance.engineSound.SetParameterValue(__instance.velocityParamIndex, __instance.useRigidbody.velocity.magnitude);
                    __instance.engineSound.SetParameterValue(__instance.depthParamIndex, __instance.transform.position.y);
                    __instance.engineSound.SetParameterValue(__instance.rpmParamIndex, (GameInput.GetMoveDirection().z + 1f) * 0.5f);
                    __instance.engineSound.SetParameterValue(__instance.turnParamIndex, Mathf.Clamp(GameInput.GetLookDelta().x * 0.3f, -1f, 1f));
                    __instance.engineSound.SetParameterValue(__instance.upgradeParamIndex, (float)(((__instance.powerEfficiencyFactor < 1f) ? 1 : 0) + (__instance.horsePowerUpgrade ? 2 : 0)));
                    if (__instance.liveMixin)
                    {
                        __instance.engineSound.SetParameterValue(__instance.damagedParamIndex, 1f - __instance.liveMixin.GetHealthFraction());
                    }
                }
                else
                {
                    __instance.engineSound.Stop();
                }
            }
            if (__instance.waitForDocking && !__instance.truckSegment.IsDocking())
            {
                __instance.waitForDocking = false;
                Player.main.ExitLockedMode(false, false, null);
            }            
            
            return false;
        }
    }
*/     
    #endregion

}
