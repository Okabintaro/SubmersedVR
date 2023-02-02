using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using System.Reflection;

    #region Patches
    static class SteamVrGameInput
    {
        public static bool InputLocked = false;

        public static bool ShouldIgnore(GameInput.Button button)
        {
            return InputLocked
                || button == GameInput.Button.Slot1
                || button == GameInput.Button.Slot2
                || button == GameInput.Button.Slot3
                || button == GameInput.Button.Slot4
                || button == GameInput.Button.Slot5
                || button == GameInput.Button.AutoMove;
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
            if(SteamVR.initializedState != SteamVR.InitializedStates.InitializeSuccess)
            {
                __result = 0.0f;
                return false;
            }
            if (SteamVrGameInput.InputLocked)
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
                    value = vec.y > 0.0f ? vec.y : 0.0f;
                    break;
                case GameInput.Button.LookDown:
                    vec = SteamVR_Actions.subnautica.Look.GetAxis(SteamVR_Input_Sources.Any);
                    value = vec.y < 0.0f ? -vec.y : 0.0f;
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
                DebugPanel.Show($"{GameInput.GetMoveDirection()}");
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
            if (__instance.interactable) {
                uGUI_CraftingMenu.Node node = __instance.GetNode(icon);
                switch (button) {
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

            // TODO: This could probably be cached/optimized
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
