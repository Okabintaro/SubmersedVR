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
    public class RecenterFix : MonoBehaviour
    {
        public static bool Prefix()
        {
            /*
            foreach (GameObject m in FindObjectsOfType(typeof(GameObject)) as GameObject[])
            {
               
                if (m.name.Equals("airsack_fish_geo"))
                {
                    foreach (SkinnedMeshRenderer r in m.GetAllComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        foreach (Material mat in r.materials)
                        {
                            if (mat.shaderKeywords.Where(x => x.Equals("WBOIT")).Count() > 0)
                            {
                                mat.DisableKeyword("WBOIT");
                                //File.AppendAllText("VRTweaksLog.txt", "Shader Keyword Disabled" + Environment.NewLine);
                            }
                        }
                    }
                }
                
            }
            */
            /*
            foreach (Material m in FindObjectsOfType(typeof(Material)) as Material[])
            {
                Mod.logger.LogInfo($"Material {m.name} {String.Join(", ", m.shaderKeywords)}");
                m.DisableKeyword("WBOIT");
                //File.AppendAllText("VRTweaksLog.txt", m.name + " " + String.Join(", ", m.shaderKeywords) + Environment.NewLine);
            }
            
            //Shader.DisableKeyword("WBOIT");
            */
            InputTracking.Recenter();
            return true;
        }
    }

}
