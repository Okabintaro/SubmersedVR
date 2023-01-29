using UnityEngine.UI;
using HarmonyLib;
using TMPro;

// Taken from https://github.com/IWhoI/SubnauticaVREnhancements/blob/8a7bccb3c88292f245d456af4f23acfb79e07c7f/VREnhancements/AdditionalVROptions.cs
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

//Adds Recenter VR button to the in game menu.
[HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Awake))]
static class IGM_Awake_Patch
{
    private static Button recenterVRButton;
    //code copied from the quit to desktop mod and modified
    static void Postfix(IngameMenu __instance)
    {
        if (__instance && recenterVRButton == null)
        {
            //Clone the quitToMainMenuButton and update it
            Button menuButton = __instance.quitToMainMenuButton.transform.parent.GetChild(0).gameObject.GetComponent<Button>();
            recenterVRButton = UnityEngine.Object.Instantiate<Button>(menuButton, __instance.quitToMainMenuButton.transform.parent);
            recenterVRButton.transform.SetSiblingIndex(1);//put the button in the second position in the menu
            recenterVRButton.name = "RecenterVR";
            recenterVRButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Recenter VR");
            recenterVRButton.onClick.RemoveAllListeners();//remove cloned listeners
            //add new listener
            recenterVRButton.onClick.AddListener(delegate ()
            {
                VRUtil.Recenter();
            });
            
        }
    }
}
