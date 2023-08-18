using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections;
using UWE;
using System.Reflection.Emit;
using System.Reflection;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    using System;
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using UnityEngine.XR;

    // Tweaks regarding the cinematics in VR mode of the game
    static class MaterialsVR
    {

    }

    #region Patches

    //Fix for injection container sample being invisible (liquid inside still doesnt show)
    //sample_extraction_machine_containerContents_geo    material.renderer.renderqueue = 2500 frolo_Cure_Liquid (Instance)
    [HarmonyPatch(typeof(MobileExtractorMachine), nameof(MobileExtractorMachine.Start))]
    public static class MobileExtractorMachineAlphaFixer
    {
        
        public static bool Prefix(MobileExtractorMachine __instance)
        {
            foreach (SkinnedMeshRenderer r in __instance.GetAllComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material mat in r.materials)
                {
                    Mod.logger.LogInfo($"MobileExtractorMachine material {mat.name} {mat.renderQueue} {String.Join(", ", mat.shaderKeywords)} {mat.doubleSidedGI} {mat.color}");
                    mat.DisableKeyword("WBOIT");
                }
            }

            return true;
        }
                
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.Start))]
    public static class CreatureFixer
    {     
        public static void Postfix(Creature __instance)
        {
            GameObject go = __instance.gameObject;
            foreach (SkinnedMeshRenderer r in go.GetAllComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material mat in r.materials)
                {
                    //Mod.logger.LogInfo($"Creature material {mat.name} {mat.renderQueue} {String.Join(", ", mat.shaderKeywords)} {mat.doubleSidedGI} {mat.color}");
                    //if(mat.name == "airsack_fish_opacity (Instance)")
                    //{
                        mat.DisableKeyword("WBOIT");
                    //}
                }
            }
        }
    }

    [HarmonyPatch(typeof(OxygenPlant), nameof(OxygenPlant.Awake))]
    public static class OxygenPlantFixer
    {     
        public static void Postfix(OxygenPlant __instance)
        {
            GameObject go = __instance.gameObject;
            foreach (SkinnedMeshRenderer r in go.GetAllComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material mat in r.materials)
                {
                    mat.DisableKeyword("WBOIT");
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(Seaglide), nameof(Seaglide.Start))]
    public static class SeaglideAlphaFixer
    {
        
        public static bool Prefix(Seaglide __instance)
        {
            Mod.logger.LogInfo($"Seaglide {__instance.name}");
            foreach (SkinnedMeshRenderer r in __instance.GetAllComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material mat in r.materials)
                {
                    Mod.logger.LogInfo($"Seaglide material {mat.name} {mat.renderQueue} {String.Join(", ", mat.shaderKeywords)} {mat.doubleSidedGI} {mat.color}");
                    mat.DisableKeyword("WBOIT");
                    mat.renderQueue = 2500;
                }
            }
            
           
            return true;
        }                
    }
    
    #endregion

}
            
