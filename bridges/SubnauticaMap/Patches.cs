using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

namespace SubnauticaMapBridge
{
    // The map mod places hover coordinates and note markers from Input.mousePosition.
    // In VR the pointer is the laser, so emulate the mouse position at the laser's screen position
    // (center of the UI event camera, the same way SubmersedVR raycasts).
    [HarmonyPatch(typeof(Input), nameof(Input.mousePosition), MethodType.Getter)]
    static class MapModMousePosition
    {
        public static void Postfix(ref Vector3 __result)
        {
            if (!MapMod.IsActive || !MapMod.PdaOpen)
            {
                return;
            }

            if (!MapMod.TryGetInputModule(out FPSInputModule input))
            {
                return;
            }

            // SubmersedVR patches GetCursorScreenPosition to return the laser position in the UI
            // event camera space, so reusing it keeps us consistent with the uGUI pointer no matter
            // how the VR camera chain is set up. The call itself is through reflection: the method
            // is private in the user's runtime build (direct call = MethodAccessException every
            // frame, v0.2.0).
            if (!MapMod.TryGetLaserScreenPosition(out Vector2 laserScreenPos))
            {
                return;
            }

            __result = new Vector3(laserScreenPos.x, laserScreenPos.y, 0f);
        }
    }

    // The map mod pans with a native uGUI ScrollRect drag. SubmersedVR lowers the drag threshold to
    // 0.04 (world space) in a prefix; we run a postfix (executed last) so our rule wins while the
    // laser is over the map, leaving SubmersedVR's behavior untouched everywhere else.
    //
    // Start rule (v0.3.2): MOVEMENT ONLY — the drag engages as soon as the laser hit point on the
    // map has moved past PanMoveThreshold (world meters, Windows-style: cursor movement decides
    // click vs drag). No hold fallback, no options: v0.3.1's sliders were tested (user) and the
    // whole options UI was removed with them (v0.3.0 philosophy: installed = active).
    //
    // This hook is STATELESS (v0.2.6): it only reads the engagement measured per-frame in
    // MapModRuntime.UpdatePanEngage. v0.2.5 held pressStart here — but ShouldStartDrag is polled
    // only while the EventSystem processes a pressed pointer, so between presses the timestamp
    // survived and the next press engaged "instantly" (log.9: "engaged after 364.36s").
    [HarmonyPatch(typeof(FPSInputModule), nameof(FPSInputModule.ShouldStartDrag))]
    static class MapModDragThreshold
    {
        public static void Postfix(ref bool __result, Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!MapMod.IsActive || !MapMod.PdaOpen || !MapMod.IsMapOpen || !MapMod.IsHoveringMap())
            {
                return;
            }

            MapModRuntime runtime = MapModRuntime.instance;
            if (runtime == null)
            {
                return;
            }

