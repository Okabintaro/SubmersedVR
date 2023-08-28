using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Tweaks regarding the spy penguin in VR mode of the game
    static class SpyPenguinVR
    {

    }

    #region Patches


    //Scale up the white camera flash overlay to cover the full screen
    [HarmonyPatch(typeof(uGUI_SpyPenguin), nameof(uGUI_SpyPenguin.OnSelfieTaken))]
    public static class SpyPenguinFix
    {
        public static bool Prefix(uGUI_SpyPenguin __instance)
        {
            __instance.cameraFlash.transform.localScale = new Vector3(3, 3, 3);
            return true;
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

    [HarmonyPatch(typeof(SpyPenguin), nameof(SpyPenguin.EnablePenguinCam))]
    public static class SpyPenguinHideBodyFix
    {
        public static void Postfix(SpyPenguin __instance)
        {
            //Mod.logger.LogInfo($"SpyPenguin.EnablePenguinCam called");
            VRHands.OnFullBodyChanged(true);
            VRHands.instance.SetBodyRendering(true);
        }
    }

    [HarmonyPatch(typeof(SpyPenguin), nameof(SpyPenguin.DisablePenguinCam))]
    public static class SpyPenguinShowBodyFix
    {
        public static void Postfix(SpyPenguin __instance)
        {
            //Mod.logger.LogInfo($"SpyPenguin.DisablePenguinCam called");
            VRHands.OnFullBodyChanged(Settings.FullBody);
            VRHands.instance.UpdateBody();
        }
    }
   
    //Dont show the selfie border
    [HarmonyPatch(typeof(uGUI_SpyPenguinSelfieBorder), nameof(uGUI_SpyPenguinSelfieBorder.Show))]
    public static class SpyPenguinCameraBorderFix
    {
        public static bool Prefix(SpyPenguinCameraScreenFXController __instance)
        {
            return false;
        }
    }

    #endregion

}
