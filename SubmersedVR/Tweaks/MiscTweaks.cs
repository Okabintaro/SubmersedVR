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

    //Press F5 with the flatscreen window active to disable current WBOIT shaders
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateKeyInputs))]
    public  class GameInputKeyTracker : MonoBehaviour
    {
        public static void Postfix(GameInput __instance, bool useKeyboard, bool useController)
        {
            //if(GameInput.lastInputPressed[(int)GameInput.lastDevice] != -1)
            //{
            //    Mod.logger.LogInfo($"GameInput.UpdateKeyInput called {GameInput.lastInputPressed[(int)GameInput.lastDevice]} ");
            //}
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 106) //F5 key
            {
                foreach (GameObject m in FindObjectsOfType(typeof(GameObject)) as GameObject[])
                {               
                    //if (m.name.Equals("airsack_fish_geo"))
                    //{
                        foreach (SkinnedMeshRenderer r in m.GetAllComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            foreach (Material mat in r.materials)
                            {
                                Mod.logger.LogInfo($"Gameobject {m.name} material {mat.name} {mat.renderQueue} {String.Join(", ", mat.shaderKeywords)} {mat.doubleSidedGI} {mat.color}");
                                //if (mat.shaderKeywords.Where(x => x.Equals("WBOIT")).Count() > 0)
                                //{
                                    mat.DisableKeyword("WBOIT");
                                    //File.AppendAllText("VRTweaksLog.txt", "Shader Keyword Disabled" + Environment.NewLine);
                                //}
                            }
                        }
                    //}
                    
                }
                        
                foreach (Material m in FindObjectsOfType(typeof(Material)) as Material[])
                {
                    Mod.logger.LogInfo($"Material {m.name} {String.Join(", ", m.shaderKeywords)}");
                    m.DisableKeyword("WBOIT");
                    //File.AppendAllText("VRTweaksLog.txt", m.name + " " + String.Join(", ", m.shaderKeywords) + Environment.NewLine);
                }
                             
                Shader.DisableKeyword("WBOIT");
            }
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
