using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Linq;
using RootMotion.FinalIK;
using System;
using System.Globalization;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;

    public struct TransformOffset
    {
        public TransformOffset(Vector3 pos, Vector3 angles)
        {
            Pos = pos;
            Angles = angles;
        }

        public TransformOffset(Transform transform) : this()
        {
            Pos = transform.localPosition;
            Angles = transform.localEulerAngles;
        }

        public Vector3 Pos { get; }
        public Vector3 Angles { get; }
        public override string ToString() => $"TransformOffset(Pos=({Pos.x:f3}, {Pos.y:f3}, {Pos.z:f3}), Angles=({Angles.x:f3}, {Angles.y:f3}, {Angles.z:f3}))";
        internal string SwitchString(string type)
        {
            FormattableString str = $"case {type} _: return new TransformOffset(new Vector3({Pos.x:f3}f, {Pos.y:f3}f, {Pos.z:f3}f), new Vector3({Angles.x:f3}f, {Angles.y:f3}f, {Angles.z:f3}f));";
            return str.ToString(CultureInfo.InvariantCulture);
        }

        public void Apply(Transform tf)
        {
            tf.localPosition = Pos;
            tf.localEulerAngles = Angles;
        }

    }

    // Define all the Offsets
    static class HandOffsets
    {
        // Default Offset when no tool is Equipped
        public static TransformOffset RightHand = new TransformOffset(new Vector3(0.05f, 0.06f, -0.17f), new Vector3(40.0f, 175.0f, 270.0f));
        public static TransformOffset LeftHand = new TransformOffset(new Vector3(-0.05f, 0.06f, -0.17f), new Vector3(-40.0f, 0.0f, 90.0f));
        public static TransformOffset PDA = new TransformOffset(new Vector3(-0.05f, 0.05f, -0.14f), new Vector3(305.0f, 355.0f, 100.0f));

        internal static TransformOffset GetHandOffset(this PlayerTool tool)
        {
            switch (tool)
            {
                case FireExtinguisher _: return new TransformOffset(new Vector3(0.062f, 0.077f, -0.148f), new Vector3(40.736f, 139.849f, 249.888f));
                case Seaglide _: return new TransformOffset(new Vector3(0.055f, 0.101f, -0.125f), new Vector3(24.986f, 153.649f, 265.740f));
                case Gravsphere _: return new TransformOffset(new Vector3(-0.010f, 0.114f, -0.125f), new Vector3(10.485f, 158.948f, 244.422f));
                case DeployableStorage _: return new TransformOffset(new Vector3(0.017f, 0.099f, -0.135f), new Vector3(27.633f, 159.160f, 251.929f));
                case Constructor _: return new TransformOffset(new Vector3(0.042f, 0.076f, -0.166f), new Vector3(53.635f, 151.667f, 249.508f));
                case LEDLight _: return new TransformOffset(new Vector3(0.051f, 0.113f, -0.122f), new Vector3(20.287f, 157.143f, 262.503f));
                case Knife _: return new TransformOffset(new Vector3(0.008f, 0.095f, -0.115f), new Vector3(17.193f, 162.033f, 250.308f));
                case FlashLight _: return new TransformOffset(new Vector3(0.011f, 0.125f, -0.123f), new Vector3(18.573f, 162.636f, 247.017f));
                case Beacon _: return new TransformOffset(new Vector3(0.006f, 0.137f, -0.165f), new Vector3(31.791f, 151.351f, 242.064f));
                case StasisRifle _: return new TransformOffset(new Vector3(0.013f, 0.091f, -0.155f), new Vector3(32.203f, 147.266f, 237.102f));
                case PropulsionCannonWeapon _: return new TransformOffset(new Vector3(0.001f, 0.078f, -0.169f), new Vector3(36.510f, 148.234f, 231.454f));
                case BuilderTool _: return new TransformOffset(new Vector3(0.042f, 0.090f, -0.129f), new Vector3(30.567f, 155.533f, 258.169f));
                case AirBladder _: return new TransformOffset(new Vector3(-0.032f, 0.090f, -0.133f), new Vector3(7.689f, 145.798f, 224.260f));
                case DiveReel _: return new TransformOffset(new Vector3(-0.019f, 0.096f, -0.119f), new Vector3(14.196f, 148.834f, 238.635f));
                case Welder _: return new TransformOffset(new Vector3(0.002f, 0.110f, -0.140f), new Vector3(23.999f, 153.807f, 241.427f));
                case ScannerTool _: return new TransformOffset(new Vector3(0.006f, 0.109f, -0.149f), new Vector3(25.775f, 145.925f, 236.092f));
                case LaserCutter _: return new TransformOffset(new Vector3(-0.017f, 0.123f, -0.133f), new Vector3(17.726f, 151.261f, 233.612f));
                case Flare _: return new TransformOffset(new Vector3(0.019f, 0.088f, -0.134f), new Vector3(31.170f, 153.374f, 244.228f));
                case RepulsionCannon _: return new TransformOffset(new Vector3(-0.002f, 0.088f, -0.166f), new Vector3(33.777f, 149.093f, 232.610f));
                default: return RightHand;
            };
        }

        internal static TransformOffset GetAimOffset(this PlayerTool tool)
        {
            switch (tool)
            {
                case BuilderTool _: return new TransformOffset(new Vector3(-0.021f, -0.040f, 0.030f), new Vector3(73.312f, 342.306f, 323.270f));
                case ScannerTool _: return new TransformOffset(new Vector3(0.008f, -0.106f, -0.016f), new Vector3(73.923f, 62.973f, 42.123f));
                case FireExtinguisher _: return new TransformOffset(new Vector3(0.003f, -0.101f, -0.054f), new Vector3(64.181f, 10.062f, 346.755f));
                case Seaglide _: return new TransformOffset(new Vector3(0.003f, -0.101f, -0.054f), new Vector3(64.181f, 10.062f, 346.755f));
                case RepulsionCannon _: return new TransformOffset(new Vector3(-0.014f, -0.083f, 0.024f), new Vector3(13.596f, 352.247f, 315.238f));
                case StasisRifle _: return new TransformOffset(new Vector3(-0.037f, -0.073f, 0.039f), new Vector3(44.380f, 349.016f, 309.364f));
                case FlashLight _: return new TransformOffset(new Vector3(-0.029f, -0.024f, 0.074f), new Vector3(74.684f, 27.358f, 341.475f));
                case DiveReel _: return new TransformOffset(new Vector3(0.020f, -0.105f, -0.084f), new Vector3(80.062f, 36.462f, 346.389f));
                case Welder _: return new TransformOffset(new Vector3(-0.028f, -0.017f, 0.056f), new Vector3(59.356f, 350.101f, 303.392f));
                case LaserCutter _: return new TransformOffset(new Vector3(-0.014f, -0.042f, 0.068f), new Vector3(60.953f, 4.267f, 316.030f));
                case HeatBlade _: return new TransformOffset(new Vector3(-0.016f, 0.033f, 0.004f), new Vector3(65.781f, 19.633f, 267.986f));
                case LEDLight _: return new TransformOffset(new Vector3(-0.021f, 0.005f, -0.008f), new Vector3(63.596f, 40.279f, 283.758f));
                default: return VRCameraRig.DefaultTargetTransform;
            };
        }
    }


    class VRHands : MonoBehaviour
    {
        public FullBodyBipedIK ik = null;

        public Transform leftTarget;
        public Transform rightTarget;

        //public Transform leftHand;
        //public Transform rightHand;
        //public Transform leftElbow;
        //public Transform rightElbow;

        //private Vector3 leftElbowOffset;
        //private Vector3 rightElbowOffset;

        public static VRHands instance;

        // private OffsetCalibrationTool calibrationTool;

        public void Setup(FullBodyBipedIK ik)
        {
            instance = this;

            Settings.FullBodyChanged -= OnFullBodyChanged;
            Settings.FullBodyChanged += OnFullBodyChanged;

            this.ik = ik;
            var camRig = VRCameraRig.instance;
            leftTarget = camRig.leftHandTarget.transform;
            rightTarget = camRig.rightHandTarget.transform;
            ResetHandTargets();

            var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            foreach (var bodyRenderer in bodyRenderers)
            {
                Mod.logger.LogInfo($"bodyRenderer {bodyRenderer.name}");
            }

            UpdateBody();
        }

        public static void OnFullBodyChanged(bool val)
        {
            VRUtil.Recenter();
        }

        public void ResetHandTargets()
        {
            HandOffsets.LeftHand.Apply(leftTarget);
            HandOffsets.RightHand.Apply(rightTarget);

        }
        public void OnOpenPDA()
        {
            HandOffsets.PDA.Apply(leftTarget);
        }
        public void OnClosePDA()
        {
            ResetHandTargets();
        }

        void Update()
        {
            if (ik.enabled && uGUI_SpyPenguin.main.activePenguin == false)
            {
                ik.solver.leftHandEffector.target = leftTarget;
                ik.solver.rightHandEffector.target = rightTarget;
            }
        }

        void LateUpdate()
        {
        }

        public void UpdateBody()
        {
           StartCoroutine(UpdateBodyRendering());
        }

        public void SetBodyRendering(bool val)
        {
            var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("body") || r.name.Contains("vest"));
            bodyRenderers.ForEach(r => r.enabled = val);
        }

        // Retries until changes have actually been made
        IEnumerator UpdateBodyRendering()
        {
            bool retry = true;
            while (retry)
            {
                Mod.logger.LogInfo($"UpdateBodyRendering {Settings.FullBody}");

                var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("body") || r.name.Contains("vest"));
                foreach (var bodyRenderer in bodyRenderers)
                {
                    if(bodyRenderer.enabled == Settings.FullBody)
                    {
                        retry = false;
                    }
                    bodyRenderer.enabled = Settings.FullBody;
                }

                yield return new WaitForSeconds(0.5f);
            }

        }

        internal void OnToolEquipped(PlayerTool tool)
        {
            var aimOffset = tool.GetAimOffset();
            VRCameraRig.instance.TargetTransform = aimOffset;
            tool.GetHandOffset().Apply(rightTarget.transform);
        }
    }

    #region Patches

    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Close))]
    class IngameMenu_Update_Body
    {
        public static void Postfix(IngameMenu __instance)
        {
           VRHands.instance.UpdateBody();
        }
    }
 
    // TODO: Move/cleanup this
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
    public class VRPlayerCreate : MonoBehaviour
    {

        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            Mod.logger.LogInfo($"ArmsController.Start called");
            // Disable IK
            __instance.ik.enabled = true;
            __instance.leftAim.aimer.enabled = false;
            __instance.rightAim.aimer.enabled = false;

            // Attach
            __instance.gameObject.AddComponent<VRHands>().Setup(__instance.ik);
            // __instance.pda.ui.canvasScaler.vrMode = uGUI_CanvasScaler.Mode.Inversed;
        }
    }

    // Reconfigure the aiming
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Reconfigure))]
    public static class ChangeAimAngleForTools
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerTool tool)
        {
            VRHands.instance?.OnToolEquipped(tool);
        }
    }

    // This removes the animation that inspects the object/tool when equipped for the first time
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.StartInspectObjectAsync))]
    public static class DontInspectObjectAnimation
    {
        public static bool Prefix(ArmsController __instance)
        {
            return false;
        }
    }

    // This configures the left hands offset for the PDA
    [HarmonyPatch(typeof(PDA), nameof(PDA.Open))]
    public static class SetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnOpenPDA();
        }
    }

    // This resets the PDA configuration for the Hand configuration for the PDA
    [HarmonyPatch(typeof(PDA), nameof(PDA.Close))]
    public static class UnsetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnClosePDA();
        }
    }

    // Aiming related fixes are following now
    // TODO: Consider moving them into their own category/class or namespace
    public static class Aiming
    {
        public static Camera GetAimCamera()
        {
            return VRCameraRig.instance?.laserPointer.eventCamera;
        }
        public static Transform GetAimTransform()
        {
            return VRCameraRig.instance?.laserPointer.transform;
        }
    }

    // This makes it so the Builder tool aims with the laser pointer
    [HarmonyPatch(typeof(Builder), nameof(Builder.GetAimTransform))]
    public static class Builder_GetAimTransform__Patch
    {
        [HarmonyPrefix]
        static bool Prefix(ref Transform __result)
        {
            __result = Aiming.GetAimTransform();
            return false;
        }
    }

    // Make the stasis rifle/or sphere shoot with the hands rotation
    // NOTE: This is easier than patching StasisRifle.Fire() but but might break some mods?
    [HarmonyPatch(typeof(StasisSphere), nameof(StasisSphere.Shoot))]
    public static class OverrideStasisRifleAim
    {
        static bool Prefix(ref Quaternion rotation)
        {
            rotation = Aiming.GetAimTransform().rotation;
            return true;
        }
    }

    // Make the Propulsion canon aim with the laser pointer/target transform
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.GetObjectPosition))]
    public static class OverridePropulsionObjectPosition
    {
        // Replace the first line/instruction in GetObjectPosition() that is Camera camera = MainCamera.camera, with our own above.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.Start().SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Propulsion canon shoot with the laser pointer/target transform
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.OnShoot))]
    public static class OverridePropulsionShootDirection
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Propulsion canon shoot with the laser pointer/target transform
    [HarmonyPatch(typeof(RepulsionCannon), nameof(RepulsionCannon.OnToolUseAnim))]
    public static class OverrideRePropulsionShootDirection
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstructionAndAdvance(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Knife, Fire Extinguisher and Exosuit aim with the laserpointer instead of camera
    [HarmonyPatch(typeof(UWE.Utils), nameof(UWE.Utils.TraceFPSTargetPosition))]
    [HarmonyPatch(new Type[] { typeof(GameObject), typeof(float), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(bool) },
                  new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal })]
    public static class TraceFPSTargetUsingControllers
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstructionAndAdvance(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    #endregion
}
