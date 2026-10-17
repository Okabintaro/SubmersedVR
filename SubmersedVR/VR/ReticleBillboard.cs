using TMPro;
using UnityEngine;
namespace SubmersedVR
{
    // Per-frame reticle layout for the two laser pointer modes:
    // - target info: the root canvas (tool texts + energy) stays anchored at
    //   the hand with its legacy fixed orientation (it rotates with the
    //   tool); the target action info is split onto a second canvas
    //   (ReticleSplit) which is projected per frame on the laser hit point,
    //   oriented to stay readable (perpendicular to the laser, world up).
    // - dot: the whole reticle follows the laser pointer dot; the position
    //   is set once at setup, this component keeps the orientation
    //   readable the same way.
    // The game's own HandReticle.LateUpdate also writes the reticle in XR,
    // but that branch is disabled upstream (NoReticleMovementInVR), so the
    // order relative to it does not matter.
    [DefaultExecutionOrder(1)]
    public class ReticleBillboard : MonoBehaviour
    {
        // Hand anchor, shared with the reticle setups in VRHud
        internal static readonly Vector3 AnchorOffset = new Vector3(0f, 0f, 0.05f);
        internal static readonly Vector3 AnchorScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Projected scale: the original reticle was parented to the pointer dot
        // (localScale 0.03) with a local scale of 0.06, an effective 0.0018;
        // it now hangs off the controller UI (scale 1.0)
        internal static readonly Vector3 TargetScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

        // Debug (Debug Overlays): last logged reticle texts
        static string lastHand;
        static string lastHandSub;
        static string lastUse;
        static string lastUseSub;
        // Full child-tree dumps (capped: the energy % text ticks every second)
        static int treeDumpCount;
        static string lastDumpContext;
        const int MaxTreeDumps = 40;

        void LateUpdate()
        {
            var rig = VRCameraRig.instance;
            if (rig == null || rig.uiCamera == null || rig.rightControllerUI == null)
            {
                return;
            }
            var laser = rig.laserPointerUI;
            var anchor = rig.rightControllerUI.transform;

            if (transform.parent == anchor)
            {
                // Target info mode: the root always stays at the hand (tool
                // texts + energy live there)
                transform.localPosition = AnchorOffset;
                transform.localScale = AnchorScale;

                // No world target (e.g. build mode): the parts stay on the
                // hand with the original layout, all info visible
                bool aiming = rig.HasWorldTarget() && laser != null;

                ReticleSplit.Enforce(aiming);

                // The root keeps its legacy fixed orientation (set in
                // SetupHandReticleLaserPointer): the tool texts rotate with
                // the tool. Only the target canvas is oriented per frame, so
                // the pointed info stays readable: plane perpendicular to the
                // hand laser (world up) - aiming implies the laser exists
                var target = ReticleSplit.TargetCanvas;
                if (target != null)
                {
                    if (aiming)
                    {
                        // 5 cm past the hit point so the beam tip does not
                        // touch the icon
                        Vector3 hit = laser.transform.position + laser.transform.forward * (rig.worldTargetDistance + 0.05f);
                        target.localPosition = transform.InverseTransformPoint(hit);
                        // Effective world scale TargetScale, relative to the root (AnchorScale)
                        target.localScale = new Vector3(TargetScale.x / AnchorScale.x, TargetScale.y / AnchorScale.y, TargetScale.z / AnchorScale.z);
                        // World orientation independent of the tool rotation
                        var desired = ReadableRotation(laser.transform.forward);
                        target.localRotation = Quaternion.Inverse(transform.rotation) * desired;
                        target.gameObject.SetActive(true);
                    }
                    else
                    {
                        target.gameObject.SetActive(false);
                    }
                }
            }
            else if (laser != null && laser.pointerDot != null && transform.parent == laser.pointerDot.transform)
            {
                // Dot mode: the position is set once in
                // SetupHandReticleOnPointerDot (the game's LateUpdate z
                // write is disabled by NoReticleMovementInVR); keep the
                // orientation readable per frame: plane perpendicular to
                // the laser, world up (the dot inherits the tool rotation,
                // the WIP one-shot orientation went stale when the tool
                // moved)
                transform.rotation = ReadableRotation(laser.transform.forward);
            }
            else
            {
                // Not anchored at the hand nor on the dot: hide the split
                // target so it does not freeze at the last hit point
                var frozenTarget = ReticleSplit.TargetCanvas;
                if (frozenTarget != null)
                {
                    frozenTarget.gameObject.SetActive(false);
                }
            }

            LogTexts();
        }

