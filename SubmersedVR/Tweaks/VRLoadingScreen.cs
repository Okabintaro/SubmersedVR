using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;



namespace SubmersedVR
{
    // Taken from https://github.com/IWhoI/SubnauticaVREnhancements/blob/master/VREnhancements/UIElementsFixes.cs#L271-L299
    /*
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
    [HarmonyPatch(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.Awake))]
    static class LoadingScreen_Patch
    {
        static void Postfix(uGUI_SceneLoading __instance)
        {
            Image loadingArtwork = __instance.loadingBackground.transform.Find("LoadingArtwork").GetComponent<Image>();
            Vector2 midCenter = new Vector2(0.5f, 0.5f);
            uGUI_Logo logo = __instance.loadingBackground.GetComponentInChildren<uGUI_Logo>();
            if (loadingArtwork != null)
            {
                //remove background image and set background to black
                loadingArtwork.sprite = null;
                loadingArtwork.color = Color.black;
                loadingArtwork.GetComponent<RectTransform>().localScale = Vector3.one;
            }
            if (logo != null)
            {
                //center the logo and loading bar
                RectTransform logoRect = logo.GetComponent<RectTransform>();
                logoRect.anchoredPosition = new Vector2(0, 120f);
                logoRect.anchorMax = logoRect.anchorMin = midCenter;
                RectTransform parentCanvasRect = logo.transform.parent.GetComponent<RectTransform>();
                parentCanvasRect.anchoredPosition = new Vector2(0, -25f);
                parentCanvasRect.anchorMin = Vector2.zero;
                parentCanvasRect.anchorMax = Vector2.one;
            }
        }
    }

    [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.Update))]
    static class LockInputWhileLoading
    {
        static void Postfix(WaitScreen __instance)
        {
            SteamVrGameInput.InputLocked = __instance.isWaiting;
        }
    }
}