using UnityEngine.EventSystems;
using UnityEngine;
using HarmonyLib;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Reflection.Emit;

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

            // TODO: Check if this could be better patched in the raycaster canvas
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
            threshold = 0.04f;
            __result = !useDragThreshold || (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
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

#if false
    // Makes it so that you can still interact with the UI, even when the Game is not focused, which only makes sense in VR I guess.
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.OnUpdate))]
    static class ContinueOnLostFocus {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.DeclaredPropertyGetter(typeof(BaseInputModule), "eventSystem");
            Mod.logger.LogInfo($"Getter: {getter}");
            // instructions.ForEach(ins => {
                // Mod.logger.LogInfo($"   : {ins}");
            // });
            var m = new CodeMatcher(instructions);
            var mc = m.Clone();

            var patched = m.MatchForward(false, new CodeMatch[] {
                // I tried to patch these four instruction out or make them always branch, but getting `Invalid IL: ret` error
                // /* 0x00128725 286616000A   */ IL_0001: call      instance class [UnityEngine.UI]UnityEngine.EventSystems.EventSystem [UnityEngine.UI]UnityEngine.EventSystems.BaseInputModule::get_eventSystem()
                // /* 0x0012872A 6F6716000A   */ IL_0006: callvirt  instance bool [UnityEngine.UI]UnityEngine.EventSystems.EventSystem::get_isFocused()
                // /* 0x0012872F 2D01         */ IL_000B: brtrue.s  IL_000E
                // /* 0x00128731 2A           */ IL_000D: ret
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ret),
            }).ThrowIfInvalid("Could not find target").RemoveInstructions(5);
            // }).ThrowIfInvalid("Could not find target").SetOpcodeAndAdvance(OpCodes.Nop).SetOpcodeAndAdvance(OpCodes.Ldc_I4_1); 

            return patched.InstructionEnumeration();
        }
    }
#endif

    // TODO: Not sure if needed. Is the GamepadInputModule used?
    // Wonder if it actually conflicts with the FPSInputModule or not
    [HarmonyPatch(typeof(GamepadInputModule), nameof(GamepadInputModule.IsInputAllowed))]
    static class AllowGamepadUnfocused
    {
        public static bool Prefix(ref bool __result)
        {
            __result = !WaitScreen.IsWaiting; // && Application.isFocused;
            return false;
        }
    }

#if false
    // TODO: Remove/Toggle
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
#endif

}

