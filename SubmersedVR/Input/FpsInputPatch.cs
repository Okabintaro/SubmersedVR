using UnityEngine.EventSystems;
using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    // Raycast from the middle of the event "camera" on the controller for accurate laserpointing
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.GetCursorScreenPosition))]
    class RaycastPointerPosition
    {
        public static void Postfix(ref Vector2 __result, FPSInputModule __instance)
        {
            if (VRCameraRig.instance == null || VRCameraRig.instance.UIControllerCamera == null)
            {
                return;
            }

            var eventCamera = VRCameraRig.instance.UIControllerCamera;
            __result = new Vector2(eventCamera.pixelWidth / 2, eventCamera.pixelHeight / 2);
        }
    }

    // Since we do the dragging/raycasting in worldspace now the drag threshold has to be way lower.
    // Can't change the EventSystem.pixelThreshold because that is only integer.
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.ShouldStartDrag))]
    class SetDragThresholdHacky
    {
        public static bool Prefix(ref bool __result, Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            // TODO: This has to be dependent on canvas scale, way to high for big pda, too low for small pda
            float newThreshold = 0.04f;
            __result = !useDragThreshold || (pressPos - currentPos).sqrMagnitude >= newThreshold * newThreshold;
            return false;
        }
    }

    // Instead of saving the screen space position of the pointer, we save the world space one. Needed to fix things like drag & drop.
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateMouseState))]
    static class UseWorldSpacePointerPosition
    {
        public static void Prefix(FPSInputModule __instance, PointerEventData leftData)
        {
            leftData.position = __instance.lastRaycastResult.worldPosition;
        }
    }

    // Use UICancel(uGUI.button2) as middle mouse button, which makes certain pointer ui work better.
    // It's needed to get X to work for pinning recipes in the fabricator.
    // TODO: Might be better to rewrite this using transpiler or rewrite completely.
    // In any case it's painful because you gotta call base methods: https://harmony.pardeike.net/articles/patching-edgecases.html#calling-base-methods
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateMouseState))]
    class EmulateMiddleMosueButtonToo : PointerInputModule
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PointerInputModule), "GetPointerData")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool GetPointerData(FPSInputModule instance, int id, out PointerEventData data, bool create)
        {
            data = null;
            return false;
        }

        public static void Postfix(FPSInputModule __instance, PointerEventData leftData)
        {
            GetPointerData(__instance, -3, out var data2, create: true);
            __instance.CopyFromTo(leftData, data2);
            data2.button = PointerEventData.InputButton.Middle;
            if (GameInput.GetPrimaryDevice() == GameInput.Device.Controller)
            {
                var buttonDown = GameInput.GetButtonDown(uGUI.button2);
                var buttonUp = GameInput.GetButtonUp(uGUI.button2);
                if (__instance.m_MouseState.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonState == PointerEventData.FramePressState.NotChanged)
                {
                    __instance.m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, FPSInputModule.ConstructPressState(buttonDown, buttonUp), data2);
                }
            }
        }

        public override void Process()
        {
        }
    }

#if false
    // Makes it so that you can still interact with the UI, even when the Game is not focused, which only makes sense in VR I guess.
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.OnUpdate))]
    static class ContinueOnLostFocus
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            var patched = m.MatchForward(false, new CodeMatch[] {
                /* Match and remove the following code from FPSInputModule.OnUpdate():
                if (!base.eventSystem.isFocused)
                {
                    return;
                }
                */
                // /* 0x0019E8A4 02           */ IL_0000: ldarg.0
                // /* 0x0019E8A5 288B1A000A   */ IL_0001: call      instance class [UnityEngine.UI]UnityEngine.EventSystems.EventSystem [UnityEngine.UI]UnityEngine.EventSystems.BaseInputModule::get_eventSystem()
                // /* 0x0019E8AA 6F8C1A000A   */ IL_0006: callvirt  instance bool [UnityEngine.UI]UnityEngine.EventSystems.EventSystem::get_isFocused()
                // /* 0x0019E8AF 2D01         */ IL_000B: brtrue.s  IL_000E
                // /* 0x0019E8B1 2A           */ IL_000D: ret
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Callvirt),
            }).ThrowIfInvalid("Could not find target").RemoveInstructions(5);
            return patched.InstructionEnumeration();
        }
    }
#endif

    // // TODO: Not sure if needed. Is the GamepadInputModule used?
    // // Wonder if it actually conflicts with the FPSInputModule or not
    // [HarmonyPatch(typeof(GamepadInputModule), nameof(GamepadInputModule.IsInputAllowed))]
    // static class AllowGamepadUnfocused
    // {
    //     public static bool Prefix(ref bool __result)
    //     {
    //         __result = !WaitScreen.IsWaiting; // && Application.isFocused;
    //         return false;
    //     }
    // }

    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.UpdateMouseState))]
    class DebugPointerEventState : PointerInputModule {
        public static void Postfix(FPSInputModule __instance) {
            var data = __instance.m_MouseState.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData;
            // Mod.logger.LogInfo($"Mouse State: {data.delta}, {data.dragging}, {data.position}, {data.pointerPress}");
            // DebugPanel.Show($"Mouse State: {data.delta}, {data.dragging}, {data.position}, {data.pointerPress}");
            bool shouldStartDrag = FPSInputModule.ShouldStartDrag(data.pressPosition, data.position, (float)EventSystem.current.pixelDragThreshold, data.useDragThreshold);
            // DebugPanel.Show($"IsDragging: {shouldStartDrag}\n IsMoving: {data.IsPointerMoving()} || {data.pointerDrag} == null \n shouldStart: {shouldStartDrag}\n useThreshold: {data.useDragThreshold}, {EventSystem.current.pixelDragThreshold}\n diff: {(data.pressPosition - data.position).sqrMagnitude}");
            DebugPanel.Show($"Position: {data.pressPosition}, {data.position}");
       }

        public override void Process()
        {
        }
    }

}

