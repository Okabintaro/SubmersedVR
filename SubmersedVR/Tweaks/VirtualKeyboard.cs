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

        // Self-managed keyboard text: the overlay buffer cannot be trusted in some environments,
        // see OnKeyboardCharInput for the reconstruction rules.
        private static TMP_InputField currentField;
        private static string tracked;
        private static bool firstCharReplacesSeed;

        // Guards against ActivateInputField focus churn reopening (and reseeding) the keyboard mid-session.
        public static bool KeyboardOpen { get; private set; }

        void Start()
        {
            SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).RemoveListener(OnKeyboardClosed);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardClosed).AddListener(OnKeyboardClosed);
            SteamVR_Events.System(EVREventType.VREvent_ShowKeyboard).RemoveListener(OnKeyboardShown);
            SteamVR_Events.System(EVREventType.VREvent_ShowKeyboard).AddListener(OnKeyboardShown);
            SteamVR_Events.System(EVREventType.VREvent_HideKeyboard).RemoveListener(OnKeyboardHidden);
            SteamVR_Events.System(EVREventType.VREvent_HideKeyboard).AddListener(OnKeyboardHidden);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).RemoveListener(OnKeyboardCharInput);
            SteamVR_Events.System(EVREventType.VREvent_KeyboardCharInput).AddListener(OnKeyboardCharInput);
            VirtualKeyboard.instance = this;
        }

        private void OnKeyboardClosed(VREvent_t evt)
        {
            KeyboardOpen = false;
            var textBuilder = new StringBuilder(256);
            int caretPosition = (int)SteamVR.instance.overlay.GetKeyboardText(textBuilder, 256);
            string gktText = textBuilder.ToString();
            // Commit the rebuilt buffer; fall back to the overlay buffer when it is empty.
            string commitText = tracked;
            if (string.IsNullOrEmpty(commitText))
            {
                commitText = gktText;
            }
            string what = callback != null ? $"committing='{commitText}'" : "no commit (no callback)";
            Mod.logger?.LogInfo($"[VRKbd] KeyboardClosed tracked='{tracked}' gkt='{gktText}' {what}");

            if (callback != null)
            {
                callback(commitText);
            }
            currentField = null;
            tracked = "";
            firstCharReplacesSeed = true;
        }

        // Defensive: keep the flag in sync on runtimes where these events fire.
        private void OnKeyboardShown(VREvent_t evt)
        {
            KeyboardOpen = true;
        }

        private void OnKeyboardHidden(VREvent_t evt)
        {
            KeyboardOpen = false;
        }

        // The SteamVR keyboard overlay text subsystem is unreliable (Quest 3 + SteamVR: the overlay
        // displays nothing, CharInput payloads are empty, GetKeyboardText returns only the last
        // character). The real text is rebuilt from the GetKeyboardText read of each keystroke:
        //   empty read / backspace / delete -> backspace (observed delivery); a fresh seed is
        //                                      preselected on a real keyboard, so the first
        //                                      backspace clears it
        //   other control char              -> ignored
        //   one printable char              -> appended (the first keystroke replaces the seed)
        //   >1 char                         -> healthy runtime, mirror the real buffer
        private void OnKeyboardCharInput(VREvent_t evt)
        {
            if (!KeyboardOpen)
            {
                return;
            }
            var gktBuilder = new StringBuilder(256);
            SteamVR.instance.overlay.GetKeyboardText(gktBuilder, 256);
            string gktText = gktBuilder.ToString();

            if (gktText.Length == 0 || gktText == "\b" || gktText == "\u007F")
            {
                if (firstCharReplacesSeed)
                {
                    tracked = "";
                }
                else if (tracked.Length > 0)
                {
                    tracked = tracked.Substring(0, tracked.Length - 1);
                }
            }
            else if (gktText.Length == 1 && !char.IsControl(gktText[0]))
            {
                if (firstCharReplacesSeed)
                {
                    tracked = "";
                    firstCharReplacesSeed = false;
                }
                tracked += gktText;
            }
            else if (gktText.Length > 1)
            {
                tracked = gktText;
                firstCharReplacesSeed = false;
            }

            if (currentField != null)
            {
                var field = currentField as uGUI_InputField;
                string display = tracked;
                if (field?.uppercase == true)
                {
                    display = display.ToUpper();
                }
                currentField.text = display;
            }
        }

        // currentField is reset here on purpose: field-based callers assign it AFTER this call,
        // the beacon path has no field.
        public static void OpenKeyboardWithText(string text, string prompt = "Input Text", Action<string> callback = null)
        {
            VirtualKeyboard.instance.callback = callback;
            currentField = null;
            tracked = text;
            firstCharReplacesSeed = true;
            bool wasOpen = KeyboardOpen;
            KeyboardOpen = true;
            Mod.logger?.LogInfo($"[VRKbd] ShowKeyboard seed='{text}' wasOpen={wasOpen}");
            SteamVR.instance.overlay.ShowKeyboard(0, 0, 0, prompt, 256, text, 1);
        }

        public static void Deactivate()
        {
            if (VirtualKeyboard.instance == null)
            {
                return;
            }
            VirtualKeyboard.instance.callback = null;
            KeyboardOpen = false;
            // Drop the session state so a still-visible overlay cannot keep writing into the field.
            currentField = null;
            tracked = "";
            firstCharReplacesSeed = true;
        }

        public static void OpenKeyboardOnTextField(TMP_InputField inputField, string prompt = "Input Text", Action<string> callback = null)
        {
            OpenKeyboardWithText(inputField.text, prompt, (text) =>
            {
                if (inputField == null)
                {
                    return;
                }
                var field = inputField as uGUI_InputField;
                if (field?.uppercase == true)
                {
                    text = text.ToUpper();
                }
                inputField.text = text;
                field?.EndEdit();
                inputField.OnDeselect(null);
            });
            // Assigned after the call: OpenKeyboardWithText resets currentField for the beacon path.
            currentField = inputField;
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
    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.ActivateInputField))]
    static class ShowVirtualKeyboardOnFocus
    {
        public static void Postfix(TMP_InputField __instance)
        {
            // Focus churn while a session is open must not reopen (and reseed) the keyboard.
            if (VirtualKeyboard.KeyboardOpen)
            {
                return;
            }
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
