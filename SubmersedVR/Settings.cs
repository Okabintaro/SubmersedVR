using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    public class Settings
    {
		public delegate void BooleanChanged(bool newValue);

        public static bool IsDebugEnabled;
		public static event BooleanChanged IsDebugChanged;

        public static bool AlwaysShowControllers;
		public static event BooleanChanged AlwaysShowControllersChanged;

        public static bool AlwaysShowLaserPointer;
		public static event BooleanChanged AlwaysShowLaserPointerChanged;

		// Saves or loads all public static properties as settings using the given serializer
        internal static void Serialize(GameSettings.ISerializer serializer)
        {
            string ns = nameof(SubmersedVR);
            foreach (var p in typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                string name = p.Name;
                var value = p.GetValue(null);
                switch (value)
                {
                    case bool val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case int val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case float val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case string val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case Color32 val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    default:
                        Mod.logger.LogError($"Can't save/load setting {name} with type {value.GetType()}");
                        break;
                }
            }
        }

		internal static void AddMenu(uGUI_OptionsPanel panel) {
			int tab = panel.AddTab("Submersed VR");
			panel.AddHeading(tab, "Debug Options");
			panel.AddToggleOption(tab, "Debug Mode", IsDebugEnabled, (value) => {IsDebugEnabled = value; IsDebugChanged(value);}, "Enables Debug Overlays and Logs.");
			panel.AddToggleOption(tab, "Always Show Controllers", AlwaysShowControllers, (value) => {AlwaysShowControllers = value; AlwaysShowControllersChanged(value);}, "Shows the controllers at all times.");
			panel.AddToggleOption(tab, "Always Show Laserpointer", AlwaysShowLaserPointer, (value) => {AlwaysShowLaserPointer = value; AlwaysShowLaserPointerChanged(value);}, "Show the laserpointer at all times.");
		}
    }

    #region Patches

	// This enables the mod to save and load settings, by serializing our settings from the class above.
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeSettings))]
    static class SerializeModSettings
    {
        public static void Postfix(GameSettings.ISerializer serializer)
        {
            Settings.Serialize(serializer);
        }
    }

	// This hooks into the tab creation to create the options menu.
	[HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
    static class CreateOptionsTab
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
			Settings.AddMenu(__instance);
        }
    }

    #endregion
}
