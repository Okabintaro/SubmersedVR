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
                /*
                foreach (HideForScreenshots hideForScreenshots in UnityEngine.Object.FindObjectsOfType<HideForScreenshots>())
                {
                    Mod.logger.LogInfo($"HideForScreenshots {hideForScreenshots.type} {hideForScreenshots.name} {hideForScreenshots.tag} {hideForScreenshots.enabled}");
                }   
                */        
            }
        }
    }


/*
    //Make building with the fabricator much quicker
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
