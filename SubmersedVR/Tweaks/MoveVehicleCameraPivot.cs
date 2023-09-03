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

    //Set booleans in the VRCameraRig so component lookups dont need to be called during OnLateUpdate()
    //Actual offsets are handled in MainCameraControl.OnLateUpdate
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.OnPilotModeBegin))]
    static class MoveExosuitCameraPivot
    {
        public static void Postfix(Exosuit __instance)
        {
            Mod.logger.LogInfo($"Exosuit.OnPilotModeBegin called");
            VRCameraRig.instance.isPilotingExosuit = true;
        }
    }
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.OnPilotModeEnd))]
    static class ResetExosuitCameraPivot
    {
        public static void Postfix(Exosuit __instance)
        {
            VRCameraRig.instance.isPilotingExosuit = false;
        }
    }

    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.Update))]
    static class MoveSeaTruckMotorCameraPivot
    {
        public static void Postfix(SeaTruckMotor __instance)
        {
            if(__instance.IsPiloted())
            {
                VRCameraRig.instance.isPilotingSeaTruck = true;
            }
        }
    }

    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.StopPiloting))]
    static class ResetSeaTruckMotorCameraPivot
    {
        public static void Postfix(SeaTruckMotor __instance)
        {
            VRCameraRig.instance.isPilotingSeaTruck = false;
        }
    }


    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.EnterVehicle))]
    static class MoveHoverbikeCameraPivot
    {
        public static void Postfix(Hoverbike __instance)
        {
            VRCameraRig.instance.isPilotingSnowbike = true;
        }
    }

    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.ExitVehicle))]
    static class ReseteHoverbikeCameraPivot
    {
        public static void Postfix(Hoverbike __instance)
        {
            VRCameraRig.instance.isPilotingSnowbike = false;
        }
    }

}