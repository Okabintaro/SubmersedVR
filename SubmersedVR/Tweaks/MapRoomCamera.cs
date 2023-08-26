using HarmonyLib;

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
           Player.main.armsController.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.FreeCamera))]
    class MapRoomCameraEndFixer
    {
        public static void Postfix(MapRoomCamera __instance)
        {
            Player.main.armsController.gameObject.SetActive(true);
        }
    }

    #endregion

}
