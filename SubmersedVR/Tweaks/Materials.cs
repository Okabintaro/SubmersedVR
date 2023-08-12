using HarmonyLib;
using UnityEngine;
using System.Collections;
using UWE;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

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
            Renderer renderer = __instance.sample.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                foreach (Material mat in materials)
                {
                    //Mod.logger.LogInfo($"MobileExtractorMachine {mat.name}");
                    if(mat.name == "frolo_Cure_Liquid")
                    {
                        mat.renderQueue = (int)RenderQueue.GeometryLast;
                    }
                }
            }
            /*
            //MobileExtractorMachine canister MobileExtractionMachine_metal
            //MobileExtractorMachine canister MobileExtractionMachine_glass
            //MobileExtractorMachine canister MobileExtractionMachine_metal_orange
            //MobileExtractorMachine sample frolo_Cure_Liquid
            GameObject go = GameObject.Find("sample_extraction_machine_containerContents_geo");
            if(go != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    foreach (Material mat in materials)
                    {
                        //Mod.logger.LogInfo($"MobileExtractorMachine {mat.name}");
                        if(mat.name == "frolo_Cure_Liquid")
                        {
                            mat.renderQueue = (int)RenderQueue.GeometryLast;
                        }
                   }
                }
            }
            go =__instance.canister;
            if(go != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    foreach (Material mat in materials)
                    {
                        Mod.logger.LogInfo($"MobileExtractorMachine canister {mat.name}");
                   }
                }
            }
            go =__instance.sample;
            if(go != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    foreach (Material mat in materials)
                    {
                        Mod.logger.LogInfo($"MobileExtractorMachine sample {mat.name}");
                   }
                }
            }
            */
            return true;
        }
                
    }
/*
    [HarmonyPatch(typeof(Seaglide), nameof(Seaglide.Start))]
    public static class SeaglideAlphaFixer
    {
        
        public static bool Prefix(Seaglide __instance)
        {
            //x_CameraSeaGlide
            Mod.logger.LogInfo($"MobileExtractorMachine {__instance.screenEffectModel.name}");
            
            
            
            return true;
        }
        public static void Postfix(Seaglide __instance)
        {
			__instance.screenEffectMat1.renderQueue = 2500;
			__instance.screenEffectMat2.renderQueue = 2500;
            
        }
                
    }
*/
 
    #endregion

}
            
