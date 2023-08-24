using HarmonyLib;
using UnityEngine;
using System.Collections;
using UWE;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using UnityEngine.XR;

    // Tweaks regarding the spy penguin in VR mode of the game
    static class SpyPenguinVR
    {

    }

    #region Patches


    //Get rid of the white overlay which is supposed to be a camera flash but was showing up in the photo.
    //If we figure out why UI items are not being hidden for photos we could put this back in if it 
    //gets scaled up significantly to cover the full screen
    [HarmonyPatch(typeof(uGUI_SpyPenguin), nameof(uGUI_SpyPenguin.OnSelfieTaken))]
    public static class SpyPenguinFix
    {
        public static bool Prefix()
        {
            return false;
        }
    }


    //eliminate camera noise glitch effects until penguin is almost out of range
    [HarmonyPatch(typeof(SpyPenguinCameraScreenFXController), nameof(SpyPenguinCameraScreenFXController.OnPreRender))]
    public static class SpyPenguinCameraNoiseFix
    {
        public static bool Prefix(SpyPenguinCameraScreenFXController __instance)
        {
            __instance.minimumNoise = 0.0f;
            return true;
        }
    }

    //eliminate pitch and roll from spy penguin camera
    [HarmonyPatch(typeof(SpyPenguin), nameof(SpyPenguin.UpdateDeployed))]
    public static class SpyPenguinRotationFix
    {
        public static void Postfix(SpyPenguin __instance)
        {
            if(VROptions.disableInputPitch) //disables adjusting camera with right joystick
            {
                __instance.cameraTransform.localRotation = Quaternion.Euler(new Vector3(0f, __instance.cameraTransform.localRotation.y, 0f)); 
            }
            //disables penguin camera tilt from rough terrain
            __instance.meshObject.transform.localRotation = Quaternion.Euler(new Vector3(0f,  __instance.meshObject.transform.localRotation.y, 0f));
        }
    }
   
    #endregion

}
