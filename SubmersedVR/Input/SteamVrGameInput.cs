using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;

    #region Patches
    static class SteamVrGameInput
    {
        public static bool ShouldIgnore(GameInput.Button button)
        {
            return button == GameInput.Button.Slot1 || button == GameInput.Button.Slot2 || button == GameInput.Button.Slot3 || button == GameInput.Button.Slot4 || button == GameInput.Button.Slot5 || button == GameInput.Button.AutoMove || button == GameInput.Button.UIClear;
        }
    }


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
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateAvailableDevices))]
    public static class ControllerOnly
    {
        public static bool Prefix()
        {
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
