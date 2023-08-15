using HarmonyLib;
using UnityEngine;
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
/*
    [HarmonyPatch(typeof(Constructable), nameof(Constructable.ReplaceMaterials))]
    public static class ConstructableMaterialFixer
    {  
        public static bool Prefix(Constructable __instance, GameObject rootObject)
        {
            Mod.logger.LogInfo($"Constructable.ReplaceMaterials called {__instance.ghostMaterial != null}");        

            if (__instance.ghostMaterial != null)
            {
                //__instance.ghostMaterial.renderQueue = (int)RenderQueue.GeometryLast;
                __instance.ghostOverlay = __instance.gameObject.AddComponent<VFXOverlayMaterial>();
                __instance.ghostOverlay.ApplyOverlay(__instance.ghostMaterial, "ConstructableGhost", false, null);
            }
            using (ListPool<Renderer> listPool = Pool<ListPool<Renderer>>.Get())
            {
                List<Renderer> list = listPool.list;
                rootObject.GetComponentsInChildren<Renderer>(true, list);
                for (int i = 0; i < list.Count; i++)
                {
                    Renderer renderer = list[i];
                    bool flag = false;
                    RendererMaterialsStorage materialsStorageForRenderer = __instance.GetMaterialsStorageForRenderer(renderer);
                    for (int j = 0; j < materialsStorageForRenderer.Count; j++)
                    {
                        Material material = materialsStorageForRenderer.GetSharedMaterial(j);
                        if (!(material == null) && !(material.shader == null) && material.HasProperty(ShaderPropertyID._BuildLinear))
                        {
                            material = materialsStorageForRenderer.GetOrCreateInstancedMaterial(j, false);
                            flag = true;
                            material.EnableKeyword("FX_BUILDING");
                            material.SetTexture(ShaderPropertyID._EmissiveTex, __instance._EmissiveTex);
                            material.SetFloat(ShaderPropertyID._Cutoff, 0.42f);
                            material.SetColor(ShaderPropertyID._BorderColor, new Color(0.7f, 0.7f, 1f, 1f));
                            material.SetFloat(ShaderPropertyID._Built, 0f);
                            material.SetVector(ShaderPropertyID._BuildParams, new Vector4(0.1f, 0.25f, 0.2f, -0.2f));
                            material.SetFloat(ShaderPropertyID._NoiseStr, 1.9f);
                            material.SetFloat(ShaderPropertyID._NoiseThickness, 0.48f);
                            material.SetFloat(ShaderPropertyID._BuildLinear, 0f);
                        }
                    }
                    if (flag)
                    {
                        materialsStorageForRenderer.ApplyInstancedMaterials();
                        __instance.renderersMaterials.Add(materialsStorageForRenderer);
                    }
                }
            }
            Shader.SetGlobalFloat(ShaderPropertyID._SubConstructProgress, 0f);
           
            return false;
        }
                
    }

    [HarmonyPatch(typeof(VFXOverlayMaterial), nameof(VFXOverlayMaterial.ApplyOverlay))]
    public static class VFXOverlayMaterialFixer
    {  
        public static bool Prefix(VFXOverlayMaterial __instance, Material mat, string debugName, bool instantiateMaterial, Renderer[] rends = null)
        {
            Mod.logger.LogInfo($"VFXOverlayMaterial.ApplyOverlay called {rends == null} {mat.color}");        

            if (rends == null)
            {
                rends = __instance.GetComponentsInChildren<Renderer>();
            }
            __instance.SetRenderers(rends);
            __instance.initColor = mat.color;
            if (instantiateMaterial)
            {
                __instance.material = new Material(mat);
                __instance.destroyMaterial = true;
            }
            else
            {
                __instance.material = mat;
            }
            __instance.material.renderQueue = (int)RenderQueue.GeometryLast;

            WBOIT.RegisterOverlay(__instance);
           
            return false;
        }
                
    }

    [HarmonyPatch(typeof(WBOIT), nameof(WBOIT.OnRenderImage))]
    public static class RebuildCommandBufferFixer
    {  
        public static bool Prefix(WBOIT __instance, RenderTexture src, RenderTexture dst)
        {
            Mod.logger.LogInfo($"WBOIT.OnRenderImage called");        

            Graphics.Blit(src, dst, __instance.compositeMaterial);
           
            return false;
        }
                
    }
*/
/*
    //Sourced from https://github.com/tommaier123/VRTweaks/blob/master/VRTweaks/WBOITFixes.cs
    //Not sure if this actually does anything
    [HarmonyPatch(typeof(WBOIT))]
    [HarmonyPatch("CreateRenderTargets")]
    internal class CreateRenderTargets_Patch
    {
        public static RenderTexture wboitTexture0;
        public static RenderTexture wboitTexture1;
        public static RenderTexture wboitTexture2;

        public static RenderBuffer[] colorBuffers;

        public static bool Prefix(WBOIT __instance, ref Camera ___camera)
        {
            
            // WBOIT.CreateRenderTargets called 1920 1080 2016 2240
            Mod.logger.LogInfo($"WBOIT.CreateRenderTargets called {__instance.camera.name} {Screen.width} {Screen.height} {__instance.camera.pixelWidth} {__instance.camera.pixelHeight}");
            __instance.wboitTexture1 = DynamicResolution.CreateRenderTexture(__instance.camera.pixelWidth, __instance.camera.pixelHeight, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            __instance.wboitTexture1.name = "WBOIT TexA";
            __instance.wboitTexture2 = DynamicResolution.CreateRenderTexture(__instance.camera.pixelWidth, __instance.camera.pixelHeight, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            __instance.wboitTexture2.name = "WBOIT TexB";
            //UWE.EditorModifications.SetOITTargets(___camera, __instance.wboitTexture1, __instance.wboitTexture2);
            WBOIT.renderTargetIdentifiers[0] = BuiltinRenderTextureType.CameraTarget;
            WBOIT.renderTargetIdentifiers[1] = new RenderTargetIdentifier(__instance.wboitTexture1);
            WBOIT.renderTargetIdentifiers[2] = new RenderTargetIdentifier(__instance.wboitTexture2);
            __instance.compositeMaterial.SetTexture(__instance.texAPropertyID, __instance.wboitTexture1);
            __instance.compositeMaterial.SetTexture(__instance.texBPropertyID, __instance.wboitTexture2);
            __instance.compositeMaterial.renderQueue = 0;
            return true;
        }
    }
*/
/*
    [HarmonyPatch(typeof(WBOIT))]
    [HarmonyPatch(nameof(WBOIT.VerifyRenderTargets))]
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
*/
    #endregion

}
            
