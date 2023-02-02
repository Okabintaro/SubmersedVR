using HarmonyLib;
using UnityEngine;

namespace SubmersedVR{
    // Tweaks regarding the HUD of the game
    static class VRHud {
        private static Transform screenCanvas;
        private static Transform overlayCanvas;
        private static Transform hud;
        private static bool hudSetup = false;

        private static Canvas staticHudCanvas = null;

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


        public static void Setup(Camera uiCamera, Transform rightControllerUI) {
            Mod.logger.LogInfo($"Setting up HUD for {uiCamera.name}");

            screenCanvas = uGUI.main.screenCanvas.gameObject.transform;
            overlayCanvas = uGUI.main.overlays.gameObject.transform.parent;
            hud = uGUI.main.hud.transform;

            if (staticHudCanvas == null) {
                var uiRig = VRCameraRig.instance.uiRig.transform;
                var go = new GameObject("StaticHUDCanvas").WithParent(uiRig);
                staticHudCanvas = go.AddComponent<Canvas>();
                staticHudCanvas.renderMode = RenderMode.WorldSpace;
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = screenCanvas.GetComponent<RectTransform>().sizeDelta;
                rt.anchoredPosition = screenCanvas.GetComponent<RectTransform>().anchoredPosition;
                go.transform.localPosition = Vector3.forward;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = screenCanvas.localScale;
                go.layer = LayerID.UI;
            }
            staticHudCanvas.worldCamera = uiCamera;

            screenCanvas.SetParent(uiCamera.transform);
            overlayCanvas.SetParent(uiCamera.transform);

            // Steal Reticle and attach to the right hand
            //TODO: set it so the hand icon does not rotate with controller
            //var handReticle = HandReticle.main.gameObject.WithParent(rightControllerUI.transform);
            var handReticle = HandReticle.main.gameObject.WithParent(VRCameraRig.instance.laserPointerUI.pointerDot.transform);
            handReticle.GetOrAddComponent<Canvas>().worldCamera = uiCamera;
            handReticle.transform.LookAt(Camera.current.transform.position);
            handReticle.transform.localRotation = Quaternion.Euler(40, 0, 0);
            handReticle.transform.localPosition = new Vector3(0, -5, VRCameraRig.instance.laserPointerUI.pointerDot.transform.localPosition.z);//new Vector3(0, 0, 0.05f);
            handReticle.transform.localScale = VRCameraRig.instance.laserPointerUI.pointerDot.transform.localScale * 2;//new Vector3(0.001f, 0.001f, 0.001f);

            var compo = screenCanvas.GetComponent<uGUI_CanvasScaler>();
            if (compo != null) {
                compo.SetDirty();
            }
            screenCanvas.GetComponentsInChildren<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
            hudSetup = true;
        }

        public static void OnEnterVehicle() {
            var player = Player.main;
            if (player != null) {
                hud.SetParent(staticHudCanvas.transform, false);
            }
        }

        public static void OnExitVehicle() {
            hud.SetParent(screenCanvas, false);
        }
    }

#region Patches

    // Switch to the vehicle quickslots when entering a vehicle and back to the player quickslots when exiting
    [HarmonyPatch]
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeBegin))]
    public static class SetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            if (__instance is SeaMoth || __instance is Exosuit) {
                VRHud.OnEnterVehicle();
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeEnd))]
    public static class ReSetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            if (__instance is SeaMoth || __instance is Exosuit) {
                VRHud.OnExitVehicle();
            }
        }
    }

#endregion

}
