using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SubmersedVR
{
    /// <summary>
    /// Provides limited compatibility with SnapBuilder by redirecting its aim transform to the VR hand pointer.
    /// </summary>
    internal static class SnapBuilderCompat
    {
        private const string AimTransformTypeName = "Tobey.SnapBuilder.AimTransform";

        private static Harmony harmonyInstance;
        private static bool isPatched;
        private static bool assemblyLoadHooked;

        public static void TryInit(Harmony harmony)
        {
            harmonyInstance = harmony;

            if (isPatched || TryPatch())
            {
                return;
            }

            if (!assemblyLoadHooked)
            {
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
                assemblyLoadHooked = true;
            }

            Mod.logger?.LogInfo("SnapBuilder not detected yet; compatibility patch will activate once it is loaded.");
        }

        private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetType(AimTransformTypeName, throwOnError: false) == null)
            {
                return;
            }

            TryPatch();
        }

        private static bool TryPatch()
        {
            if (isPatched || harmonyInstance == null)
            {
                return isPatched;
            }

            Type aimTransformType = GetAimTransformType();
            if (aimTransformType == null)
            {
                return false;
            }

            MethodInfo builderGetter = AccessTools.PropertyGetter(aimTransformType, "BuilderAimTransform");
            if (builderGetter == null)
            {
                Mod.logger?.LogWarning("SnapBuilder detected but AimTransform.BuilderAimTransform getter was not found. Compatibility disabled.");
                return false;
            }

            harmonyInstance.Patch(builderGetter, postfix: new HarmonyMethod(typeof(SnapBuilderCompat), nameof(BuilderAimTransformPostfix)));

            MethodInfo offsetGetter = AccessTools.PropertyGetter(aimTransformType, "OffsetAimTransform");
            if (offsetGetter != null)
            {
                harmonyInstance.Patch(offsetGetter, prefix: new HarmonyMethod(typeof(SnapBuilderCompat), nameof(OffsetAimTransformPrefix)));
            }
            else
            {
                Mod.logger?.LogWarning("SnapBuilder detected but AimTransform.OffsetAimTransform getter was not found; VR snapping may jitter.");
            }

            isPatched = true;

            if (assemblyLoadHooked)
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
                assemblyLoadHooked = false;
            }

            Mod.logger?.LogInfo("SnapBuilder detected. Redirecting aim transform to VR controller.");
            return true;
        }

        private static Type GetAimTransformType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(AimTransformTypeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Transform GetVRPointer()
        {
            return Aiming.GetAimTransform();
        }

        private static void BuilderAimTransformPostfix(ref Transform __result)
        {
            Transform aimTransform = GetVRPointer();
            if (aimTransform != null)
            {
                __result = aimTransform;
            }
        }

        private static bool OffsetAimTransformPrefix(ref Transform __result)
        {
            Transform aimTransform = GetVRPointer();
            if (aimTransform == null)
            {
                return true;
            }

            // VR pointer does not bob with the head, so reusing it as the offset source is sufficient.
            __result = aimTransform;
            return false;
        }
    }
}