            __result = runtime.IsPanEngaged();
        }
    }

    // The map mod creates a note on pointer click + LeftCtrl. In VR a plain trigger click on the
    // map creates a note instead. LeftCtrl is left to the mod's own path so no double creation happens.
    // CreateNote(null) drops an orphan note at (0,0) when its raycast is off the map, hence the
    // hover gate on the click event's own raycast + the cursor sync from that raycast.
    static class NoteOnMapClick
    {
        // ~5 cm of hand movement. SubmersedVR stores world-space positions in the pointer event, so
        // the threshold is in meters. CONFIRM AT FIRST LAUNCH via the one-shot diag logs below:
        // if the values are screen coordinates instead, all notes would be blocked and this
        // constant must be re-scaled (plan §5.2a / test W1).
        const float ClickMaxDragDistance = 0.05f;

        static bool loggedNoteClick;
        static bool loggedDragSuppression;
        static int diagClickCount; // first 10 clicks: log the gate status (diagnose silent skips)

        public static void Prefix(PointerEventData eventData)
        {
            // Hover gate on the click event's OWN raycast — the one that dispatched the click —
            // not FPSInputModule.lastRaycastResult, which can be stale/invalid on the dispatch
            // frame (log.3: hover=False on all 10 clicks while the event raycast hit "Map").
            // "Map" is scrollView.content (decompiled L1372), a child of mapContainer by
            // construction, so this check is timing-independent.
            RaycastResult clickRay = eventData.pointerCurrentRaycast;
            GameObject target = clickRay.isValid && clickRay.gameObject != null ? clickRay.gameObject : eventData.pointerPress;
            bool hover = MapMod.IsChildOfMapContainer(target != null ? target.transform : null);

            if (diagClickCount < 10)
            {
                int n = diagClickCount++;
                bool active = MapMod.IsActive;
                bool pda = active && MapMod.PdaOpen;
                bool map = pda && MapMod.IsMapOpen;
                bool form = map && MapMod.NoteFormActive;
                bool lrValid = MapMod.TryGetLastRaycast(out RaycastResult lr);
                string lrName = lrValid ? lr.gameObject.name : "-";
                string press = eventData.pointerPress != null ? eventData.pointerPress.name : "null";
                float dist = (eventData.position - eventData.pressPosition).magnitude;
                Mod.logger.LogInfo($"[diag] note click #{n}: active={active} pda={pda} map={map} form={form} hover={hover} press={press} lastRaycast={lrName} dist={dist:0.000} dragging={eventData.dragging}");
            }

            if (!MapMod.IsActive || !MapMod.PdaOpen || !MapMod.IsMapOpen || MapMod.NoteFormActive)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                return;
            }

            // Left button only: on desktop the mod's note action is LeftCtrl + left click. The
            // other emulated VR buttons (A/B/X = middle/right) must not create notes — middle on
            // map icons is the mod's native color/visibility action (ToggleColor, decompiled
            // L2252-2254) and is left untouched.
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!hover)
            {
                return;
            }

            // Releasing a DRAG also fires a pointer click: suppress it. The geometric check is the
            // primary mechanism (world-space distance between press and release, self-contained);
            // `dragging` is a best-effort addition — nobody can verify who sets it in the game's
            // EventSystem (the NuGet stubs have empty bodies).
            if (eventData.pointerPress == null)
            {
                return;
            }

            float pressDistance = (eventData.position - eventData.pressPosition).magnitude;
            if (pressDistance > ClickMaxDragDistance)
            {
                if (!loggedDragSuppression)
                {
                    loggedDragSuppression = true;
                    Mod.logger.LogWarning($"[diag] note suppressed: pressDistance={pressDistance:0.000} > {ClickMaxDragDistance:0.00} (expected ~0.01-0.05 for a clean click if units are meters)");
                }
                return;
            }

            if (eventData.dragging)
            {
                return;
            }

            if (!loggedNoteClick)
            {
                loggedNoteClick = true;
                Mod.logger.LogInfo($"[diag] first note click: position={eventData.position:0.000}, pressDistance={pressDistance:0.000}");
            }

            // Point the mod's cursor at the click raycast so CreateNote() places the note exactly
            // where the user clicked (its placement reads CursorManager.lastRaycast, decompiled
            // L1483, and orphans the note at (0,0) when that is invalid).
            MapMod.SyncCursorFromRaycast(clickRay);
            MapMod.CreateNote();
        }
    }

    // Detect the map mod (retry here in case it loads after us) and create the runtime once.
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    static class SetupBridge
    {
        public static void Postfix()
        {
            if (!MapMod.Detect())
            {
                return;
            }

            MapMod.ApplyNotePatch();
            MapMod.ApplyScanCirclePatch();

            if (MapModRuntime.instance == null)
            {
                MapModRuntime.instance = new GameObject("SubnauticaMapBridgeRuntime").AddComponent<MapModRuntime>();
            }
        }
    }

    // Drives the map mod's ScrollRect with the left stick and its private Zoom() with the right
    // stick while the laser cursor is over the map. Also samples the (trigger/A = "UISubmit")
    // press once per frame for the long-press pan hold: Unity's canonical input pattern is to
    // sample input in Update and keep event hooks stateless — the ShouldStartDrag hook only runs
    // while the EventSystem polls the pointer, so state held there latches between presses (the
    // v0.2.5 "engaged after 364s" bug).
    public class MapModRuntime : MonoBehaviour
    {
        public static MapModRuntime instance;

        const float PanSpeed = 0.5f;
        const float DeadZone = 0.05f;
        const float ZoomNotchesPerSecond = 10f; // full stick → 10 steps/s × 50px = 500px/s
        const float ZoomStep = 50f; // the mod's "controller" step (Zoom with controller: true)

        float zoomAccum;
        bool firstZoomLogged;
        bool firstPanLogged;

        // Pan-engage state, sampled every frame here (never in the ShouldStartDrag hook, which is
        // only polled while a pointer is pressed — v0.2.5 latch bug, log.9 "engaged after 364s").
        // v0.3.2: engage = the laser hit point on the map moved past PanMoveThreshold
        // (Windows-style). No hold fallback (v0.3.1's was removed with the sliders).
        const float PanMoveThreshold = 0.002f; // v0.3.1 slider tested at minimum (log.15), user asked to raise it (log.16 test)

        float panPressStart = -1f;
        Vector3 panPressPos;
        bool panHasPressPos;
        bool panEngaged;
        bool pressDetectedLogged;
        bool engageLogged;

        public bool IsPanEngaged()
        {
            return MapMod.IsActive && panEngaged;
        }

        public float PanPressElapsed => panPressStart > 0f ? Time.unscaledTime - panPressStart : -1f;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        // Runs every frame, before any context gate: the release must be observed no matter what,
        // otherwise panPressStart latches (the v0.2.5 bug).
        void UpdatePanEngage()
        {
            // Discontinuity guard: a frame gap of several maximum timesteps = pause/backgrounding,
            // the input history across it is unknown → reset (Unity Time.maximumDeltaTime pattern).
            if (Time.unscaledDeltaTime > Time.maximumDeltaTime * 4f)
            {
                panPressStart = -1f;
                panEngaged = false;
                panHasPressPos = false;
                return;
            }

            // String polling, deliberately NOT an action handle: SteamVR_Action.Create before the
            // plugin's action init returns a non-null object with handle 0, which a null check
            // cannot detect — that broke v0.2.6 (native "InvalidHandle handle: 0" every poll,
            // press never seen, log10). The string statics cache the lookup, and SubmersedVR
            // itself polls by string in its per-frame input path (SteamVrGetButtonDown,
            // SteamVrGameInput.cs:120) — for a single action the difference is negligible.
            if (SteamVR_Input.GetState("UISubmit", SteamVR_Input_Sources.Any))
            {
                if (panPressStart < 0f)
                {
                    panPressStart = Time.unscaledTime;
                    panEngaged = false;
                    panHasPressPos = MapMod.TryGetLastRaycast(out RaycastResult pressRay) && pressRay.isValid;
                    panPressPos = pressRay.worldPosition;
                    if (!pressDetectedLogged)
                    {
                        pressDetectedLogged = true;
                        Mod.logger.LogInfo("[diag] drag timer: press detected (UISubmit)");
                    }
                }
                else if (!panEngaged)
                {
                    // Windows-style: cursor movement decides click vs drag. The laser hit point on
                    // the map (world space — same basis as SubmersedVR's 0.04m drag threshold)
                    // moving past the threshold engages the drag immediately.
                    if (panHasPressPos && MapMod.TryGetLastRaycast(out RaycastResult ray) && ray.isValid)
                    {
                        float moveDist = (ray.worldPosition - panPressPos).magnitude;
                        if (moveDist > PanMoveThreshold)
                        {
                            panEngaged = true;
                            LogEngage($"engaged by movement ({moveDist:0.000}m after {PanPressElapsed:0.00}s)");
                        }
                    }
                }
            }
            else
            {
                panPressStart = -1f;
                panEngaged = false;
                panHasPressPos = false;
            }
        }

        void LogEngage(string detail)
        {
            if (engageLogged)
            {
                return;
            }
            engageLogged = true;
            Mod.logger.LogInfo($"[diag] drag timer: {detail} (move={PanMoveThreshold:0.000}m)");
        }

        void Update()
        {
            UpdatePanEngage();
            UpdateScanCircleDiag();

            if (!MapMod.IsActive)
            {
                return;
            }

            if (!MapMod.TryGetInputModule(out FPSInputModule input))
            {
                return;
            }

            MapMod.SyncCursorRaycast();

            if (!MapMod.PdaOpen || !MapMod.IsMapOpen || MapMod.NoteFormActive || !MapMod.IsHoveringMap())
            {
                return;
            }

            ScrollRect scrollView = MapMod.GetScrollView();
            if (scrollView == null)
            {
                return;
            }

            // "Move" is SubmersedVR's left thumbstick action, read from the global SteamVR action
            // registry (no action set / actions.json of our own, so no StreamingAssets conflict).
            Vector2 stick = SteamVR_Input.GetVector2("subnautica", "Move", SteamVR_Input_Sources.Any);
            if (stick.sqrMagnitude >= DeadZone * DeadZone)
            {
                Vector2 normalized = scrollView.normalizedPosition;
                normalized.x = Mathf.Clamp01(normalized.x + stick.x * PanSpeed * Time.unscaledDeltaTime);
                normalized.y = Mathf.Clamp01(normalized.y + stick.y * PanSpeed * Time.unscaledDeltaTime);
                scrollView.normalizedPosition = normalized;

                if (!firstPanLogged)
                {
                    firstPanLogged = true;
                    Mod.logger.LogInfo("[diag] first map pan (left stick)");
                }
            }

            // "UIScroll" is SubmersedVR's right thumbstick. The mod's native Zoom() is unreachable
            // from it (its Update gates Zoom behind an exact ±1 scroll, impossible with a
            // continuous stick), so accumulate whole 50px steps and call the private Zoom()
            // directly — the only path that also recomputes mapScale/UpdateIcons.
            if (MapMod.ZoomAvailable)
            {
                float zy = SteamVR_Input.GetVector2("subnautica", "UIScroll", SteamVR_Input_Sources.Any).y;
                if (zy > DeadZone || zy < -DeadZone)
                {
                    zoomAccum += zy * ZoomNotchesPerSecond * Time.unscaledDeltaTime;
                }
                else
                {
                    zoomAccum = 0f; // reset residual drift
                }

                while (zoomAccum >= 1f || zoomAccum <= -1f)
                {
                    // Same gate as the mod's native zoom (laser not over scan panel / icons).
                    if (!MapMod.IsLaserOverExcludedUi())
                    {
                        MapMod.InvokeZoom(zoomAccum > 0f ? ZoomStep : -ZoomStep);
                        if (!firstZoomLogged)
                        {
                            firstZoomLogged = true;
                            Mod.logger.LogInfo("[diag] first map zoom (right stick)");
                        }
                    }
                    zoomAccum -= zoomAccum > 0f ? 1f : -1f;
                }
            }
        }

        // v0.3.5 — the scan-circle plane fix + diagnostic. The mod's Rotate coroutine
        // (decompiled L2981-2991) spins the scan distance circle with transform.eulerAngles —
        // WORLD space. In 2D the PDA canvas world rotation is identity, so it behaves like an
        // in-plane spin. In VR the canvas is rotated to the PDA screen's world orientation
        // (SubmersedVR head-space placement, log.17/18), so each frame the circle is snapped to
        // the world XZ plane: out of the map plane, frozen at a world ~(0,0,0) orientation
        // while its center (localPosition, L1705) still tracks the room icon — exactly the
        // user's report ("cercle pas dans le même plan que la carte, figé dans le monde,
        // orientation 0,0,0, centre suit l'icône"). The ScanCircleRotateLocal transpiler
        // (applied in MapMod.ApplyScanCirclePatch) redirects that setter to
        // set_localEulerAngles: an in-plane spin, identical behavior in 2D.
        // v0.3.4's canvas pin is gone: log.18 proved the core re-places the canvas after
        // LateUpdate every frame (the pin was dead code) — and the map placement itself is
        // CORRECT (user: map stuck to the PDA screen, follows the PDA). Only the circle was
        // wrong. The diagnostic (3 samples, UNSCALED time — Update still runs while the PDA is
        // open) measures the circle's world angle against the canvas plane to validate the fix.
        const int ScanCircleDiagMax = 3; // first sample on (re)open, then every 2 s unscaled

        int scanCircleDiagSamples;
        bool scanCircleDiagWasOpen;
        double scanCircleDiagNext = -1d;

        void UpdateScanCircleDiag()
        {
            if (!MapMod.ModPresent || scanCircleDiagSamples >= ScanCircleDiagMax)
            {
                return;
            }

            bool mapOpen = MapMod.PdaOpen && MapMod.IsMapOpen;
            if (!mapOpen)
            {
                scanCircleDiagWasOpen = false;
                scanCircleDiagNext = -1d;
                return;
            }
            if (!scanCircleDiagWasOpen)
            {
                scanCircleDiagWasOpen = true; // (re)open: sample immediately
            }
            if (Time.unscaledTime < scanCircleDiagNext)
            {
                return;
            }
            scanCircleDiagNext = Time.unscaledTime + 2f;
            scanCircleDiagSamples++;

            try
            {
                // Plane reference: the PDA canvas root rect (scaler._rectTransform, reflection —
                // private at the user's runtime, AGENTS.md visibility lesson).
                uGUI_PDA pdaUI = null;
                foreach (uGUI_PDA p in FindObjectsOfType<uGUI_PDA>())
                {
                    pdaUI = p;
                    break;
                }
                RectTransform canvasRt = pdaUI != null && pdaUI.canvasScaler != null ? GetScalerField<RectTransform>(pdaUI.canvasScaler, "_rectTransform") : null;
                Mod.logger.LogInfo($"[diag] scancircle: #{scanCircleDiagSamples} canvas eul={(canvasRt != null ? canvasRt.eulerAngles.ToString("0.0") : "-")} timeScale={Time.timeScale:0.00}");

                Image[] circles = MapMod.GetScanCircles();
                if (circles == null)
                {
                    Mod.logger.LogInfo("[diag] scancircle: mapRoomMapIconList/scanCircle not found in this mod version");
                    return;
                }

                foreach (Image circle in circles)
                {
                    if (circle == null)
                    {
                        continue;
                    }
                    // The circle's rotation expressed in canvas space: in-plane = a PURE Z
                    // spin, so the healthy state is x≈0 ∧ y≈0 — NOT "quaternion angle to the
                    // canvas is small" (v0.3.5-0.3.7 indicator, a false alarm while the radar
                    // sweeps: a healthy in-plane circle 155° into its sweep is 155° away from
                    // the canvas rotation as a quaternion). planeDelta = x/y/z in canvas space
                    // (x/y wrapped to [-180,180] to kill the euler wrap ambiguity).
                    string planeDelta = "n/a";
                    string inPlane = "?";
                    if (canvasRt != null)
                    {
                        Vector3 d = (Quaternion.Inverse(canvasRt.rotation) * circle.transform.rotation).eulerAngles;
                        d.x = Wrap180(d.x);
                        d.y = Wrap180(d.y);
                        planeDelta = $"{d.x:0.0}/{d.y:0.0}/{d.z:0.0}";
                        inPlane = (Mathf.Abs(d.x) < 2f && Mathf.Abs(d.y) < 2f) ? "True" : "False";
                    }
                    string sprite = circle.sprite != null ? circle.sprite.name : "-";
                    Mod.logger.LogInfo($"[diag] scancircle: go='{circle.gameObject.name}' pos={circle.transform.position:0.000} eul={circle.transform.eulerAngles:0.0} sprite='{sprite}' enabled={circle.enabled} inPlane={inPlane} planeDelta={planeDelta}");
                }
            }
            catch (Exception ex)
            {
                Mod.logger.LogWarning($"[diag] scancircle: sample failed: {ex.Message}");
            }
        }

        // v0.3.7 safety net: re-align the scan circle with the map plane after the mod's Rotate
        // coroutine snaps it to the world XZ plane (its world-eulerAngles write — the Harmony
        // transpiler is the primary fix, this guarantees the plane even if the IL match misses
        // a future mod rebuild). LateUpdate = after every Update-phase coroutine, so the
        // correction is the last write of the frame (no flicker). No-op when the transpiler
        // works (the canvas-space delta is then a pure in-plane z).
        RectTransform circleCanvasRt;
        int circleCorrections;
        bool circleCorrLogged;

        void LateUpdate()
        {
            if (!MapMod.ModPresent || !MapMod.PdaOpen || !MapMod.IsMapOpen)
            {
                return;
            }

            Image[] circles = MapMod.GetScanCircles();
            if (circles == null)
            {
                return;
            }

            // Canvas root rect (scaler._rectTransform) — the map plane reference. Re-resolved
            // if stale (the PDA UI is rebuilt across scene loads; destroyed Unity objects are
            // "fake null", hence the extra `!` check).
            if (circleCanvasRt == null || !circleCanvasRt)
            {
                uGUI_PDA pdaUI = null;
                foreach (uGUI_PDA p in FindObjectsOfType<uGUI_PDA>())
                {
                    pdaUI = p;
                    break;
                }
                circleCanvasRt = pdaUI != null && pdaUI.canvasScaler != null ? GetScalerField<RectTransform>(pdaUI.canvasScaler, "_rectTransform") : null;
                if (circleCanvasRt == null)
                {
                    return;
                }
            }

            Quaternion canvasRot = circleCanvasRt.rotation;
            foreach (Image circle in circles)
            {
                if (circle == null)
                {
                    continue;
                }
                // The circle's rotation expressed in canvas space: in-plane = a pure z spin.
                Vector3 delta = (Quaternion.Inverse(canvasRot) * circle.transform.rotation).eulerAngles;
                if (Mathf.Abs(Wrap180(delta.x)) < 2f && Mathf.Abs(Wrap180(delta.y)) < 2f)
                {
                    continue;
                }
                // The mod wrote a WORLD rotation this frame: re-apply the sweep angle as an
                // in-plane (local) z spin, keeping the radar sweep itself.
                circle.transform.localEulerAngles = new Vector3(0f, 0f, delta.z);
                circleCorrections++;
                if (!circleCorrLogged)
                {
                    circleCorrLogged = true;
                    Mod.logger.LogInfo($"[diag] scancircle: world-snapped rotation re-aligned in-plane (delta vs canvas before={(Wrap180(delta.x)):0.0}/{(Wrap180(delta.y)):0.0}/{delta.z:0.0}, corrections so far={circleCorrections})");
                }
            }
        }

        static float Wrap180(float a)
        {
            a %= 360f;
            if (a > 180f)
            {
                a -= 360f;
            }
            if (a < -180f)
            {
                a += 360f;
            }
            return a;
        }

        static Dictionary<string, FieldInfo> scalerFieldCache = new Dictionary<string, FieldInfo>();

        // Visibility-agnostic field read (publicized-stub lesson, AGENTS.md) + inheritance-walking
        // lookup: GetField does not walk the hierarchy for non-public members, and _anchor /
        // _rectTransform / _canvas are private in the user's runtime build.
        static T GetScalerField<T>(object target, string name) where T : class
        {
            if (!scalerFieldCache.TryGetValue(name, out FieldInfo field))
            {
                field = null;
                for (Type t = target.GetType(); t != null && field == null; t = t.BaseType)
                {
                    field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                scalerFieldCache[name] = field;
            }
            return field?.GetValue(target) as T;
        }
    }

    // Redirects the mod's scan-circle spin from world space to the rect's local space — applied
    // to SubnauticaMap.MapRoomMapIcon.Rotate AND the MoveNext of its nested coroutine state
    // machines by MapMod.ApplyScanCirclePatch (the actual calls live in the state machine —
    // raw IL in MapRoomMapIcon/<Rotate>d__15::MoveNext, NOT in the Rotate body). Swaps the
    // call's operand only (identical signatures); if a future mod build stops calling them
    // there, the transpiler is a harmless no-op.
    //
    // BOTH the getter and the setter are swapped (v0.3.8): the coroutine is a FEEDBACK LOOP —
    // `z = transform.eulerAngles.z - 30°·dt; transform.eulerAngles = (0,0,z)`. Swapping the
    // setter only (v0.3.7) left the world-space read-back: with a tilted PDA the world euler z
    // of canvasRot×Rz(localZ) ≠ localZ, so the effective sweep speed — and even direction —
    // depended on the PDA orientation (user observation, log.21: ~184° in 2 s instead of 60°).
    // Swapping both makes the loop purely local: a constant 30°/s in-plane spin, identical to
    // the mod's 2D behavior and independent of the canvas orientation.
    static class ScanCircleRotateLocal
    {
        static MethodInfo getEulerAngles;
        static MethodInfo setEulerAngles;
        static MethodInfo getLocalEulerAngles;
        static MethodInfo setLocalEulerAngles;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            if (setEulerAngles == null)
            {
                getEulerAngles = AccessTools.Method(typeof(Transform), "get_eulerAngles");
                setEulerAngles = AccessTools.Method(typeof(Transform), "set_eulerAngles");
                getLocalEulerAngles = AccessTools.Method(typeof(Transform), "get_localEulerAngles");
                setLocalEulerAngles = AccessTools.Method(typeof(Transform), "set_localEulerAngles");
            }

            // Operand-based match on BOTH call forms: the mod's IL uses `callvirt` (class
            // method through a typed reference), matching `call` only was a v0.3.5 no-op.
            // Opcode names are matched as strings: System.Reflection.Emit.OpCodes is shadowed
            // by a Unity stub type in this project (CS0117, CS0436 suppressed in the csproj).
            foreach (CodeInstruction code in codes)
            {
                bool isCall = code.opcode.Name == "call" || code.opcode.Name == "callvirt";
                if (isCall && ReferenceEquals(code.operand, setEulerAngles) && setLocalEulerAngles != null)
                {
                    code.operand = setLocalEulerAngles;
                }
                else if (isCall && ReferenceEquals(code.operand, getEulerAngles) && getLocalEulerAngles != null)
                {
                    code.operand = getLocalEulerAngles;
                }
                yield return code;
            }
        }
    }

    // Neutralizes the map mod's native double zoom: its Update calls Zoom() whenever
    // Input.mouseScrollDelta.y is exactly ±1, and SubmersedVR emits ±1.0 every frame while the
    // right stick is fully deflected — a normal state for an analog stick, not an edge case.
    // Clamping to ±0.99 in the map context keeps the mod's `num3 != 1f` gate closed for its
    // native Zoom() without patching the mod; 0.99 vs 1.0 is imperceptible for the native
    // ScrollRect pan (kept on purpose).
    //
    // The gate is live on every read (no cached state): IsMapOpen = the mod's own MapIsOpened()
    // (PDA state 0 + map tab 3 + mapContainer activeSelf), so the clamp closes on the same frame
    // the map does and never touches the other PDA lists.
    [HarmonyPatch(typeof(Input), nameof(Input.mouseScrollDelta), MethodType.Getter)]
    static class MapModScrollClamp
    {
        public static void Postfix(ref Vector2 __result)
        {
            if (!MapMod.IsActive || !MapMod.IsMapOpen)
            {
                return;
            }

            if (Mathf.Abs(__result.y) > 0.99f)
            {
                __result = new Vector2(__result.x, Mathf.Sign(__result.y) * 0.99f);
            }
        }
    }

}
