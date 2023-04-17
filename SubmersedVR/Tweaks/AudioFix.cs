using HarmonyLib;
using UnityEngine;
using FMODUnity;
// Taken from https://github.com/IWhoI/SubnauticaVREnhancements/blob/master/VREnhancements/AudioFix.cs
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


namespace VREnhancements
{
    class AudioFix
    {
        [HarmonyPatch(typeof(SNCameraRoot), nameof(SNCameraRoot.Awake))]
        class Awake_Patch
        {
            static void Postfix(SNCameraRoot __instance)
            {
                if (SNCameraRoot.main.mainCam)
                {
                    //remove the audio listeners from the PlayerCameras object that does not rotate with the VR headset
                    Object.Destroy(__instance.gameObject.GetComponent<AudioListener>());
                    Object.Destroy(__instance.gameObject.GetComponent<StudioListener>());
                    //add new listener to the main camera that does rotate with the VR headset
                    SNCameraRoot.main.mainCam.gameObject.AddComponent<AudioListener>();
                    SNCameraRoot.main.mainCam.gameObject.AddComponent<StudioListener>();
                }
            }
        }
    }
}
