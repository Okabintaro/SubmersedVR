using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{ 
    // Tweaks regarding the hoverbike in VR mode of the game
    static class HoverbikeVR
    {

    }

    #region Patches

    //This makes bike rotation occur immediately when moving the rotation control instead of using the "cone of view"
    [HarmonyPatch(typeof(HoverbikeHeadControlManager), nameof(HoverbikeHeadControlManager.Update))]
    public static class HoverbikeHeadControlFixer
    {
        
        public static bool Prefix(HoverbikeHeadControlManager __instance)
        {
            __instance.maxViewConeAperture = 0.0f;
            __instance.minViewConeAperture = 0.0f;

            return true;
        }
        
        //Eliminate the yaw and pitch lerps to reduce motion sickness
        public static void Postfix(HoverbikeHeadControlManager __instance)
        {
            if (__instance.activeManager && !Player.main.GetPDA().isInUse && !VROptions.enableCinematics)
		    {
                //Mod.logger.LogInfo($"HoverbikeHeadControlManager.Update called vector2 = { __instance.overflowInput}");
                __instance.smoothedBikeRot = new Vector3(0f, __instance.smoothedBikeRot.y, 0f);
                Vector3 b2 = new Vector3(__instance.defaultCameraPitch, 0f, 0f);
		        MainCameraControl.main.vehicleOverrideHeadRot = __instance.smoothedBikeRot + __instance.lookOffset + b2;
            }
        }
        
    }

    //While piloting vehicles the player's head camera gets locked into position when using XRSettings.enabled = true
    //While on the hoverbike we dont want to be locked in because we want to eliminate the yaw and pitch to reduce nausea
    //using the methods above
    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnUpdate))]
    static class MainCameraControl_Fixer
    {
        public static bool Prefix()
        {
            if(Player.main.inHovercraft)//(Player.main.GetComponentInParent<Hoverbike>() != null)
            {
                XRSettingsEnabled.isEnabled = false;
            }
            return true;
        }
        public static void Postfix()
        {
            if(Player.main.inHovercraft)//(Player.main.GetComponentInParent<Hoverbike>() != null)
            {
                XRSettingsEnabled.isEnabled = true;
            }
        }
    }

    //Snap turning override
    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.PhysicsMove))]
    static class HoverbikeSnapTurn
    {
        public static bool Prefix(Hoverbike __instance)
        {
            __instance.isPiloting = __instance.GetPilotingCraft();
            if (__instance.dockedPad)
            {
                return false;
            }
            if (!__instance.isPiloting)
            {
                return false;
            }
            __instance.moveDirection = (AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero);
            new Vector3(__instance.moveDirection.x, 0f, __instance.moveDirection.z);
            Vector2 vector = __instance.overflowInput;
            if (__instance.energyMixin.IsDepleted())
            {
                __instance.moveDirection = Vector3.zero;
            }
            __instance.appliedThrottle = (__instance.moveDirection != Vector3.zero);
            float d = __instance.overWater ? (__instance.horizontalDampening / __instance.waterDampening) : __instance.horizontalDampening;
            __instance.rb.AddTorque(__instance.transform.right * -vector.x * __instance.sidewaysTorque * __instance.verticalDampening, ForceMode.VelocityChange);
            if(Settings.IsSnowBikeSnapTurningEnabled == false)
            {
                __instance.rb.AddTorque(__instance.transform.up * vector.y * __instance.sidewaysTorque * d, ForceMode.VelocityChange);
            }
            if(Settings.IsSnowBikeSnapTurningEnabled == true)
            {
                __instance.rb.transform.rotation = Quaternion.Euler( new Vector3( __instance.rb.transform.rotation.eulerAngles.x, __instance.rb.transform.rotation.eulerAngles.y + vector.y,  __instance.rb.transform.rotation.eulerAngles.z));
            }
            Vector3 velocity = __instance.rb.velocity;
            Vector3 point = __instance.moveDirection;
            Mathf.Min(1f, point.magnitude);
            point.y = 0f;
            point.Normalize();
            __instance.horizMoveDir = MainCamera.camera.transform.rotation * point;
            float d2 = __instance.overWater ? (__instance.forwardAccel / __instance.waterDampening) : __instance.forwardAccel;
            __instance.rb.AddForce(__instance.horizMoveDir * d2);
           
            return false;
        }
    }

    #endregion

}
