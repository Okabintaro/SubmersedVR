using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;
using System;
using System.Linq;
using Story;

namespace SubmersedVR
{
    public static class MiscTweaks
    {
        public static void BetterTextureQuality()
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.masterTextureLimit = 0;
        }
    }


    [HarmonyPatch(typeof(Bench), nameof(Bench.CanSit))]
    public static class CanAlwaysSit
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class QualityHook
    {
        public static void Postfix()
        {
            MiscTweaks.BetterTextureQuality();
        }
    }

    //Tracking goal completion to see if we can bypass Cut Scenes using this
    [HarmonyPatch(typeof(OnGoalUnlockTracker), nameof(OnGoalUnlockTracker.NotifyGoalComplete))]
    public static class OnGoalUnlockTrackerList
    {
        public static void Postfix(string completedGoal)
        {
            Mod.logger.LogInfo($"OnGoalUnlockTracker.NotifyGoalComplete called {completedGoal} ");
        }
    }

    //Add in the recentering  by using UnityEngine.XR InputTracking
    [HarmonyPatch(typeof(VRUtil), nameof(VRUtil.Recenter))]
    public static class RecenterFix
    {
        public static bool Prefix()
        {           
            InputTracking.Recenter();
            return true;
        }
    }

    
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateKeyInputs))]
    public  class GameInputKeyTracker : MonoBehaviour
    {
        public static void Postfix(GameInput __instance, bool useKeyboard, bool useController)
        {
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 107) //F6 key
            {
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 106) //F5 key
            {
            }
        }
    }

    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.MakeBubbles))]
    public static class PlayerBubbles_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
            //We are going to temporarily make the vrcamera the parent during the transform calculation 
            //and we want to set the parent back to the original when we are done
            //This makes it so you can spin in the real world and the bubbles will orient correctly (unlike the flippers)
            //but also does not lock the generated bubbles to the head so that you can look around and see them
            Transform parent = __instance.anchor.GetParent();
            Camera camera = VRCameraRig.instance?.vrCamera;
			if (camera != null)
			{
                __instance.anchor.SetParent(camera.transform);

 				Transform transform = camera.transform;
				Vector3 vector = transform.position + transform.forward * 0.2f; //put the bubbles a little out in front
                vector += new Vector3(0f, -0.2f, 0.0f); //move the bubbles a little lower toward the mouth
				Quaternion quaternion = transform.rotation;
                __instance.anchor.transform.SetPositionAndRotation(vector, quaternion);
                __instance.anchor.transform.localRotation = Quaternion.Euler( new Vector3(-90f, 0f, 0f) ); //setting the bubbles transform to match the camera breaks the bubbles x angle. Fix it here
                
                __instance.anchor.SetParent(parent);
			}        

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.MakeBreath))]
    public static class PlayerBreath_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
            
            //We are going to temporarily make the vrcamera the parent during the transform calculation 
            //and we want to set the parent back to the original when we are done
            //This makes it so you can spin in the real world and the breath will orient correctly (unlike the flippers)
            //but also does not lock the generated breath to the head so that you can look around and see it
            Transform parent = __instance.breathAnchor.GetParent();
            Camera camera = VRCameraRig.instance?.vrCamera;
			if (camera != null)
			{
                __instance.breathAnchor.SetParent(camera.transform);

 				Transform transform = camera.transform;
				Vector3 vector = transform.position + transform.forward * 0.05f; //put the breath a little out in front
                vector += new Vector3(0f, -0.15f, 0.0f); //move the breath a little lower toward the mouth
				Quaternion quaternion = transform.rotation;
                __instance.breathAnchor.transform.SetPositionAndRotation(vector, quaternion);
                //__instance.breathAnchor.transform.localRotation = Quaternion.Euler( new Vector3(-10f, 0f, 0f) ); //Make breath rise slightly
                
                __instance.breathAnchor.SetParent(parent);
			}        
            
            return true;
        }
    }
/*
    //Make bubbles generate more often
    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.Start))]
    public static class PlayerBreathBubbles_Patch2
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
           __instance.delay = 5.0f;
            return true;
        }
    }
*/
/*
    //Quick build until ghost material can be fixed
    [HarmonyPatch(typeof(Constructable), nameof(Constructable.GetConstructInterval))]
    public static class AutoBuildFix
    {
        public static bool Prefix(Base __instance, ref float __result)
        {           
           __result = 0.05f;
           return false;
        }
    }
*/

}
