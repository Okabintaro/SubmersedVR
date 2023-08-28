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
            GameObject progressBar = __instance.loadingBackground.transform.Find("LoadingBarCanvas").gameObject;
            if (loadingArtwork != null)
            {
                //resize the background image
                loadingArtwork.GetComponent<RectTransform>().localScale = new Vector3(0.28f, 0.28f, 0.28f);
            }
            if (progressBar != null)
            {
                //adjust the loading bar
                RectTransform logoRect = progressBar.GetComponent<RectTransform>();
                logoRect.localScale = new Vector3(0.6f, 0.6f, 0.6f);//Vector3.one;
                logoRect.anchoredPosition = new Vector2(logoRect.rect.width / 5.0f, 200.0f);
            }
        }
    }

    [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.Update))]
    static class LockInputWhileLoading
    {
        static void Postfix(WaitScreen __instance)
        {
            SteamVrGameInput.InputLocked = __instance.isShown;
        }
    }

    //We modified the screen scale to be smaller in order to display all UI items but we want
    //this screen to cover the entire UI so it needs to be scaled up
    [HarmonyPatch(typeof(WaitScreen), nameof(WaitScreen.Awake))]
    public static class WaitScreen_FixVRScale
    {
        [HarmonyPostfix]
        public static void Postfix(WaitScreen __instance)
        {
            uGUI.main.loading.transform.localScale = new Vector3(3.4f, 3.4f, 3.4f);
        }
    }

}