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
/* Moved to SnapTurning.cs
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
*/

    #endregion

}
