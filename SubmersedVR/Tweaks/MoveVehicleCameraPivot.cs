using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{

    // Moves the camera/attach point of the seamoth 10 cm in front
    [HarmonyPatch(typeof(SeaMoth), nameof(SeaMoth.Start))]
    static class MoveSeamothCameraPivot
    {
        public static void Postfix(SeaMoth __instance)
        {
            __instance.playerPosition.transform.localPosition = new Vector3(0.0f, 0.0f, 0.2f);
        }
    }

    // Moves the camera/attach point of the exosuit 10 cm in front
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.Start))]
    static class MoveExosuitCameraPivot
    {
        public static void Postfix(Exosuit __instance)
        {
            __instance.playerPosition.transform.localPosition = new Vector3(0.0f, 0.0f, 0.2f);
        }
    }

}