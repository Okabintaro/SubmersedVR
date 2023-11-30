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

    // [HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.OnPlayerEntered))]
    // static class RecenterInSeamoth
    // {
    //     public static void Postfix()
    //     {
    //         VRUtil.Recenter();
    //     }
    // }

    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.OnPlayerEntered))]
    static class RecenterInExosuit
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

    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.StartPiloting))]
    static class RecenterInSeaTruckMotor
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.EnterVehicle))]
    static class RecenterInHoverbike
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

}