/*
            Material compositeMaterial = Traverse.Create(__instance).Field("compositeMaterial").GetValue() as Material;
            Camera camera = Traverse.Create(__instance).Field("camera").GetValue() as Camera;
            int texAPropertyID = (int)Traverse.Create(__instance).Field("texAPropertyID").GetValue();
            int texBPropertyID = (int)Traverse.Create(__instance).Field("texBPropertyID").GetValue();

            wboitTexture0 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            wboitTexture0.name = "WBOIT Tex0";
            Traverse.Create(__instance).Field("wboitTexture0").SetValue(wboitTexture0);

            wboitTexture1 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            wboitTexture1.name = "WBOIT TexA";
            Traverse.Create(__instance).Field("wboitTexture1").SetValue(wboitTexture1);

            wboitTexture2 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);

            wboitTexture2.name = "WBOIT TexB";
            Traverse.Create(__instance).Field("wboitTexture2").SetValue(wboitTexture2);

            compositeMaterial.SetTexture(texAPropertyID, wboitTexture1);

            compositeMaterial.SetTexture(texBPropertyID, wboitTexture2);

            colorBuffers = new RenderBuffer[]
            {
                wboitTexture0.colorBuffer,
                wboitTexture1.colorBuffer,
                wboitTexture2.colorBuffer
            };
            Traverse.Create(__instance).Field("colorBuffers").SetValue(colorBuffers);
            */