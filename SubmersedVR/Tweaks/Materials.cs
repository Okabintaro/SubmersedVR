using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace SubmersedVR
{
    // Tweaks regarding the materials in VR mode of the game
    static class MaterialsVR
    {

    }

    #region Patches

    //https://github.com/elliotttate/VRTweaks/blob/master/VRTweaks/WBOITFixes.cs
    [HarmonyPatch(typeof(WBOIT), nameof(WBOIT.CreateRenderTargets))]
    internal class CreateRenderTargets_Patch
    {
        public static bool Prefix(WBOIT __instance)
        {
            __instance.wboitTexture1 = DynamicResolution.CreateRenderTexture(__instance.camera.pixelWidth, __instance.camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            __instance.wboitTexture1.name = "WBOIT TexA";
            __instance.wboitTexture2 = DynamicResolution.CreateRenderTexture(__instance.camera.pixelWidth, __instance.camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            __instance.wboitTexture2.name = "WBOIT TexB";
            EditorModifications.SetOITTargets(__instance.camera, __instance.wboitTexture1, __instance.wboitTexture2);
            WBOIT.renderTargetIdentifiers[0] = BuiltinRenderTextureType.CameraTarget;
            WBOIT.renderTargetIdentifiers[1] = new RenderTargetIdentifier(__instance.wboitTexture1);
            WBOIT.renderTargetIdentifiers[2] = new RenderTargetIdentifier(__instance.wboitTexture2);
            __instance.compositeMaterial.SetTexture(__instance.texAPropertyID, __instance.wboitTexture1);
            __instance.compositeMaterial.SetTexture(__instance.texBPropertyID, __instance.wboitTexture2);
            return false;
        }
    }

    [HarmonyPatch(typeof(WBOIT), nameof(WBOIT.VerifyRenderTargets))]
    internal class VerifyRenderTargets_Patch
    {
        private static MethodInfo screenGetWidth = AccessTools.Method(typeof(Screen), "get_width");
        private static MethodInfo screenGetHeight = AccessTools.Method(typeof(Screen), "get_height");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = new List<CodeInstruction>(instructions);
            var patched = new List<CodeInstruction>();
            for (int i = 0; i < original.Count; i++)
            {
                var instruction = original[i];
                if (instruction.Calls(screenGetHeight))
                {
                    patched.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    patched.Add(CodeInstruction.LoadField(typeof(WBOIT), "camera"));
                    patched.Add(CodeInstruction.Call(typeof(Camera), "get_pixelHeight"));
                }
                else if (instruction.Calls(screenGetWidth))
                {
                    patched.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    patched.Add(CodeInstruction.LoadField(typeof(WBOIT), "camera"));
                    patched.Add(CodeInstruction.Call(typeof(Camera), "get_pixelWidth"));
                }
                else
                {
                    patched.Add(instruction);
                }
            }
            return patched;

        }
    }

/*
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
*/

    #endregion

}
            
