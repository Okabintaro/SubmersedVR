using HarmonyLib;
using UnityEngine;
using System.Collections;
using UWE;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using UnityEngine.XR;

    // Tweaks regarding the cinematics in VR mode of the game
    static class SeaTruckVR
    {

    }

    #region Patches

     //Without this, SeaTruck lights turn on and off with every click on the PDA
    [HarmonyPatch(typeof(SeaTruckLights), nameof(SeaTruckLights.Update))]
    static class SeaTruckLights_PDA_Fixer
    {
        public static bool Prefix()
        {
            if(Player.main.GetPDA().isOpen)
            {
                return false;
            }
            return true;
        }
    }
     
    #endregion

}
