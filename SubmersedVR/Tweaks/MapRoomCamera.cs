using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Tweaks regarding the toolbars in VR mode of the game
    static class MapRoomCameraVR
    {

    }

    #region Patches

    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.ControlCamera))]
    class MapRoomCameraStartFixer
    {
        public static void Postfix(MapRoomCamera __instance)
        {
            //turning off the armsController when IK is disabled does 
            //bad things when hands are turned back on
            if(Player.main.armsController.ik.enabled)
            {
                Player.main.armsController.gameObject.SetActive(false);
            }
            else
            {
                VRHands.instance?.SetHandRendering(false);
            }
        }
    }

    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.FreeCamera))]
    class MapRoomCameraEndFixer
    {
        public static void Postfix(MapRoomCamera __instance)
        {
            if(Player.main.armsController.ik.enabled)
            {
                Player.main.armsController.gameObject.SetActive(true);
            }
            else
            {
                VRHands.instance?.SetHandRendering(true);
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_CameraDrone), nameof(uGUI_CameraDrone.Awake))]
    class MapRoomCameraConnectingFixer
    {
        public static void Postfix(uGUI_CameraDrone __instance)
        {
             __instance.fader.transform.localScale = new Vector3(5, 5, 5);
        }
    }

    #endregion

}
