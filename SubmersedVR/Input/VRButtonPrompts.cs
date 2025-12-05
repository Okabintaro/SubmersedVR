using System;
using HarmonyLib;

namespace SubmersedVR
{
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;

    /// <summary>
    /// Patches GameInput.FormatButton to return SteamVR localized button names
    /// instead of generic gamepad button names.
    /// This makes button prompts show the actual bound VR controller buttons.
    /// </summary>
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.FormatButton))]
    public static class VRButtonPrompts
    {
        /// <summary>
        /// Gets the localized button name from SteamVR for the given action.
        /// Returns null if no SteamVR action is mapped or if SteamVR isn't ready.
        /// </summary>
        private static string GetLocalizedButtonName(GameInput.Button button)
        {
            // Get the action by name - SteamVR actions match GameInput.Button names
            string actionName = button.ToString();
            var action = SteamVR_Input.GetBooleanAction("subnautica", actionName);
            
            if (action == null || !action.activeBinding)
                return null;

            // GetLocalizedOriginPart returns the human-readable button name
            var localizedName = action.GetLocalizedOriginPart(
                SteamVR_Input_Sources.Any,
                EVRInputStringBits.VRInputString_InputSource
            );

            if (!string.IsNullOrEmpty(localizedName))
            {
                return $"[{localizedName}]";
            }

            return null;
        }

        static bool Prefix(GameInput.Button action, bool allBindingSets, ref string __result)
        {
            try
            {
                // Only intercept when SteamVR is ready
                if (!SteamVrGameInput.IsSteamVrReady)
                    return true;

                var vrButtonName = GetLocalizedButtonName(action);
                if (vrButtonName != null)
                {
                    __result = vrButtonName;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Mod.logger.LogError($"VRButtonPrompts error for {action}: {ex.Message}");
            }

            return true;
        }
    }
}