        // Debug (Debug Overlays): logs the four text fields when they change,
        // and dumps the full child tree when the pointed object/action changes
        // (placeholder audit: the split must cover every element, incl. the
        // Count text and the craft material icons)
        // Plane perpendicular to the laser, world up - with a fallback up
        // when the laser points straight up/down (degenerate LookRotation)
        static Quaternion ReadableRotation(Vector3 forward)
        {
            var up = Mathf.Abs(forward.y) > 0.999f ? Vector3.right : Vector3.up;
            return Quaternion.LookRotation(forward, up);
        }

        void LogTexts()
        {
            if (!Settings.IsDebugEnabled) return;
            var handReticle = HandReticle.main;
            if (handReticle == null) return;

            string hand = SafeText(handReticle.compTextHand);
            string handSub = SafeText(handReticle.compTextHandSubscript);
            string use = SafeText(handReticle.compTextUse);
            string useSub = SafeText(handReticle.compTextUseSubscript);
            if (hand == lastHand && handSub == lastHandSub && use == lastUse && useSub == lastUseSub) return;
            lastHand = hand;
            lastHandSub = handSub;
            lastUse = use;
            lastUseSub = useSub;
            Mod.logger.LogInfo($"[ReticleDebug] Hand=\"{hand}\" HandSub=\"{handSub}\" Use=\"{use}\" UseSub=\"{useSub}\"");

            string context = hand + "\u0001" + handSub;
            if (context != lastDumpContext && treeDumpCount < MaxTreeDumps)
            {
                lastDumpContext = context;
                treeDumpCount++;
                DumpReticleTree();
            }
        }

        // Debug (Debug Overlays): recursive dump of the reticle child tree;
        // our split target canvas is skipped (it is our own overlay)
        void DumpReticleTree()
        {
            var skip = ReticleSplit.TargetCanvas;
            var sb = new System.Text.StringBuilder();
            DumpNode(transform, skip, sb, 0);
            foreach (var line in sb.ToString().Split('\n'))
            {
                if (line.Length > 0)
                {
                    Mod.logger.LogInfo("[ReticleDebug] tree: " + line);
                }
            }
        }

        static void DumpNode(Transform t, Transform skip, System.Text.StringBuilder sb, int depth)
        {
            if (t == null || t == skip)
            {
                return;
            }
            // Mark the cap instead of dropping silently: the audit must not
            // mistake a truncated node for a non-existent one
            if (depth >= 5)
            {
                sb.Append(new string(' ', depth * 2)).Append("… (depth limit)\n");
                return;
            }
            var tmg = t.GetComponent<TextMeshProUGUI>();
            var img = t.GetComponent<UnityEngine.UI.Image>();
            var canvas = t.GetComponent<Canvas>();
            var rt = t as RectTransform;
            sb.Append(new string(' ', depth * 2)).Append(t.name);
            if (!t.gameObject.activeInHierarchy)
            {
                sb.Append(" [inactive]");
            }
            if (rt != null)
            {
                sb.Append(" pos=").Append(rt.anchoredPosition).Append(" size=").Append(rt.sizeDelta);
            }
            if (tmg != null)
            {
                sb.Append(" TMP=\"").Append(EscapeText(tmg.text)).Append('"');
            }
            else if (img != null)
            {
                sb.Append(canvas != null ? " Image+Canvas" : " Image");
            }
            else if (canvas != null)
            {
                sb.Append(" Canvas");
            }
            sb.Append('\n');
            for (int i = 0; i < t.childCount; i++)
            {
                DumpNode(t.GetChild(i), skip, sb, depth + 1);
            }
        }

        static string EscapeText(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }
            return s.Replace("\r", "").Replace("\n", "\\n");
        }

        static string SafeText(TextMeshProUGUI comp)
        {
            return comp != null ? comp.text : null;
        }
    }
}
