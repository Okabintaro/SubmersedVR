using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace SubmersedVR
{
    /// <summary>
    /// Forces SnapBuilder fine snapping / rotation toggles whenever VR build tools are equipped.
    /// </summary>
    internal static class SnapBuilderControls
    {
        private static Harmony harmonyInstance;
        private static bool onEnableHooked;
        private static bool toggleOverrideActive;

        private static object fineSnapToggle;
        private static object fineRotateToggle;
        private static MethodInfo toggleIsEnabledGetter;
        private static bool toggleGetterPatched;

        private static readonly object sync = new object();

        public static void TryAugmentFineControls(Harmony harmony, Type snapBuilderType)
        {
            if (harmony == null || snapBuilderType == null)
            {
                return;
            }

            lock (sync)
            {
                if (onEnableHooked)
                {
                    return;
                }
                harmonyInstance = harmony;
                MethodInfo onEnable = Method(snapBuilderType, "OnEnable");
                if (onEnable == null)
                {
                    Mod.logger?.LogWarning("SnapBuilder.OnEnable not found; cannot auto-enable fine controls.");
                    return;
                }

                harmony.Patch(onEnable, postfix: new HarmonyMethod(typeof(SnapBuilderControls), nameof(SnapBuilderOnEnablePostfix)));
                onEnableHooked = true;
                Mod.logger?.LogInfo("SnapBuilder fine control override initialized; waiting for SnapBuilder to enable.");
            }
        }

        private static void SnapBuilderOnEnablePostfix()
        {
            lock (sync)
            {
                if (toggleOverrideActive)
                {
                    return;
                }

                try
                {
                    Type togglesType = TypeByName("Tobey.SnapBuilder.Config+Toggles");
                    Type toggleType = TypeByName("Tobey.SnapBuilder.Toggle");
                    if (togglesType == null || toggleType == null)
                    {
                        return;
                    }

                    MethodInfo fineSnapGetter = PropertyGetter(togglesType, "FineSnapping");
                    MethodInfo fineRotateGetter = PropertyGetter(togglesType, "FineRotation");
                    if (fineSnapGetter == null && fineRotateGetter == null)
                    {
                        return;
                    }

                    fineSnapToggle = fineSnapGetter?.Invoke(null, null);
                    fineRotateToggle = fineRotateGetter?.Invoke(null, null);
                    if (fineSnapToggle == null && fineRotateToggle == null)
                    {
                        return;
                    }

                    toggleIsEnabledGetter = toggleIsEnabledGetter ?? PropertyGetter(toggleType, "IsEnabled");
                    if (toggleIsEnabledGetter == null || harmonyInstance == null)
                    {
                        return;
                    }

                    if (!toggleGetterPatched)
                    {
                        harmonyInstance.Patch(toggleIsEnabledGetter, postfix: new HarmonyMethod(typeof(SnapBuilderControls), nameof(ForceFineTogglePostfix)));
                        toggleGetterPatched = true;
                    }

                    toggleOverrideActive = true;
                    Mod.logger?.LogInfo("SnapBuilder fine snapping/rotation will remain enabled while VR build tools are equipped.");
                }
                catch (Exception ex)
                {
                    Mod.logger?.LogWarning($"Failed to finalize SnapBuilder fine-control override: {ex}");
                }
            }
        }

        private static void ForceFineTogglePostfix(object __instance, ref bool __result)
        {
            if (!toggleOverrideActive || !HasBuildToolEquipped())
            {
                return;
            }   

            if (__instance == fineSnapToggle || __instance == fineRotateToggle)
            {
                __result = true;
            }
        }

        private static bool HasBuildToolEquipped()
        {
            var held = Inventory.main?.GetHeld();
            if (held == null)
            {
                return false;
            }

            return held.GetComponent<BuilderTool>() != null
                || held.GetComponent<PlaceTool>() != null;
        }
    }
}
