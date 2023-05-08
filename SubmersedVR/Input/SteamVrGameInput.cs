using System;
using HarmonyLib;
using UnityEngine;

#pragma warning disable Harmony003
namespace SubmersedVR
{
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;
    using System.Reflection;

    #region Patches
    static class SteamVrGameInput
    {
        public static bool InputLocked = false;
        public static bool IsSteamVrReady = false;
        public static bool SnapTurned = false;

        public static bool ShouldIgnore(GameInput.Button button)
        {
            return !IsSteamVrReady || InputLocked
                || button == GameInput.Button.Slot1
                || button == GameInput.Button.Slot2
                || button == GameInput.Button.Slot3
                || button == GameInput.Button.Slot4
                || button == GameInput.Button.Slot5
                || button == GameInput.Button.AutoMove;
        }

        public static Vector2 GetScrollDelta()
        {
            if (!IsSteamVrReady || InputLocked)
            {
                return Vector2.zero;
            }
            return SteamVR_Actions.subnautica.UIScroll.GetAxis(SteamVR_Input_Sources.Any);
        }
    }

    // The following three patches map the steamvr actions to the button states
    // TODO: They could be optimized by using a switch instead of the GetStateDown(string) lookup
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetButtonDown))]
    public static class SteamVrGetButtonDown
    {
        static bool Prefix(GameInput.Button button, ref bool __result)
        {
            if (SteamVrGameInput.ShouldIgnore(button))
            {
                return false;
            }

            __result = SteamVR_Input.GetStateDown(button.ToString(), SteamVR_Input_Sources.Any);
            return false;
        }
    }

    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetButtonUp))]
    public static class SteamVrGetButtonUp
    {
        static bool Prefix(GameInput.Button button, ref bool __result)
        {
            if (SteamVrGameInput.ShouldIgnore(button))
            {
                return false;
            }

            __result = SteamVR_Input.GetStateUp(button.ToString(), SteamVR_Input_Sources.Any);
            return false;
        }
    }

    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetButtonHeld))]
    public static class SteamVrGetButtonHeld
    {
        static bool Prefix(GameInput.Button button, ref bool __result)
        {
            if (SteamVrGameInput.ShouldIgnore(button))
            {
                return false;
            }

            __result = SteamVR_Input.GetState(button.ToString(), SteamVR_Input_Sources.Any);
            return false;
        }
    }

    // Make the game believe to be controllerd by controllers only
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateAvailableDevices))]
    public static class ControllerOnly
    {
        public static bool Prefix()
        {
            // Choose XBox, since it has the ABXY from Quest controllers
            GameInput.chosenControllerLayout = GameInput.ControllerLayout.Xbox360;
            GameInput.lastDevice = GameInput.Device.Controller;
            return false;
        }
    }

    // Implement Snap turning for the player
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetLookDelta))]
    public static class SnapTurning
    {
        public static void Postfix(ref Vector2 __result)
        {
            bool isInVehicle = Player.main?.currentMountedVehicle != null;
            if (Settings.IsSnapTurningEnabled && !isInVehicle) {
                float lookX = __result.x;
                float absX = Mathf.Abs(lookX);
                float threshold = 0.5f;
                if (absX > threshold && !SteamVrGameInput.SnapTurned) {
                    __result.x = Settings.SnapTurningAngle * Mathf.Sign(lookX);
                    SteamVrGameInput.SnapTurned = true;
                } else  {
                    __result.x = 0;
                    if (absX <= threshold) {
                        SteamVrGameInput.SnapTurned = false;
                    }
                }
            }
        }
    }

    // Pretend the controlers are always available to make Subnautica not switch to Keyboard/Mouse and mess VR controls up
    // TODO: This should/could probably be changed though, asking SteamVR if controllers are available?
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateControllerAvailable))]
    public static class ControllerAlwaysAvailable
    {
        public static bool Prefix()
        {
            GameInput.controllerAvailable = true;
            return false;
        }
    }
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateKeyboardAvailable))]
    public static class KeyboardNeverAvialable
    {
        public static bool Prefix()
        {
            GameInput.keyboardAvailable = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetAnalogValueForButton))]
    public static class SteamVrGetAnalogValue
    {
        public static bool Prefix(GameInput.Button button, ref float __result)
        {
            if (SteamVrGameInput.InputLocked || !SteamVrGameInput.IsSteamVrReady)
            {
                __result = 0.0f;
                return false;
            }
            Vector2 vec;
            bool isPressed = false;
            float value = 0.0f;
            switch (button)
            {
                case GameInput.Button.MoveForward:
                    vec = SteamVR_Actions.subnautica.Move.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.y > 0.0f ? vec.y : 0.0f;
                    break;
                case GameInput.Button.MoveBackward:
                    vec = SteamVR_Actions.subnautica.Move.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.y < 0.0f ? -vec.y : 0.0f;
                    break;
                case GameInput.Button.MoveRight:
                    vec = SteamVR_Actions.subnautica.Move.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.x > 0.0f ? vec.x : 0.0f;
                    break;
                case GameInput.Button.MoveLeft:
                    vec = SteamVR_Actions.subnautica.Move.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.x < 0.0f ? -vec.x : 0.0f;
                    break;
                case GameInput.Button.MoveUp:
                    isPressed = SteamVR_Actions.subnautica.MoveUp.GetState(SteamVR_Input_Sources.Any);
                    value = isPressed ? 1.0f : 0.0f;
                    break;
                case GameInput.Button.MoveDown:
                    isPressed = SteamVR_Actions.subnautica.MoveDown.GetState(SteamVR_Input_Sources.Any);
                    value = isPressed ? 1.0f : 0.0f;
                    break;
                case GameInput.Button.LookUp:
                    vec = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
                    if (Settings.InvertYAxis)
                    {
                        value = vec.y < 0.0f ? -vec.y : 0.0f;
                    }
                    else
                    {
                        value = vec.y > 0.0f ? vec.y : 0.0f;
                    }
                    break;
                case GameInput.Button.LookDown:
                    vec = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
                    if (Settings.InvertYAxis)
                    {
                        value = vec.y > 0.0f ? vec.y : 0.0f;
                    }
                    else {
                        value = vec.y < 0.0f ? -vec.y : 0.0f;
                    }
                    break;
                case GameInput.Button.LookRight:
                    vec = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.x > 0.0f ? vec.x : 0.0f;
                    break;
                case GameInput.Button.LookLeft:
                    vec = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.x < 0.0f ? -vec.x : 0.0f;
                    break;
            }

            __result = Mathf.Clamp(value, -1.0f, 1.0f);

            return false;
        }
    }


    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateAxisValues))]
    public static class SteamVrDontUpdateAxisValues
    {
        static bool Prefix(GameInput __instance, bool useKeyboard, bool useController)
        {
            if (Settings.IsDebugEnabled)
            {
                // DebugPanel.Show($"{GameInput.axisValues[0]}, {GameInput.axisValues[1]}, {GameInput.axisValues[2]}, {GameInput.axisValues[3]}, {GameInput.axisValues[4]}, {GameInput.axisValues[5]}\nAvailable: {GameInput.controllerAvailable} -> Primary: {GameInput.GetPrimaryDevice()} IsGamePad: {GameInput.IsPrimaryDeviceGamepad()}");
                // DebugPanel.Show($"{GameInput.GetMoveDirection()}");
                // var sd = SteamVrGameInput.GetScrollDelta();
                // DebugPanel.Show($"Scroll: {s}, ScrollDelta: {sd}");
            }
            return false;
        }
    }

    // This makes GameInput.AnyKeyDown() return true incase any boolean action is pressed. Is needed for the intro skip and credits.
    // But hmm, where is the any key on the controllers? (https://www.youtube.com/watch?v=st6-DgWeuos)
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.AnyKeyDown))]
    public static class SteamVRPressAnyKey
    {
        static void Postfix(GameInput __instance, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            foreach (var action in SteamVR_Input.actionsBoolean)
            {
                if (action.GetStateDown(SteamVR_Input_Sources.Any))
                {
                    __result = true;
                    break;
                }
            }
        }
    }

    // This makes it so the crafting menu from the fabricators actually use the controller buttons
    // [HarmonyPatch(typeof(uGUI_CraftingMenu), "OnPointerClick")]
    [HarmonyPatch(typeof(uGUI_CraftingMenu))]
    public static class CraftingMenuUseControllerButtons
    {
        public static MethodBase TargetMethod()
        {
            var type = typeof(uGUI_CraftingMenu);
            return AccessTools.FirstMethod(type, method => method.Name.Contains("OnPointerClick"));
        }

        static bool Prefix(ref bool __result, uGUI_CraftingMenu __instance, uGUI_ItemIcon icon, int button)
        {
            if (__instance.interactable)
            {
                uGUI_CraftingMenu.Node node = __instance.GetNode(icon);
                switch (button)
                {
                    case 0: // uGUI.button0 => UISubmit
                        __instance.Action(node);
                        __result = true;
                        break;
                    case 1: // uGUI.button1 => UICancel
                        __instance.Deselect();
                        __result = true;
                        break;
                    case 2: // uGUI.button2 => UIClear => Pinning
                        if (node.action == TreeAction.Craft)
                        {
                            TechType techType = node.techType;
                            if (CrafterLogic.IsCraftRecipeUnlocked(techType))
                            {
                                PinManager.TogglePin(techType);
                            }
                        }
                        __result = true;
                        break;
                    default:
                        __result = false;
                        break;
                }
            }
            return false;
        }
    }

    // Make the builder gun rotation use custom steamvr actions
    [HarmonyPatch(typeof(Builder), nameof(Builder.CalculateAdditiveRotationFromInput))]
    public static class BuilderRotateUseCustomActions
    {
        static bool Prefix(float additiveRotation, ref float __result)
        {
            if (SteamVR_Actions.subnautica_BuilderRotateRight.GetState(SteamVR_Input_Sources.Any))
            {
                additiveRotation = MathExtensions.RepeatAngle(additiveRotation - Builder.GetDeltaTimeForAdditiveRotation() * Builder.additiveRotationSpeed);
            }
            else if (SteamVR_Actions.subnautica_BuilderRotateLeft.GetState(SteamVR_Input_Sources.Any))
            {
                additiveRotation = MathExtensions.RepeatAngle(additiveRotation + Builder.GetDeltaTimeForAdditiveRotation() * Builder.additiveRotationSpeed);
            }
            __result = additiveRotation;
            return false;
        }
    }

    // Rotate base pieces using custom steamvr actions
    [HarmonyPatch(typeof(Builder), nameof(Builder.UpdateRotation))]
    public static class BuilderUpdateRotationUseCustomActions
    {
        static bool Prefix(int max, ref bool __result)
        {
            if (SteamVR_Actions.subnautica_BuilderRotateRight.GetStateDown(SteamVR_Input_Sources.Any))
            {
                Builder.lastRotation = (Builder.lastRotation + max - 1) % max;
                __result = true;
                return false;
            }
            if (SteamVR_Actions.subnautica_BuilderRotateLeft.GetStateDown(SteamVR_Input_Sources.Any))
            {
                Builder.lastRotation = (Builder.lastRotation + 1) % max;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }


    // Force the gaze based cursor, since we use it for the laserpointer
    [HarmonyPatch(typeof(VROptions), nameof(VROptions.GetUseGazeBasedCursor))]
    public static class ForceGazeBasedCursor
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    // Use Action vector as scroll delta to enable scrolling in the UI
    [HarmonyPatch(typeof(Input), nameof(Input.mouseScrollDelta), MethodType.Getter)]
    static class EmulateUnityScrollDelta
    {
        static bool Prefix(ref Vector2 __result)
        {
            __result = SteamVrGameInput.GetScrollDelta();
            return false;
        }
    }

#if false 
    // Use the ui camera for tooltip scaling instead of controller event camera
    [HarmonyPatch(typeof(uGUI_Tooltip), nameof(uGUI_Tooltip.ExtractParams))]
    static class UseCameraForTooltapScaling
    {
        public static Camera GetUiCamera()
        {
            return VRCameraRig.instance.uiCamera;
        }

        public static void Postfix(uGUI_Tooltip __instance, ref bool __result) {
            if (__result) {
                Transform tf = GetUiCamera().transform;
                __instance.aimingPosition = tf.position;
                __instance.aimingForward = tf.forward;
            }
        }

    }

    // Use the ui camera for tooltip scaling instead of controller event camera
    [HarmonyPatch(typeof(uGUI_Tooltip), nameof(uGUI_Tooltip.UpdatePosition))]
    static class ScaleDownTooltip
    {
        public static PDA pda;
        public const float PDA_ScaleFactor = 0.5f;

        public static float GetTooltipScaler()
        {
            if (pda == null) {
                pda = Player.main.GetPDA();
            }
            return pda.isInUse ? PDA_ScaleFactor : 1.0f;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] {
                new CodeMatch(OpCodes.Stloc_0)
            }).Insert(new CodeInstruction[] {
                CodeInstruction.Call(typeof(ScaleDownTooltip), nameof(ScaleDownTooltip.GetTooltipScaler)),
                new CodeInstruction(OpCodes.Mul),
            });
            return m.InstructionEnumeration();
        }
    }
