using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{

    // TODO: Apply for SeaTruck
    // // Moves the camera/attach point of the seamoth 10 cm in front
    // [HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.Start))]
    // static class MoveSeamothCameraPivot
    // {
    //     public static void Postfix(SeaMoth __instance)
    //     {
    //         __instance.playerPosition.transform.localPosition = new Vector3(0.0f, 0.0f, 0.2f);
    //     }
    // }

    // Moves the camera/attach point of the exosuit 10 cm in front
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.Start))]
    static class MoveExosuitCameraPivot
    {
        public static void Postfix(Exosuit __instance)
        {
            __instance.playerPosition.transform.localPosition = new Vector3(0.0f, 0.2f, 0.2f);
        }
    }

    //__instance.pilotPosition.localPosition should be used but Player.main.mode = Player.Mode.LockedPiloting is somehow
    //interfering with that. Using Player.main.transform.localPosition instead which causes the PDA to jitter
    //when opened while piloting the SeaTruck because Player.main.transform.localPosition is fighting with LockedPiloting
    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.Update))]
    static class MoveSeaTruckMotorCameraPivot
    {
        public static void Prefix(SeaTruckMotor __instance)
        {
            if(__instance.IsPiloted())
            {
                Player.main.transform.localPosition = new Vector3(0.0f, 0.0f, 0.35f);
            }
        }
    }


    // Moves the camera/attach point of the Hoverebike 20 cm in front
    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.Update))]
    static class MoveHoverbikeCameraPivot
    {
        public static void Postfix(Hoverbike __instance)
        {
            if(__instance.isPiloting)
            {
                Player.main.transform.localPosition = new Vector3(0.0f, 0.0f, 0.1f);
            }
        }
    }

}