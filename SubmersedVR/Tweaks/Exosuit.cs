using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{ 
    // Tweaks regarding the hoverbike in VR mode of the game
    static class ExosuitVR
    {

    }

    #region Patches
/*
    //Snap turning override
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.Update))]
    static class VehicleSnapTurnFixer
    {
        public static bool Prefix(Vehicle __instance)
        {
            if (__instance.CanPilot())
            {
                __instance.steeringWheelYaw = Mathf.Lerp(__instance.steeringWheelYaw, 0f, Time.deltaTime);
                __instance.steeringWheelPitch = Mathf.Lerp(__instance.steeringWheelPitch, 0f, Time.deltaTime);
                if (__instance.mainAnimator)
                {
                    __instance.mainAnimator.SetFloat("view_yaw", __instance.steeringWheelYaw * 70f);
                    __instance.mainAnimator.SetFloat("view_pitch", __instance.steeringWheelPitch * 45f);
                }
            }
            Mod.logger.LogInfo($"Vehicle.Update controlScheme = {__instance.controlSheme}");                       

            if (__instance.GetPilotingMode() && __instance.CanPilot() && (__instance.moveOnLand || __instance.transform.position.y < Ocean.GetOceanLevel()))
            {
                Vector2 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetLookDelta() : Vector2.zero;
                __instance.steeringWheelYaw = Mathf.Clamp(__instance.steeringWheelYaw + vector.x * __instance.steeringReponsiveness, -1f, 1f);
                __instance.steeringWheelPitch = Mathf.Clamp(__instance.steeringWheelPitch + vector.y * __instance.steeringReponsiveness, -1f, 1f);
                if (__instance.controlSheme == Vehicle.ControlSheme.Submersible)
                {
                    Mod.logger.LogInfo($"Vehicle.Update called scheme = {__instance.controlSheme}");
                    float d = 3f;
                    __instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * __instance.sidewaysTorque * 0.0015f * d, ForceMode.VelocityChange);
                    __instance.useRigidbody.AddTorque(__instance.transform.right * -vector.y * __instance.sidewaysTorque * 0.0015f * d, ForceMode.VelocityChange);
                    __instance.useRigidbody.AddTorque(__instance.transform.forward * -vector.x * __instance.sidewaysTorque * 0.0002f * d, ForceMode.VelocityChange);
                }
                else if (__instance.controlSheme == Vehicle.ControlSheme.Submarine || __instance.controlSheme == Vehicle.ControlSheme.Mech)
                {
                    Mod.logger.LogInfo($"Vehicle.Update in Exosuit {vector}");                       
                    //Exosuit
                    if (vector.x != 0f)
                    {
                        if(Settings.IsExosuitSnapTurningEnabled == false)
                        {
                            __instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * __instance.sidewaysTorque, ForceMode.VelocityChange);
                        }
                        if(Settings.IsExosuitSnapTurningEnabled == true)
                        {
                            Mod.logger.LogInfo($"Vehicle.Update with snap turning called x = {vector.x}");                       
                            __instance.useRigidbody.transform.rotation = Quaternion.Euler( new Vector3(  __instance.useRigidbody.transform.rotation.eulerAngles.x, __instance.useRigidbody.transform.rotation.eulerAngles.y + vector.x,  __instance.useRigidbody.transform.rotation.eulerAngles.z));
                        }
                    }
                }
                else if (__instance.controlSheme == Vehicle.ControlSheme.Hoverbike)
                {
                    Mod.logger.LogInfo($"Vehicle.Update called scheme = {__instance.controlSheme}");
                    __instance.useRigidbody.AddRelativeTorque(new Vector3(vector.y, 0f, 0f));
                }
            }
            bool flag = __instance.IsPowered();
            if (__instance.wasPowered != flag)
            {
                __instance.wasPowered = flag;
                __instance.OnPoweredChanged(flag);
            }
            __instance.ReplenishOxygen();
        
            return false;
        }
    }
*/
    #endregion

}
