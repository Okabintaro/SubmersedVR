using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;
using System;
using System.Linq;
using Story;
using BepInEx.Logging;

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


}
