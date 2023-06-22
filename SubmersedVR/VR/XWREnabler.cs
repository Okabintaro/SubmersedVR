/**
Enalbe the XR Support again, inspired by.
https://github.com/tommaier123/VRTweaks/blob/master/UWEXRActivation/ModdedClasses/XRSettings.cs
Thanks!
*/

using HarmonyLib;
using UnityEngine;
using UWEXR;
using UXR = UnityEngine.XR;

[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.enabled), MethodType.Getter)]
class XRSettingsEnabled
{
    public static bool Prefix(ref bool __result)
    {
        __result = UXR.XRSettings.enabled;
        // __result = false;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.isDeviceActive), MethodType.Getter)]
class XRSettingsIsDeviceActive
{
    public static bool Prefix(ref bool __result)
    {
        __result = UXR.XRSettings.isDeviceActive;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.showDeviceView), MethodType.Getter)]
class XRSettingsShowDeviceView
{
    public static bool Prefix(ref bool __result)
    {
        __result = UXR.XRSettings.showDeviceView;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.renderScale), MethodType.Getter)]
class XRSettingsRenderScale
{
    [System.Obsolete]
    public static bool Prefix(ref float __result)
    {
        __result = UXR.XRSettings.renderScale;
        return false;
    }
}

[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.renderScale), MethodType.Setter)]
class XRSettingsSetRenderScale
{
    [System.Obsolete]
    public static bool Prefix(float value)
    {
        UXR.XRSettings.renderScale = value;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.eyeTextureResolutionScale), MethodType.Getter)]
class XRSettingsEyeResoltuionScale
{
    public static bool Prefix(ref float __result)
    {
        __result = UXR.XRSettings.eyeTextureResolutionScale;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.eyeTextureResolutionScale), MethodType.Setter)]
class XRSettingsSetEyeResoltuionScale
{
    public static bool Prefix(float value)
    {
        UXR.XRSettings.eyeTextureResolutionScale = value;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.eyeTextureWidth), MethodType.Getter)]
class XRSettingsGetEyeTextureWidth
{
    public static bool Prefix(ref int __result)
    {
        __result = UXR.XRSettings.eyeTextureWidth;
        return false;
    }
}


[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.eyeTextureHeight), MethodType.Getter)]
class XRSettingsGetEyeTextureHeight
{
    public static bool Prefix(ref int __result)
    {
        __result = UXR.XRSettings.eyeTextureHeight;
        return false;
    }
}

[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.eyeTextureDesc), MethodType.Getter)]
class XRSettingsGetEyeTextureDes
{
    public static bool Prefix(ref RenderTextureDescriptor __result)
    {
        __result = UXR.XRSettings.eyeTextureDesc;
        return false;
    }
}

[HarmonyPatch(typeof(XRSettings))]
[HarmonyPatch(nameof(XRSettings.loadedDeviceName), MethodType.Getter)]
class XRSettingsLoadedDeviceName
{
    public static bool Prefix(ref string __result)
    {
        __result = UXR.XRSettings.loadedDeviceName;
        return false;
    }
}



