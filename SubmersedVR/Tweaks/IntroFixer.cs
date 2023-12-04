using System.IO;
using UnityEngine;
using HarmonyLib;

namespace SubmersedVR
{

    static class IntroFixerVR
    {
	    public static float animTime = 0.0f;
	    public static float animTimeTimeout = 15.0f;


    }

    //During the intro sequence, only the first 15 seconds controls the players head, allowing them to
    //be completely seated in the chair and oriented correctly. After that, to reduce nausea, head control by the app is disabled
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.UpdatePlayerPosition))]
    public static class IntroSequenceFixer
    {
        public static bool Prefix(PlayerCinematicController __instance)
        {        
            if(__instance.gameObject.name == "Life_Pod_damaged_03" && !VROptions.enableCinematics)
            {
                IntroFixerVR.animTime += Time.deltaTime;
                Transform component = __instance.player.GetComponent<Transform>();
                Transform component2 = MainCameraControl.main.GetComponent<Transform>();
                component.position = __instance.animatedTransform.position;
                component2.position = __instance.player.camAnchor.position;
                if(IntroFixerVR.animTime < IntroFixerVR.animTimeTimeout)
                {
                    component.rotation = __instance.animatedTransform.rotation;
                    component2.rotation = __instance.animatedTransform.rotation;
                }
                return false;
            }
		
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.EndCinematicMode))]
    public static class IntroSequenceFixerCompletion
    {
        public static void Postfix(PlayerCinematicController __instance)
        {        
            if(__instance.gameObject.name == "Life_Pod_damaged_03")
            {
                IntroFixerVR.animTime = 0f;
            }
        }
    }

}