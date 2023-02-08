using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;

    // This class implements a way to switch between tools on the quickbar using a radial menu.
    // It was inspired by the Inventory/Quick Select form Half Life: Alyx.
    public class VRQuickSlots : uGUI_QuickSlots
    {
        private bool setup = false;
        private bool active = false;
        private SteamVR_Action_Boolean action;

        private Transform controllerTarget;
        public float wheelRadius = 90.0f;

        public float threshold = 0.02f;
        public float angleOffset = -Mathf.PI / 2.0f;

        public int lastSlot = -1;
        private int currentSlot = -2;
        private Canvas canvas;

        private int nSlots
        {
            get
            {
                return icons.Length;
            }
        }

        void Awake()
        {
            gameObject.AddComponent<RectTransform>();
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.layer = LayerID.UI;
            transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            controllerTarget = VRCameraRig.instance.rightControllerUI.transform;
            canvas.enabled = false;
        }

        new void Start()
        {
            var qs = FindObjectsOfType<uGUI_QuickSlots>().First(obj => obj.name == "QuickSlots");
            Mod.logger.LogInfo($"[nameof{this.GetType()}] Start, stealing stuff from on {qs.name}");
            materialBackground = qs.materialBackground;
            spriteLeft = qs.spriteLeft;
            spriteCenter = qs.spriteCenter;
            spriteRight = qs.spriteRight;
            spriteNormal = qs.spriteNormal;
            spriteHighlighted = qs.spriteHighlighted;
            spriteExosuitArm = qs.spriteExosuitArm;
            spriteSelected = qs.spriteSelected;
        }

        new void Init(IQuickSlots newTarget)
        {
            if (newTarget == null) {
                return;
            }
            Mod.logger.LogInfo($"[nameof{this.GetType()}] Init on {newTarget}");
            base.Init(newTarget);
            ArangeIconsInCircle(wheelRadius);
            OnSelect(this.target.GetActiveSlotID());
        }

        new void OnSelect(int slotID) {
            if (target == null) {
                return;
            }
            base.OnSelect(slotID);
            if (slotID >= 0) {
                var pos = CirclePosition(slotID, nSlots, radius);
                selector.rectTransform.anchoredPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        void ArangeIconsInCircle(float radius)
        {
            for (int i = 0; i < nSlots; i++)
            {
                var pos = CirclePosition(i, nSlots, radius);
                icons[i].rectTransform.anchoredPosition = new Vector3(pos.x, pos.y, 0);
                backgrounds[i].rectTransform.anchoredPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        int DetermineSlot(float angle)
        {
            return Mathf.RoundToInt((angle / (2 * Mathf.PI)) * nSlots) % nSlots;
        }

        new void Update()
        {
            IQuickSlots quickSlots = this.GetTarget();
            if (this.target != quickSlots)
            {
                this.target = quickSlots;
                this.Init(this.target);
            }
            if (active)
            {
                var from = controllerTarget.position;
                var origin = transform.position;
                var pX = Vector3.Dot(from - origin, transform.right);
                var pY = Vector3.Dot(from - origin, transform.up);
                var projected = new Vector2(pX, pY);

                // var relPos = transform.position - controllerTarget.position;
                var angle = Mathf.Atan2(projected.y, projected.x);
                angle += angleOffset;
                if (angle < 0.0f)
                {
                    angle += 2 * Mathf.PI;
                }

                var distance = projected.sqrMagnitude;
                var doSwitch = distance > threshold;
                // TODO: Probably should use events to determine current slot, extending interface methods
                if (doSwitch)
                {
                    lastSlot = currentSlot;
                    currentSlot = DetermineSlot(angle);
                    if (currentSlot != lastSlot)
                    {
                        var targetSlots = GetTarget();
                        if (targetSlots == null) {
                            return;
                        }
                        targetSlots.SlotKeyDown(currentSlot);
                        SteamVR_Actions.subnautica_HapticsRight.Execute(0.0f, 0.1f, 10f, 0.5f, SteamVR_Input_Sources.Any);
                    }
                }
                else
                {
                    var targetSlots = GetTarget();
                    if (targetSlots == null) {
                        return;
                    }
                    // NOTE: you can't deselect in vehicles
                    if (targetSlots is QuickSlots) {
                        targetSlots.DeselectSlots();
                        currentSlot = -2;
                    }
                }

                // DebugPanel.Show($"y:{projected.y:f3} x:{projected.x:f3} -> {angle:f3}\n {distance:f3} -> {doSwitch}\n {angle:f3} -> {currentSlot}");
                this.selector.enabled = false;
            }
        }

        public void Activate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            canvas.enabled = true;
            transform.position = controllerTarget.transform.position;
            bool isVehicleSlot = GetTarget() is Vehicle;
            base.Update(); // This updates the battery values.

            // TODO: This still could use some tweaking, maybe just align with the controller
            var targetPos = VRCameraRig.instance.uiCamera.transform.position;
            SteamVR_Actions.subnautica_HapticsRight.Execute(0.0f, 0.1f, 10f, 0.5f, SteamVR_Input_Sources.Any);

            // Don't rotate the wheel up/down when not in vehicle.
            if (!isVehicleSlot) {
                targetPos.y = transform.position.y;
            }

            this.transform.LookAt(targetPos);

            currentSlot = -2;
            lastSlot = -1;
            active = true;
            FPSInputModule.current.lockRotation = true;
        }
        public void Deactivate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            canvas.enabled = false;
            active = false;
            FPSInputModule.current.lockRotation = false;
        }

        public void Setup(SteamVR_Action_Boolean activeAction)
        {
            if (setup)
            {
                Mod.logger.LogWarning($"Trying to setup {nameof(VRQuickSlots)} twice!");
                return;
            }
            // Setup the actions/callbacks from steam
            action = activeAction;
            action.onStateDown += Activate;
            action.onStateUp += Deactivate;
            setup = true;
        }

        new void OnDestroy() {
            Mod.logger.LogDebug($"Destroying {nameof(VRQuickSlots)}...");
            base.OnDestroy();
            if (setup) {
                action.onStateDown -= Activate;
                action.onStateUp -= Deactivate;
            }
        }

        private static Vector2 CirclePosition(int i, int nSlots, float radius = 10.0f)
        {
            float stepSize = 2 * Mathf.PI / nSlots;
            float angle = i * stepSize;
            angle += Mathf.PI / 2.0f; // Offset by 90°, so the layout is better with item 1 being at the top
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }

#region Patches

    // Switch to the vehicle quickslots when entering a vehicle and back to the player quickslots when exiting
    [HarmonyPatch]
    public static class QuickSlots_Patch
    {
        [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeBegin))]
        public static void Postfix(Vehicle __instance)
        {
            VRCameraRig.instance?.VrQuickSlots?.SetTarget(__instance);
        }

        [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeEnd))]
        public static void Postfix()
        {
            VRCameraRig.instance?.VrQuickSlots?.SetTarget(null);
        }
    }
#endregion

}