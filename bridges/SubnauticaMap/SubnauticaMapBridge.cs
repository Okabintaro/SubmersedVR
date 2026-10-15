using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.XR;

namespace SubnauticaMapBridge
{
    // VR input bridge for the SubnauticaMap mod (Nexus 12, closed source).
    // Relies on SubmersedVR for the laser pointer / VR PDA, talks to the map mod through reflection only.
    [BepInPlugin("SubnauticaMapBridge", "SubnauticaMap VR Bridge", "0.3.8")]
    [BepInDependency("SubmersedVR")]
    public class Mod : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static Harmony harmony;

        private void Awake()
        {
            logger = Logger;
            if (XRSettings.loadedDeviceName != "OpenVR")
            {
                Logger.LogInfo("Not running in OpenVR mode, bridge disabled.");
                return;
            }

            harmony = new Harmony("SubnauticaMapBridge");
            harmony.PatchAll();

            if (MapMod.Detect())
            {
                Logger.LogInfo("SubnauticaMap detected, VR map controls active.");
            }
            else
            {
                Logger.LogInfo("SubnauticaMap not detected, will retry at scene load. Bridge idle.");
            }
        }
    }
}
