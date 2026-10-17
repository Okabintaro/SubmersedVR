using System;
using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    // Tweaks regarding the HUD of the game
    static class VRHud
    {
        private static Transform screenCanvas;
        private static Transform overlayCanvas;
        private static Transform hud;

        private static Canvas staticHudCanvas = null;
        // private static OffsetCalibrationTool calibrationTool;

        // Last reticle root a mode was applied to; the VRCameraRig watchdog
        // re-applies the mode when a new reticle instance appears
        // (save/level load recreates it after VRHud.Setup already ran)
        public static Transform SetupReticle;

        // TODO: Hud Distance needs dedicated canvas, since the Pips seem to assume the 1 meter canvas distance.
#if false
        public static float hudDistance = 1.0f;
        public static float HudDistance {
            get {
                return hudDistance;
            }
            set {
                hudDistance = value;
                if (staticHudCanvas == null || screenCanvas == null) {
                    return;
                }
                hud.transform.localPosition = Vector3.forward * (hudDistance - 1.0f);
            }
        }
        public static void OnHudDistanceChanged(float value) {
            HudDistance = value;
        }
#endif

        public static void SetupHandReticle(string mode)
        {
            if (HandReticle.main == null) return;
            if (mode == HandReticleModes.PointerEnd)
            {
                SetupHandReticleOnPointerDot();
            }
            else if (mode == HandReticleModes.TargetInfo)
            {
                SetupHandReticleLaserPointer();
            }
            else
            {
                SetupHandReticleOnHand();
            }
            SetupReticle = HandReticle.main.transform;
            // Laser pointer modes: the target canvas is projected on the hit
            // point per frame (target info), or the whole reticle is kept
            // readable on the dot (dot)
            HandReticle.main.gameObject.GetOrAddComponent<ReticleBillboard>().enabled = mode == HandReticleModes.TargetInfo || mode == HandReticleModes.PointerEnd;
        }

        public static void SetupHandReticleOnHand()
        {
            ReticleSplit.Unsplit();
            var rig = VRCameraRig.instance;
            // Steal Reticle and attach to the right hand
            var handReticle = HandReticle.main.gameObject.WithParent(rig.rightControllerUI.transform);
            handReticle.GetOrAddComponent<Canvas>().worldCamera = rig.uiCamera;
            handReticle.transform.localEulerAngles = new Vector3(90, 0, 0);
            handReticle.transform.localPosition = ReticleBillboard.AnchorOffset;
            handReticle.transform.localScale = ReticleBillboard.AnchorScale;
        }

        // The original WIP layout: the whole reticle follows the laser
        // pointer dot; ReticleBillboard keeps it readable per frame
        public static void SetupHandReticleOnPointerDot()
        {
            ReticleSplit.Unsplit();
            var rig = VRCameraRig.instance;
            // The laser pointer may not be set up yet (the VRCameraRig
            // watchdog retries until it is)
            var dot = rig.laserPointerUI?.pointerDot?.transform;
            if (dot == null)
            {
                return;
            }
            var handReticle = HandReticle.main.gameObject.WithParent(dot);
            handReticle.GetOrAddComponent<Canvas>().worldCamera = rig.uiCamera;
            handReticle.transform.localPosition = new Vector3(0, -5, dot.localPosition.z);
            handReticle.transform.localScale = dot.localScale * 2;
        }

        public static void SetupHandReticleLaserPointer()
        {
            var rig = VRCameraRig.instance;
            // Same anchor, size and fixed orientation as hand mode: the
            // reticle stays attached to the tool and rotates with it (legacy
            // layout). ReticleBillboard only projects the split target canvas
            // on the laser hit point per frame.
            var handReticle = HandReticle.main.gameObject.WithParent(rig.rightControllerUI.transform);
            handReticle.transform.localEulerAngles = new Vector3(90, 0, 0);
            handReticle.transform.localPosition = ReticleBillboard.AnchorOffset;
            handReticle.transform.localScale = ReticleBillboard.AnchorScale;
            var canvas = handReticle.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = handReticle.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = rig.uiCamera;
            handReticle.layer = LayerID.UI;
            ReticleSplit.Split(rig.uiCamera);
        }

        public static void OnHandReticleModeChanged(string mode)
        {
            var rig = VRCameraRig.instance;
            if (rig == null || rig.uiCamera == null || rig.rightControllerUI == null)
            {
                return;
            }
            SetupHandReticle(mode);
        }

        public static Canvas CreateWorldCanvas(this GameObject go)
        {
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.layer = LayerID.UI;
            return canvas;
        }

        public static void Setup(Camera uiCamera, Transform rightControllerUI)
        {
            Mod.logger.LogDebug($"Setting up HUD for {uiCamera.name}");

            screenCanvas = uGUI.main.screenCanvas.gameObject.transform;
            overlayCanvas = uGUI.main.overlays.gameObject.transform.parent;
            hud = uGUI.main.hud.transform;

            if (staticHudCanvas == null)
            {
                var uiRig = VRCameraRig.instance.uiRig.transform;
                var go = new GameObject("StaticHUDCanvas").WithParent(uiRig);
                staticHudCanvas = go.CreateWorldCanvas();
                var rt = go.GetComponent<RectTransform>();
                go.transform.localScale = screenCanvas.localScale;
                rt.sizeDelta = screenCanvas.GetComponent<RectTransform>().sizeDelta;
                rt.anchoredPosition = screenCanvas.GetComponent<RectTransform>().anchoredPosition;
                go.transform.localPosition = Vector3.forward;
                go.transform.localRotation = Quaternion.identity;
            }
            staticHudCanvas.worldCamera = uiCamera;

            screenCanvas.SetParent(uiCamera.transform, true);
            overlayCanvas.SetParent(uiCamera.transform, true);

            SetupHandReticle(Settings.HandReticleMode);
            Settings.HandReticleModeChanged -= OnHandReticleModeChanged;
            Settings.HandReticleModeChanged += OnHandReticleModeChanged;

            WristHud.Setup();

            var compo = screenCanvas.GetComponent<uGUI_CanvasScaler>();
            if (compo != null)
            {
                compo.SetDirty();
            }
            screenCanvas.GetComponentsInChildren<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
        }

        public static void OnEnterVehicle()
        {
            var player = Player.main;
            if (player != null)
            {
                hud.SetParent(staticHudCanvas.transform, false);
            }
        }

        public static void OnExitVehicle()
        {
            hud.SetParent(screenCanvas, false);
        }



    }

    static class WristHud
    {
        private static TransformOffset wristOffset = new TransformOffset(new Vector3(-0.079f, 0.148f, -0.158f), new Vector3(350.494f, 88.400f, 244.161f));
        private static GameObject wristTarget;
        private static Canvas canvas;
        private static CanvasGroup canvasGroup;

        // Cached Values
        private static Transform hudContent;
        private static Transform uiCamera;
        private static Transform cachedIndexTip;
        private static FMODAsset turnOnSound;
        private static FMODAsset turnOffSound;

        // State
        public static bool isHudOn = true;
        private static bool touchingWrist = false;
        private static bool prevTouchingWrist = false;

        public static FMODAsset CreateFMODAsset(string eventPath)
        {
            FMODAsset asset = ScriptableObject.CreateInstance<FMODAsset>();
            asset.path = eventPath;
            return asset;
        }

        // Create Wrist World Canvas
        public static void Setup()
        {
            var rig = VRCameraRig.instance;
            uiCamera = rig.uiCamera.transform;
            hudContent = uGUI.main.hud.transform.GetChild(0);

            if (wristTarget == null)
            {
                wristTarget = new GameObject("WristTarget").WithParent(rig.leftControllerUI).ResetTransform();
                var wristCanvasGo = new GameObject("WristCanvas").WithParent(wristTarget).ResetTransform();
                canvas = wristCanvasGo.CreateWorldCanvas();
                canvasGroup = wristCanvasGo.AddComponent<CanvasGroup>();
                wristCanvasGo.transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f);
                wristOffset.Apply(wristTarget.transform);
            }

            Settings.PutBarsOnWristChanged -= OnPutBarsOnHandChanged;
            Settings.PutBarsOnWristChanged += OnPutBarsOnHandChanged;
            Toggle(Settings.PutBarsOnWrist);

            turnOnSound = CreateFMODAsset("event:/tools/flashlight/turn_on");
            turnOffSound = CreateFMODAsset("event:/tools/flashlight/turn_off");
        }

        public static Transform GetIndexFingerTip()
        {
            if (cachedIndexTip != null)
            {
                return cachedIndexTip;
            }
            var animator = Player.main?.playerAnimator;
            if (animator is Animator anim)
            {
                var tip = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_R/clav_R_aim/shoulder_R/hand_R/hand_R_point_base/hand_R_point_mid/hand_R_point_tip_rig");
                if (tip != null)
                {
                    cachedIndexTip = tip;
                    return tip;
                }
            }
            return null;
        }

        public static void OnPutBarsOnHandChanged(bool isOn)
        {
            Toggle(isOn);
        }


        public static void OnUpdate()
        {
            if (!uGUI.isMainLevel)
            {
                return;
            }
            var camPos = uiCamera.transform.position;
            var worldRigPos = VRCameraRig.instance.rigParentTarget.position;
            var wristPos = wristTarget.transform.position;

            Vector3 wristDir = wristTarget.transform.TransformDirection(Vector3.forward);
            Vector3 toCam = (wristPos - camPos).normalized;

            float wristCamDot = Vector3.Dot(wristDir, toCam);
            bool isFacingCamera = wristCamDot > 0.1f;
            // DebugPanel.Show($"dot = {dot} <= {wristDir}, {toCam}");
            canvasGroup.alpha = Mathf.Max(wristCamDot, 0.0f);

            if (isFacingCamera && GetIndexFingerTip() is Transform indexTip)
            {
                var uiIndexPos = indexTip.position - worldRigPos;
                var wristDistance = Vector3.Distance(uiIndexPos, wristPos);
                // DebugPanel.Show($"wristDistance = {wristDistance} <= uiPos{uiIndexPos}, {wristPos}");
                const float threshold = 0.1f;
                touchingWrist = wristDistance < threshold;
                if (touchingWrist && !prevTouchingWrist)
                {
                    isHudOn = !isHudOn;
                    Utils.PlayFMODAsset(isHudOn ? turnOnSound : turnOffSound);
                }
                prevTouchingWrist = wristDistance < threshold;
            }
        }

        public static void Toggle(bool isOn)
        {
            if (canvas == null)
            {
                Setup();
            }

            var barsPanel = uGUI.main.barsPanel;
            if (isOn)
            {
                // Move to wrist
                Mod.logger.LogDebug("Turning WristHud on");
                barsPanel.WithParent(canvas.transform).ResetTransform();
                barsPanel.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
                ManagedUpdate.Subscribe(ManagedUpdate.Queue.PreCanvasFirst, new ManagedUpdate.OnUpdate(OnUpdate));
            }
            else
            {
                // Move back
                Mod.logger.LogDebug("Turning WristHud off");
                barsPanel.transform.SetParent(hudContent.transform, false);
                barsPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                barsPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.0f);
                ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.PreCanvasFirst, new ManagedUpdate.OnUpdate(OnUpdate));
                isHudOn = true;
            }

        }
    }

    #region Patches

    // Switch to the vehicle quickslots when entering a vehicle and back to the player quickslots when exiting
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeBegin))]
    public static class SetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            if (__instance is SeaMoth || __instance is Exosuit)
            {
                VRHud.OnEnterVehicle();
            }
        }
    }

    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeEnd))]
    public static class ResetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            if (__instance is SeaMoth || __instance is Exosuit)
            {
                VRHud.OnExitVehicle();
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_Pings), nameof(uGUI_Pings.IsVisibleNow))]
    public static class HidePingsWhenHudOff
    {
        public static void Postfix(ref bool __result)
        {
            __result &= WristHud.isHudOn;
        }
    }

    [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.GetDepthInfo))]
    public static class HideDepthCompassWhenHudOff
    {
        public static void Postfix(ref uGUI_DepthCompass.DepthMode __result)
        {
            if (!WristHud.isHudOn)
            {
                __result = uGUI_DepthCompass.DepthMode.None;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_PinnedRecipes), nameof(uGUI_PinnedRecipes.GetMode))]
    public static class HidePinnedRecipesWhenHudOff
    {
        public static void Postfix(ref uGUI_PinnedRecipes.Mode __result)
        {
            if (!WristHud.isHudOn)
            {
                __result = uGUI_PinnedRecipes.Mode.Off;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_PowerIndicator), nameof(uGUI_PowerIndicator.IsPowerEnabled))]
    public static class HidePowerWhenHudOff
    {
        public static void Postfix(ref bool __result)
        {
            __result &= WristHud.isHudOn;
        }
    }

    [HarmonyPatch(typeof(uGUI_SeamothHUD), nameof(uGUI_SeamothHUD.Update))]
    public static class HideSeamothHudWhenHudOff
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(true, new CodeMatch[] {
                new CodeMatch(OpCodes.Stloc_3),
            }).Advance(1).Insert(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                CodeInstruction.LoadField(typeof(WristHud), nameof(WristHud.isHudOn)),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Stloc_3),
            });
            return m.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(uGUI_ExosuitHUD), nameof(uGUI_ExosuitHUD.Update))]
    public static class HideExosuitHudWhenHudOff
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(true, new CodeMatch[] {
                new CodeMatch(OpCodes.Stloc_3),
            }).Advance(1).Insert(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                CodeInstruction.LoadField(typeof(WristHud), nameof(WristHud.isHudOn)),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Stloc_3),
            });
            return m.InstructionEnumeration();
        }
    }

    #endregion

}
