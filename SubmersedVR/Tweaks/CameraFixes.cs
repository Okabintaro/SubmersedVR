using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

/*
This is a subset of the fixes from IWhoIs SubnauticaVREnhancements mod.
https://github.com/IWhoI/SubnauticaVREnhancements/blob/master/VREnhancements/CameraFixes.cs#L161

MIT License

Copyright (c) 2023 IWhoI

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace SubmersedVR
{

    [HarmonyPatch(typeof(WaterSunShaftsOnCamera), nameof(WaterSunShaftsOnCamera.Awake))]
    class SunShafts_Awake_Patch
    {
        static void Postfix(WaterSunShaftsOnCamera __instance)
        {
            __instance.reduction = 6;//default is 2. console is 4. This improves performance with little noticable difference to the sun shafts.
        }

    }
    //This fixes the black screen issue. The render targets are being created with a width and height determined by XRSettings.eyeTextureDesc
    //but VerifyRenderTargets was using Screen.width and Screen.height which only works for monitors.
    [HarmonyPatch(typeof(WBOIT), nameof(WBOIT.VerifyRenderTargets))]
    class WBOIT_VerifyRenderTargets_Patch
    {
        static bool Prefix(WBOIT __instance)
        {
            RenderTexture wboitTex1 = Traverse.Create(__instance).Field("wboitTexture1").GetValue<RenderTexture>();
            //use the VR eyetexture dimensions instead of Screen.width and height
            if (wboitTex1 != null && (XRSettings.eyeTextureWidth != wboitTex1.width || XRSettings.eyeTextureHeight != wboitTex1.height))
            {
                Traverse.Create(__instance).Method("DestroyRenderTargets").GetValue();
            }
            if (wboitTex1 == null)
            {
                Traverse.Create(__instance).Method("CreateRenderTargets").GetValue();
            }
            return false;//don't run the original method
        }

    }

}