#endif

    // Don't scale the tooltips with the controller distance
    [HarmonyPatch(typeof(uGUI_Tooltip), nameof(uGUI_Tooltip.UpdatePosition))]
    [HarmonyDebug]
    static class DontScaleToolTips
    {
        public static PDA pda;
        public const float PDA_ScaleFactor = 0.25f;

        public static float GetTooltipScaler()
        {
            if (pda == null)
            {
                pda = Player.main.GetPDA();
            }
            return pda.isInUse ? PDA_ScaleFactor : 1.0f;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            var pos = m.MatchForward(false, new CodeMatch[] {
                // new CodeMatch(OpCodes.Stloc_0)
                new CodeMatch(ci => ci.Calls(AccessTools.DeclaredMethod(typeof(Vector3), nameof(Vector3.Dot))))
            }).Pos;
            m.Start().RemoveInstructionsInRange(0, pos).Insert(new CodeInstruction[] {
                CodeInstruction.Call(typeof(DontScaleToolTips), nameof(DontScaleToolTips.GetTooltipScaler)),
            });
            return m.InstructionEnumeration();
        }
    }

    // Previous attempt which tried to emulate controllers, not as clean and not needed
#if false
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateAxisValues))]
    public static class SteamVrUpdateAxisValues
    {
        static bool Prefix(GameInput __instance, bool useKeyboard, bool useController)
        {
            if (useKeyboard && !useController) {
                return true;
            }

            for (int i = 0; i < GameInput.axisValues.Length; i++)
            {
                GameInput.axisValues[i] = 0f;
            }

            Vector2 move = SteamVR_Actions.subnautica.Move.GetAxis(SteamVR_Input_Sources.Any);
            Vector2 look = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
            bool move_up = SteamVR_Actions.subnautica.MoveUp.GetState(SteamVR_Input_Sources.Any);
            bool move_down = SteamVR_Actions.subnautica.MoveDown.GetState(SteamVR_Input_Sources.Any);

            GameInput.axisValues[0] = look.x; // Right Stick X
            GameInput.axisValues[1] = -look.y; // Right Stick Y
            GameInput.axisValues[2] = move.x; // Left Stick X
            GameInput.axisValues[3] = -move.y; // Left Stick Y
            GameInput.axisValues[4] = move_up ? 1.0f : 0.0f; // LeftTrigger - Unused
            GameInput.axisValues[5] = move_down ? 1.0f : 0.0f; // RightTrigger - Unused

            GameInput.axisValues[6] = 0; // DPadX - Unused
            GameInput.axisValues[7] = 0; // DPadY - Unused
            GameInput.axisValues[8] = 0; // MouseX - Unused
            GameInput.axisValues[9] = 0; // MouseY - Unused

            GameInput.axisValues[10] = -look.y; // Mouse Wheel - Emulate from right stick y


            if (Settings.IsDebugEnabled) {
                DebugPanel.Show($"{GameInput.axisValues[0]}, {GameInput.axisValues[1]}, {GameInput.axisValues[2]}, {GameInput.axisValues[3]}, {GameInput.axisValues[4]}, {GameInput.axisValues[5]}\nAvailable: {GameInput.controllerAvailable} -> Primary: {GameInput.GetPrimaryDevice()} IsGamePad: {GameInput.IsPrimaryDeviceGamepad()}");
            }
            return false;
        }
    }
#endif

    #endregion

}
