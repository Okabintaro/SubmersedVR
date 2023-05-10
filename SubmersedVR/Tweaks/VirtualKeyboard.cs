using System.Text;
using UnityEngine;
using TMPro;

// This uses the SteamVR Keyboard overlay to enable text input on text fields in the game.
namespace SubmersedVR
{
    extern alias SteamVRRef;

    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using HarmonyLib;
    using SteamVRRef.Valve.VR;

    public class VirtualKeyboard : MonoBehaviour
    {
        private Action<string> callback;
        private static VirtualKeyboard instance;

        void Start()
        {
            SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).RemoveListener(OnKeyboardClosed);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).AddListener(OnKeyboardClosed);
            VirtualKeyboard.instance = this;
        }

        private void OnKeyboardClosed(VREvent_t evt)
        {
            var textBuilder = new StringBuilder(256);
            int caretPosition = (int)SteamVR.instance.overlay.GetKeyboardText(textBuilder, 256);
            string text = textBuilder.ToString();

            if (callback != null)
            {
                callback(text);
            }
        }

        public static void OpenKeyboardWithText(string text, string prompt = "Input Text", Action<string> callback = null)
        {
            VirtualKeyboard.instance.callback = callback;
            SteamVR.instance.overlay.ShowKeyboard(0, 0, 0, prompt, 256, text, 1);
        }

        public static void Deactivate()
        {
            if (VirtualKeyboard.instance == null) {
                return;
            }
            VirtualKeyboard.instance.callback = null;
        }

        public static void OpenKeyboardOnTextField(TMP_InputField inputField, string prompt = "Input Text", Action<string> callback = null)
        {
            OpenKeyboardWithText(inputField.text, prompt, (text) =>
            {
                // Make it uppercase if needed and close the inputgroup
                if (inputField is uGUI_InputField field)
                {
                    if (field.uppercase)
                    {
                        text = text.ToUpper();
                    }
                    field.EndEdit();
                }
                inputField.text = text;
                inputField.OnDeselect(null);
            });
        }
    }

    #region Patches

    // Setup the keyboard singleton
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class SetupKeyboard
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var keyboard = new GameObject(nameof(VirtualKeyboard)).AddComponent<VirtualKeyboard>();
            UnityEngine.Object.DontDestroyOnLoad(keyboard.gameObject);
        }
    }

    // Open virtual keyboard once the input field was activated
    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.ActivateInputFieldInternal))]
    static class ShowVirtualKeyboardOnFocus
    {
        public static void Postfix(TMP_InputField __instance)
        {
            VirtualKeyboard.OpenKeyboardOnTextField(__instance);
        }
    }

    // Forget callback on deactivation
    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.DeactivateInputField))]
    static class ClearCallbackOnDeactivate
    {
        public static void Postfix(TMP_InputField __instance)
        {
            VirtualKeyboard.Deactivate();
        }
    }

    // Replace the whole beacon dialog with the SteamVR virtual keyboard
    [HarmonyPatch(typeof(BeaconLabel), nameof(BeaconLabel.OnHandClick))]
    static class ShowVirtualKeyboardOnBeacon
    {
        public static bool Prefix(BeaconLabel __instance)
        {
            VirtualKeyboard.OpenKeyboardWithText(__instance.labelName, __instance.stringBeaconLabel, (label) =>
            {
                __instance.SetLabel(label);
            });
            return false;
        }
    }

    // Dont focus text field immediately when editing signs so you can still adjust the other sign settings
    [HarmonyPatch(typeof(uGUI_SignInput), nameof(uGUI_SignInput.OnSelect))]
    static class DontFocusTextFieldOnSignEdit
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).MatchForward(false, new CodeMatch[] {
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(ci => ci.Calls(typeof(TMP_InputField).GetMethod(nameof(TMP_InputField.ActivateInputField)))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(ci => ci.Calls(typeof(uGUI_InputField).GetMethod(nameof(uGUI_InputField.SelectAllText))))
            }).ThrowIfNotMatch("Could not find target").RemoveInstructions(6).InstructionEnumeration();
        }
    }

    // We don't use touch keyboard on Desktop
    // Without this submiting with the VRKeyboard breaks
    [HarmonyPatch(typeof(TouchScreenKeyboardManager), nameof(TouchScreenKeyboardManager.isSupported), MethodType.Getter)]
    static class DisableTouchScreenKeyboard2
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    // But focus text field immediately on ColoredLabels used by Lockers
    [HarmonyPatch(typeof(ColoredLabel), nameof(ColoredLabel.OnHandClick))]
    static class ActivateInputFieldOnCloredLabel
    {
        public static void Postfix(ColoredLabel __instance)
        {
            if (__instance.enabled)
            {
                __instance.signInput.inputField.ActivateInputField();
            }
        }
    }

    // Enable deselection of input groups at all times.
    // TODO: There might be a better place for this
    [HarmonyPatch(typeof(uGUI_InputGroup), nameof(uGUI_InputGroup.Update))]
    static class DeslectOnUICancel
    {
        public static void Postfix(uGUI_InputGroup __instance)
        {
            if (__instance.focused && GameInput.GetButtonDown(GameInput.Button.UICancel))
            {
                __instance.Deselect();
            }
        }
    }

    #endregion
}
