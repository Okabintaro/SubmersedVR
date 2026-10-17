using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SubmersedVR
{
    // Splits the hand reticle for the target info reticle mode: the target
    // action (compTextHand/HandSubscript, e.g. "Climb the ladder [A]") and
    // its icon and progress donut move to a second world canvas that
    // ReticleBillboard projects on the laser hit point, while the tool info
    // (use texts, energy) stays on the root canvas at the hand. Split only
    // registers the parts; all movement is driven per-frame by Enforce (see
    // ReticleBillboard), which is only active in that reticle mode.
    static class ReticleSplit
    {
        class MovedPart
        {
            public RectTransform rt;
            public Transform originalParent;
            public Quaternion originalLocalRot;
            public Vector3 originalLocalScale;
            public Vector2 originalAnchorMin;
            public Vector2 originalAnchorMax;
            public Vector2 originalPivot;
            public Vector2 originalAnchoredPos;
            // Zero when the part was registered inactive (the game collapses
            // the rect while the text is empty) - Enforce then falls back to
            // the text's natural width
            public Vector2 originalSizeDelta;
            // Zone on the target canvas (0 = icon/donut, 1 = name, 2 = action);
            // the Y (top of the block) is re-applied per frame
            public int zone;
            // Cached so Enforce does not GetComponent per frame (null on the
            // icon container, which has no text of its own)
            public TextMeshProUGUI tmp;
        }

        static GameObject targetCanvasGo;
        static List<MovedPart> moved = new List<MovedPart>();
        // Debug (Debug Overlays): cap on re-parent logs per split
        static int reparentLogCount;
        // Reticle root currently split (null when unsplit); the VRCameraRig
        // watchdog re-applies the setup when a new reticle instance appears
        // (save/level load recreates it after VRHud.Setup already ran)
        static Transform splitRoot;

        public static Transform TargetCanvas => targetCanvasGo != null ? targetCanvasGo.transform : null;

        public static Transform SplitRoot => splitRoot;

        public static void Split(Camera uiCamera)
        {
            Unsplit();
            reparentLogCount = 0;
            if (HandReticle.main == null)
            {
                return;
            }
            // On save/level load the reticle is recreated empty (texts and
            // RectTransform sizes are zero): the game lays it out a few
            // frames later. Splitting the zero state freezes a broken
            // layout, so wait until the game has laid it out once (the
            // VRCameraRig watchdog retries every frame)
            var handText = HandReticle.main.compTextHand;
            var handRt = handText != null ? handText.transform as RectTransform : null;
            if (handRt != null && handRt.sizeDelta.x <= 0.5f && handRt.rect.width <= 0.5f)
            {
                return;
            }
            var root = HandReticle.main.gameObject.transform;
            splitRoot = root;
            targetCanvasGo = new GameObject("TargetReticleCanvas", typeof(RectTransform));
            targetCanvasGo.transform.SetParent(root, false);

            var canvas = targetCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = uiCamera;
            var rootCanvas = root.GetComponent<Canvas>();
            if (rootCanvas != null)
            {
                // One above the hand canvas so the projected info never
                // fights it for draw order
                canvas.sortingOrder = rootCanvas.sortingOrder + 1;
                canvas.sortingLayerID = rootCanvas.sortingLayerID;
            }
            targetCanvasGo.layer = root.gameObject.layer;

            // No uGUI_CanvasScaler clone: the billboard drives the canvas
            // transform, and a runtime clone would miss the prefab _anchor.
            // Log the root's scaler (if any) to confirm in game
            var rootScaler = root.GetComponent<uGUI_CanvasScaler>();
            if (rootScaler != null && Settings.IsDebugEnabled)
            {
                Mod.logger.LogInfo($"[ReticleSplit] Root uGUI_CanvasScaler: refRes {rootScaler.referenceResolution} mode {rootScaler.mode} vrMode {rootScaler.vrMode}");
            }

            // The primary action on the focused target (e.g. "Climb the
            // ladder [A]") is the target info: it goes to the hit point,
            // with its action icon and the repair progress donut (%). The
            // tool info (use texts + energy) stays on the hand canvas.
            // Like the original hand layout: icon on top, text below
            var iconRt = HandReticle.main.iconCanvas;
            RegisterPart(iconRt, 0);
            // The progress donut and % label: a child of the icon container
            // follows the icon move, a sibling has to be registered on its own
            foreach (var comp in new Component[] { HandReticle.main.progressImage, HandReticle.main.progressText })
            {
                if (comp == null)
                {
                    continue;
                }
                if (!comp.transform.IsChildOf(iconRt))
                {
                    RegisterPart(comp, 0);
                }
            }
            RegisterPart(HandReticle.main.compTextHand, 1);
            RegisterPart(HandReticle.main.compTextHandSubscript, 2);
        }

        // Target canvas layout, top of each block (the blocks grow downward);
        // values calibrated in game against the original hand layout
        static float ZoneY(int zone)
        {
            return zone == 0 ? 20f : zone == 1 ? -20f : -55f;
        }

        // Captures the original layout for RestorePart and the split zone
        // for Enforce, without moving anything: Enforce places the parts
        static void RegisterPart(Component comp, int zone)
        {
            if (comp == null)
            {
                return;
            }
            var rt = (RectTransform)comp.transform;
            var part = new MovedPart
            {
                rt = rt,
                originalParent = rt.parent,
                originalLocalRot = rt.localRotation,
                originalLocalScale = rt.localScale,
                originalAnchorMin = rt.anchorMin,
                originalAnchorMax = rt.anchorMax,
                originalPivot = rt.pivot,
                originalAnchoredPos = rt.anchoredPosition,
                zone = zone,
                tmp = rt.GetComponent<TextMeshProUGUI>(),
                originalSizeDelta = rt.sizeDelta,
            };
            moved.Add(part);
            if (Settings.IsDebugEnabled)
            {
                Mod.logger.LogInfo($"[ReticleSplit] registered {rt.name}: anchors {rt.anchorMin}/{rt.anchorMax} pivot {rt.pivot} size {rt.sizeDelta} pos {rt.anchoredPosition}");
            }
        }

        // The game re-parents the primary action text to its icon every frame
        // (icon-driven layout), so the split must be re-asserted per frame.
        // Called from ReticleBillboard.LateUpdate with aiming = world target:
        // true puts the parts on the target canvas with the split layout,
        // false puts them back on the hand with the original layout
        public static void Enforce(bool aiming)
        {
            if (targetCanvasGo == null)
            {
                return;
            }
            var target = targetCanvasGo.transform;
            foreach (var part in moved)
            {
                if (part.rt == null)
                {
                    continue;
                }
                if (aiming)
                {
                    if (part.rt.parent != target)
                    {
                        if (Settings.IsDebugEnabled && reparentLogCount < 10)
                        {
                            reparentLogCount++;
                            var gameParent = part.rt.parent != null ? part.rt.parent.name : "null";
                            Mod.logger.LogInfo($"[ReticleSplit] Enforce: {part.rt.name} re-parented by game to {gameParent}, moving back to target");
                        }
                        part.rt.SetParent(target, false);
                        part.rt.anchorMin = part.rt.anchorMax = new Vector2(0.5f, 0.5f);
                        // Top-center pivot: the offset is the top of the block,
                        // which grows downward like the game's own layout
                        part.rt.pivot = new Vector2(0.5f, 1f);
                    }
                    // Per-frame: the prefab layout that resizes the text
                    // blocks does not run on this runtime canvas - own the
                    // position, the height and the width (a part registered
                    // while inactive was captured at zero width and would
                    // render nothing)
                    part.rt.anchoredPosition = new Vector2(0f, ZoneY(part.zone));
                    if (part.tmp != null && part.rt.gameObject.activeInHierarchy)
                    {
                        float h = part.tmp.preferredHeight;
                        float w = part.originalSizeDelta.x > 0.5f ? part.originalSizeDelta.x : part.tmp.preferredWidth;
                        if (Mathf.Abs(part.rt.sizeDelta.y - h) > 0.01f || Mathf.Abs(part.rt.sizeDelta.x - w) > 0.01f)
                        {
                            part.rt.sizeDelta = new Vector2(w, h);
                        }
                    }
                }
                else if (part.rt.parent == target)
                {
                    // Lost the target: back to the hand, original layout
                    RestorePart(part);
                }
            }
        }

        // Puts a part back on its original parent with the original layout;
        // the game re-lays it out (icon-driven) on the following frames
        static void RestorePart(MovedPart part)
        {
            if (part.rt == null || part.originalParent == null)
            {
                return;
            }
            part.rt.SetParent(part.originalParent, false);
            // Anchors first: the anchored position only maps to the local
            // position once the original anchors are back
            part.rt.anchorMin = part.originalAnchorMin;
            part.rt.anchorMax = part.originalAnchorMax;
            part.rt.pivot = part.originalPivot;
            part.rt.anchoredPosition = part.originalAnchoredPos;
            part.rt.localRotation = part.originalLocalRot;
            part.rt.localScale = part.originalLocalScale;
        }

        public static void Unsplit()
        {
            foreach (var part in moved)
            {
                // Destroyed objects (level load without unsplit) are skipped
                RestorePart(part);
            }
            moved.Clear();
            splitRoot = null;
            if (targetCanvasGo != null)
            {
                UnityEngine.Object.Destroy(targetCanvasGo);
                targetCanvasGo = null;
            }
        }
    }
}
