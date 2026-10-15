using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.XR;
using System.Linq;

namespace SubmersedVR
{
    // Main plguin/entry point of the mod
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(VREnhancements.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SN1MC.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Mod : BaseUnityPlugin
    {
        static class VREnhancements
        {
            public const string GUID = "com.whotnt.subnautica.vrenhancements.mod";
            public const string NAME = "VREnhancements";
        }
        static class SN1MC
        {
            public const string GUID = "mod.ihatetn931.subnautica.motioncontrols";
            public const string NAME = "Motion Controls Subnautica";
        }

        public static ManualLogSource logger;
        public static Mod instance;
        // Set on application quit (the Application.quitting stub is an
        // event, not the runtime property)
        public static volatile bool quitting;

        private bool IsSetupValid()
        {
            bool valid = true;
            if (Chainloader.PluginInfos.Any(plugin => plugin.Value.Metadata.GUID == VREnhancements.GUID))
            {
                Logger.LogError($"Found loaded {VREnhancements.NAME} mod. Please remove it if you want to use {PluginInfo.PLUGIN_NAME}. They are not compatible.");
                valid = false;
            }
            if (Chainloader.PluginInfos.Any(plugin => plugin.Value.Metadata.GUID == SN1MC.GUID))
            {
                Logger.LogError($"Found old {SN1MC.NAME} mod. Please remove it if you want to use {PluginInfo.PLUGIN_NAME}. They are not compatible.");
                valid = false;
            }
            return valid;
        }

        private void Awake()
        {
            if (!IsSetupValid())
            {
                return;
            }

            logger = Logger;
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded and patches applied!");
            VROptions.gazeBasedCursor = true;

            instance = this;
        }

        // Traces the mod's shutdown in the log (diagnose the quit hang)
        void OnApplicationQuit()
        {
            quitting = true;
            if (logger != null)
            {
                logger.LogInfo($"{PluginInfo.PLUGIN_NAME} OnApplicationQuit");
            }
        }
    }
}
