using HarmonyLib;

namespace SubmersedVR
{
    static class VehiclesVR
    {
        public static Exosuit PilotedExosuit()
        {
            return Player.main?.currentMountedVehicle == null ? null : Player.main?.currentMountedVehicle as Exosuit;
        }
    }

    //This gets called when starting to pilot the seamoth and exosuit but not the cyclops
    [HarmonyPatch(typeof(Player), nameof(Player.EnterLockedMode))]
    static class RecenterWhenPilotingLocked
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.EnterPilotingMode))]
    static class RecenterWhenPilotingCyclops
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

    //This gets called when, while piloting the cyclops, camera mode is turned on
    [HarmonyPatch(typeof(CyclopsExternalCams), nameof(CyclopsExternalCams.SetActive))]
    static class RecenterWhenUsingCyclopsCams
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

    /*
    [HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.OnPlayerEntered))]
    static class RecenterInSeamoth
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.OnPlayerEntered))]
    static class RecenterInExosuit
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }
*/

}