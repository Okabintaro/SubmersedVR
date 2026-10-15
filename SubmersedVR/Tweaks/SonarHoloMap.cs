using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SubmersedVR
{
    // Hologram map (3D) - the new sonar screen effect (26/09, strategy M):
    // on ping, the game's own hologram shader (FX/WBOIT-HoloMap - the
    // Seaglide / Cyclops / map room map material) is rendered on overlays
    // that SHARE the real terrain mesh: the sonar look on the true world
    // relief, revealed by a circular front (the screen wave's frontR(t)
    // formula) from the ping origin, then fading out. The effect is ADDED
    // on top of the normal terrain (the original renderer is never
    // touched). The blue wave screen effect runs ON THE SAME TIME (it is
    // active in this mode too - see SonarEffectOptions.IsScreenWave): the
    // floor shows the hologram, the whole view (objects, monsters, water)
    // glows with the blue wave. The reveal is FIXED to the blue wave's
    // calibration (RevealRadius / RevealSpeed / HoldTime / FadeTime below)
    // so both layers read as one ping. No bundle needed for the floor -
    // the material is a game asset (probed and validated in logs
    // .42/.43, see SubmersedVR-isobath-world-notes.md).
    //
    // Timeline: GAME time (Time.time), like the game's own screen effect
    // (Time.deltaTime in SonarScreenFX.Update) - a paused game freezes the
    // reveal/fade.
    //
    // The look is FROZEN (the user's final calibration, log .56); the only
    // live parameter is the shared Ping Range (Settings).
    static class SonarHoloMap
    {
        // The game's hologram shader (shared by Seaglide / Cyclops / map
        // room - select by shader name, never by instance)
        const string HoloShaderName = "FX/WBOIT-HoloMap";
        // Overlay creation budget per frame (spreads the GameObject cost
        // over ~1 s instead of one-frame spike)
        const int AddBudgetPerFrame = 40;
        // Re-collect the terrain pieces that stream in (s)
        const float ReCollectInterval = 2f;
        // Margin beyond the front (m) so the soft fade edge is always
        // backed by geometry
        const float EdgeMargin = 25f;
        // --- Reveal: SHARED Ping Range + frozen sweep / fade ---
        // The floor reveal follows the SHARED Ping Range (the front reaches
        // the same radius as the screen wave) at constant speed (Linear) -
        // the finalized look. hold = 0 so the fade starts right when the
        // sweep completes, and the fade (2.5 s) matches the wave's trail
        // (2.0 + 0.5) so both layers disappear together
        const float RevealSweep = 2.5f;
        const float FadeTime = 2.5f;
        static float RevealRadius => Settings.SonarPingRange;
        static float RevealSpeed => RevealRadius / RevealSweep;
        static float HoldTime => 0f;
        // Re-arm freshness window: the screen wave's full frozen lifetime
        // (sweep 2.5 + trail 2.0 + 0.5) + a small grace. HasPinged latches
        // forever, so WITHOUT this gate a holo->other->holo switch long
        // after the last ping would re-arm a fully-revealed ghost floor
        // while the screen wave (stateless) is long gone. Time.time is
        // frozen while paused, so the intended "ping, pause, switch to
        // tune" flow still re-arms (the delta stays at the pause moment)
        const float ReArmWindow = 2.5f + 2.0f + 0.5f + 1f;

        static Worker worker;

        internal static void OnPing(Vector3 origin)
        {
            if (Mod.quitting)
            {
                return;
            }
            if (!Settings.SonarModEnabled)
            {
                return;
            }
            if (!SonarEffectOptions.IsHoloMap(Settings.SonarScreenEffect))
            {
                return;
            }
            EnsureWorker();
            worker.StartSession(origin);
        }

        internal static void OnQuit()
        {
            if (worker != null)
            {
                worker.Shutdown();
                Object.DestroyImmediate(worker.gameObject);
                worker = null;
            }
        }

        static void EnsureWorker()
        {
            if (worker != null)
            {
                return;
            }
            var go = new GameObject("SubmersedVR Sonar Holo Map");
            Object.DontDestroyOnLoad(go);
            worker = go.AddComponent<Worker>();
        }

        class Worker : MonoBehaviour
        {
            struct Piece
            {
                public MeshFilter mf;
                public float dist;
            }

            struct Overlay
            {
                public GameObject go;
                public MeshRenderer mr;
                public MeshFilter srcMf;
                public MeshRenderer srcMr;
            }

            bool hasSession;
            Vector3 origin;
            // Accumulated GAME time of the session (only advances while the
            // holo map mode is selected and the game is not paused)
            float t;
            float radius;
            bool prevHolo;

            Material baseMaterial;
            Material matInstance;
            bool materialMissLogged;

            readonly List<Overlay> overlays = new List<Overlay>();
            readonly HashSet<MeshFilter> overlaid = new HashSet<MeshFilter>();
            List<Piece> pending;
            int pendingIdx;
            float nextReCollect;
            float nextMatRetry;

            public void StartSession(Vector3 pingOrigin, bool revealed = false)
            {
                if (hasSession)
                {
                    ClearOverlays();
                }
                // A re-ping during the fade must NOT reuse the partially
                // faded material instance - the new map would render dimmed
                // until the new session's fade start (brightness pop)
                if ((Object)(object)matInstance != null)
                {
                    Object.Destroy(matInstance);
                    matInstance = null;
                }
                hasSession = true;
                origin = pingOrigin;
                radius = RevealRadius;
                // revealed=true (a mode switch back to the holo map, no
                // fresh ping): start with the front already complete so the
                // map is visible straight away (tunable in the pause menu)
                t = revealed ? RevealTime() : 0f;
                materialMissLogged = false;
                nextMatRetry = 0f;
                pendingIdx = 0;
                nextReCollect = Time.time + ReCollectInterval;
                // Collect beyond the radius so the soft fade edge at the
                // boundary is always backed by geometry
                pending = CollectPieces(origin, radius + EdgeMargin);
                float maxDist = 0f;
                for (int i = 0; i < pending.Count; i++)
                {
                    if (pending[i].dist > maxDist)
                    {
                        maxDist = pending[i].dist;
                    }
                }
                Mod.logger.LogInfo($"[HoloMap] session{(revealed ? " (re-armed, revealed)" : "")}: origin={origin}, radius={radius:F0} m, {pending.Count} piece(s) in range, farthest {maxDist:F0} m - front {RevealSpeed:F0} m/s (linear), sweep {RevealSweep:F1} s, hold {HoldTime:F1} s, fade {FadeTime:F1} s");
                Mod.logger.LogInfo($"[HoloMap] chunk window: {LogChunkWindowExtent()}");
            }

            // t at which the reveal front (constant speed) has reached the
            // full radius
            float RevealTime()
            {
                float v = RevealSpeed;
                if (v < 1f)
                {
                    return 0f;
                }
                return radius / v;
            }

            // One-time-per-session diagnostic: how far do the STREAMED chunks
            // reach? The floor can only show the holo where terrain pieces
            // exist (the blue wave on screen is not so limited), so this line
            // plus the "farthest" piece distance shows the geometry extent.
            // CollectPieces already skips disabled renderers.
            static string LogChunkWindowExtent()
            {
                var streamer = LargeWorldStreamer.main;
                if ((Object)(object)streamer == null || (Object)(object)streamer.land == null)
                {
                    return "<no streamer>";
                }
                var window = streamer.land.chunkWindow;
                if (window == null || window.Length == 0)
                {
                    return "<empty window>";
                }
                int count = 0;
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int i = 0; i < window.Length; i++)
                {
                    var st = window[i];
                    if (st == null || (Object)(object)st.chunk == null)
                    {
                        continue;
                    }
                    count++;
                    var p = st.chunk.transform.position;
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                if (count == 0)
                {
                    return "<no chunks>";
                }
                return $"{count} chunk(s), extent {(max - min).x:F0} x {(max - min).y:F0} x {(max - min).z:F0} m around {(min + max) * 0.5f}";
            }

            void Update()
            {
                bool holo = SonarEffectOptions.IsHoloMap(Settings.SonarScreenEffect);
                if (hasSession && !holo)
                {
                    // Suspended (the mode switched to another effect): the
                    // timeline freezes and the overlays hide - switching
                    // back to the holo map RESUMES the same session (live
                    // mode switch, no re-ping, like the screen effects)
                    prevHolo = holo;
                    SyncOverlays(false);
                    return;
                }
                if (!hasSession)
                {
                    // Re-arm only from a RECENT ping (within ReArmWindow) -
                    // see the ReArmWindow constant for why (HasPinged
                    // latches forever; the screen wave is stateless/5 s)
                    bool recentPing = Time.time - SonarWorldPing.LastPingTime <= ReArmWindow;
                    if (holo && !prevHolo && SonarWorldPing.HasPinged && recentPing)
                    {
                        // The mode just switched to the holo map with no
                        // live session: re-arm from the last ping, already
                        // revealed, so the switch is visible straight away
                        StartSession(SonarWorldPing.LastOrigin, revealed: true);
                    }
                    prevHolo = holo;
                    return;
                }
                prevHolo = holo;
                // GAME time: frozen while paused and while suspended
                t += Time.deltaTime;
                // The radius is fixed (RevealRadius, set once in StartSession)
                if (!EnsureMaterial())
                {
                    // The session must still expire even if the material
                    // never appears (the asset loads with the Seaglide /
                    // Cyclops / map room interfaces). The expiry is based on
                    // the reveal completion, like the fade below.
                    if (t > RevealTime() + HoldTime + FadeTime + 10f)
                    {
                        EndSession("hologram material never found");
                    }
                    return;
                }
                SyncOverlays(true);
                float r = FrontRadius(t);
                ApplyUniforms(r);
                AddOverlaysUpTo(r);
                if (Time.time > nextReCollect)
                {
                    nextReCollect = Time.time + ReCollectInterval;
                    MergePending();
                }
                float hold = HoldTime;
                float fade = FadeTime;
                // The hold starts when the reveal front COMPLETES
                // (RevealTime), not at t=0 - otherwise a short hold (1.5 s)
                // would fade the map while the front is still traveling
                // (reveal takes 2.5 s at the blue wave calibration). A
                // re-armed session (t starts at RevealTime) then holds for
                // `hold` seconds before fading, as intended.
                float fadeStart = RevealTime() + hold;
                if (t > fadeStart)
                {
                    if (fade <= 0.01f)
                    {
                        EndSession("fade complete (fade disabled)");
                        return;
                    }
                    float f = (t - fadeStart) / fade;
                    if (f >= 1f)
                    {
                        EndSession("fade complete");
                        return;
                    }
                    // Fade the alpha (ApplyUniforms just set _Color to the
                    // glow color + transparency this same frame) - only the
                    // alpha is scaled, the RGB is preserved
                    var c = matInstance.GetColor("_Color");
                    c.a *= 1f - f;
                    matInstance.SetColor("_Color", c);
                }
            }

            // The overlays mirror their source piece's display state: the
            // voxeland unloads chunks, LOD-toggles the renderers and re-pools
            // pieces while the session is running - without the sync the
            // hologram would ghost on terrain that is no longer drawn
            void SyncOverlays(bool active)
            {
                for (int i = overlays.Count - 1; i >= 0; i--)
                {
                    var o = overlays[i];
                    var srcMf = o.srcMf;
                    if ((Object)(object)srcMf == null)
                    {
                        RemoveOverlay(i);
                        continue;
                    }
                    bool shown = active
                               && (Object)(object)o.srcMr != null
                               && o.srcMr.enabled
                               && srcMf.gameObject.activeInHierarchy;
                    if ((Object)(object)o.mr != null)
                    {
                        o.mr.enabled = shown;
                    }
                }
            }

            void RemoveOverlay(int i)
            {
                var o = overlays[i];
                if ((Object)(object)o.go != null)
                {
                    Object.Destroy(o.go);
                }
                if ((Object)(object)o.srcMf != null)
                {
                    overlaid.Remove(o.srcMf);
                }
                overlays.RemoveAt(i);
            }

            float FrontRadius(float t)
            {
                float r = RevealSpeed * t;
                if (r > radius)
                {
                    r = radius;
                }
                return r;
            }

            bool EnsureMaterial()
            {
                if ((Object)(object)matInstance != null)
                {
                    return true;
                }
                if ((Object)(object)baseMaterial == null)
                {
                    // Throttled: the fallback scans ALL loaded assets
                    if (Time.time > nextMatRetry)
                    {
                        nextMatRetry = Time.time + ReCollectInterval;
                        baseMaterial = FindBaseMaterial();
                    }
                }
                if ((Object)(object)baseMaterial == null)
                {
                    if (!materialMissLogged)
                    {
                        materialMissLogged = true;
                        Mod.logger.LogError("[HoloMap] hologram material NOT found (no loaded material with shader 'FX/WBOIT-HoloMap') - the session is inert and will expire; it loads with the Seaglide / Cyclops / map room interfaces");
                    }
                    return false;
                }
                matInstance = Object.Instantiate<Material>(baseMaterial);
                matInstance.name = "SubmersedVR Sonar Holo Map";
                // The _Color (glow color + transparency) is set every frame in
                // ApplyUniforms from the live sliders - no need to capture the
                // game material's color here
                Mod.logger.LogInfo($"[HoloMap] material '{baseMaterial.name}' (shader {matInstance.shader.name}) active");
                return true;
            }

            static Material FindBaseMaterial()
            {
                var mw = Object.FindObjectsOfType<MiniWorld>();
                for (int i = 0; i < mw.Length; i++)
                {
                    var m = mw[i].hologramMaterial;
                    if ((Object)(object)m != null && m.shader != null && m.shader.name == HoloShaderName)
                    {
                        return m;
                    }
                }
                foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if ((Object)(object)mat == null)
                    {
                        continue;
                    }
                    if (mat.shader != null && mat.shader.name == HoloShaderName)
                    {
                        return mat;
                    }
                }
                return null;
            }

            void ApplyUniforms(float r)
            {
                matInstance.SetVector("_MapCenterWorldPos", origin);
                matInstance.SetFloat("_FadeRadius", r);
                // Frozen floor look (the user's final calibration, log .56):
                // relief flattened, scan pulse + wireframe softened, 50%
                // transparency so the floor blends into the screen wave
                matInstance.SetFloat("_FadeSharpness", 2f);
                matInstance.SetFloat("_MinShadingSmoothness", 9.8f);
                matInstance.SetFloat("_MaxShadingSmoothness", 10f);
                matInstance.SetFloat("_FresnelFade", 2f);
                matInstance.SetFloat("_FresnelPow", -0.5f);
                matInstance.SetFloat("_ScanIntensity", 0.25f);
                matInstance.SetFloat("_ScanFrequency", 0.05f);
                matInstance.SetFloat("_ScanSpeed", -0.05f);
                matInstance.SetFloat("_ScanWidth", 0.25f);
                matInstance.SetFloat("_WireframeIntensity", 0.2f);
                matInstance.SetFloat("_NoiseIntensity", 0.5f);
                matInstance.SetVector("_ColorStrength", new Vector4(2f, 2f, 2f, 1f));
                Color32 g = Settings.SonarPresetColor(Settings.SonarColor);
                matInstance.SetColor("_Color", new Color(g.r / 255f, g.g / 255f, g.b / 255f, 0.5f));
            }

            void AddOverlaysUpTo(float r)
            {
                int budget = AddBudgetPerFrame;
                while (budget > 0 && pendingIdx < pending.Count)
                {
                    var p = pending[pendingIdx];
                    if (p.dist > r + EdgeMargin)
                    {
                        break;
                    }
                    pendingIdx++;
                    var mf = p.mf;
                    if ((Object)(object)mf == null || !overlaid.Add(mf))
                    {
                        continue;
                    }
                    CreateOverlay(mf);
                    budget--;
                }
            }

            void MergePending()
            {
                var fresh = CollectPieces(origin, radius + EdgeMargin);
                var seen = new HashSet<MeshFilter>(overlaid);
                for (int i = pendingIdx; i < pending.Count; i++)
                {
                    seen.Add(pending[i].mf);
                }
                int added = 0;
                for (int i = 0; i < fresh.Count; i++)
                {
                    var f = fresh[i];
                    if (seen.Add(f.mf))
                    {
                        pending.Add(f);
                        added++;
                    }
                }
                if (added > 0)
                {
                    pending.Sort((a, b) => a.dist.CompareTo(b.dist));
                    pendingIdx = 0;
                }
            }

            static List<Piece> CollectPieces(Vector3 origin, float radius)
            {
                var list = new List<Piece>();
                var seen = new HashSet<MeshFilter>();
                var streamer = LargeWorldStreamer.main;
                if ((Object)(object)streamer != null && (Object)(object)streamer.land != null)
                {
                    var window = streamer.land.chunkWindow;
                    if (window != null && window.Length > 0)
                    {
                        for (int i = 0; i < window.Length; i++)
                        {
                            var st = window[i];
                            if (st == null || (Object)(object)st.chunk == null)
                            {
                                continue;
                            }
                            var chunk = st.chunk;
                            for (int f = 0; f < chunk.hiFilters.Count; f++)
                            {
                                AddPiece(chunk.hiFilters[f], origin, radius, seen, list);
                            }
                            for (int f = 0; f < chunk.loFilters.Count; f++)
                            {
                                AddPiece(chunk.loFilters[f], origin, radius, seen, list);
                            }
                            AddPiece(chunk.opaqueFilter, origin, radius, seen, list);
                        }
                        if (list.Count > 0)
                        {
                            list.Sort((a, b) => a.dist.CompareTo(b.dist));
                            return list;
                        }
                    }
                }
                var pieces = Object.FindObjectsOfType<TerrainChunkPiece>();
                for (int i = 0; i < pieces.Length; i++)
                {
                    var p = pieces[i];
                    if ((Object)(object)p == null)
                    {
                        continue;
                    }
                    if (p.pieceType != TerrainChunkPieceType.Layer && p.pieceType != TerrainChunkPieceType.Root)
                    {
                        continue;
                    }
                    AddPiece(p.meshFilter, origin, radius, seen, list);
                }
                list.Sort((a, b) => a.dist.CompareTo(b.dist));
                return list;
            }

            static void AddPiece(MeshFilter mf, Vector3 origin, float radius, HashSet<MeshFilter> seen, List<Piece> list)
            {
                if ((Object)(object)mf == null || !seen.Add(mf))
                {
                    return;
                }
                var m = mf.sharedMesh;
                if (m == null || m.vertexCount < 3)
                {
                    return;
                }
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled || !mf.gameObject.activeInHierarchy)
                {
                    return;
                }
                var lb = m.bounds;
                var wc = mf.transform.TransformPoint(lb.center);
                var ext = lb.extents * Mathf.Max(1f, mf.transform.lossyScale.x);
                float dist = DistToAABB(origin,
                    new Vector3(wc.x - ext.x, wc.y - ext.y, wc.z - ext.z),
                    new Vector3(wc.x + ext.x, wc.y + ext.y, wc.z + ext.z));
                if (dist > radius)
                {
                    return;
                }
                list.Add(new Piece { mf = mf, dist = dist });
            }

            void CreateOverlay(MeshFilter mf)
            {
                var src = mf.transform;
                var go = new GameObject("SubmersedVR HoloMap overlay");
                if (src.parent != null)
                {
                    go.transform.SetParent(src.parent, false);
                    go.transform.localPosition = src.localPosition;
                    go.transform.localRotation = src.localRotation;
                    go.transform.localScale = src.localScale;
                }
                else
                {
                    go.transform.position = src.position;
                    go.transform.rotation = src.rotation;
                    go.transform.localScale = src.localScale;
                }
                var omf = go.AddComponent<MeshFilter>();
                // SHARED mesh reference - zero extra geometry
                omf.sharedMesh = mf.sharedMesh;
                var omr = go.AddComponent<MeshRenderer>();
                omr.sharedMaterial = matInstance;
                omr.shadowCastingMode = ShadowCastingMode.Off;
                omr.receiveShadows = false;
                var srcMr = mf.GetComponent<MeshRenderer>();
                overlays.Add(new Overlay { go = go, mr = omr, srcMf = mf, srcMr = srcMr });
            }

            void EndSession(string reason)
            {
                hasSession = false;
                Mod.logger.LogInfo($"[HoloMap] session ended ({reason}), {overlays.Count} overlay(s)");
                ClearOverlays();
                if ((Object)(object)matInstance != null)
                {
                    Object.Destroy(matInstance);
                    matInstance = null;
                }
            }

            void ClearOverlays()
            {
                for (int i = 0; i < overlays.Count; i++)
                {
                    if ((Object)(object)overlays[i].go != null)
                    {
                        Object.Destroy(overlays[i].go);
                    }
                }
                overlays.Clear();
                overlaid.Clear();
            }

            public void Shutdown()
            {
                hasSession = false;
                ClearOverlays();
                if ((Object)(object)matInstance != null)
                {
                    Object.DestroyImmediate(matInstance);
                    matInstance = null;
                }
            }

            void OnDestroy()
            {
                if (!Mod.quitting)
                {
                    Shutdown();
                }
            }

            static float DistToAABB(Vector3 p, Vector3 mn, Vector3 mx)
            {
                float dx = Mathf.Max(mn.x - p.x, 0f);
                float dx2 = p.x - mx.x;
                if (dx2 > dx)
                {
                    dx = dx2;
                }
                float dy = Mathf.Max(mn.y - p.y, 0f);
                float dy2 = p.y - mx.y;
                if (dy2 > dy)
                {
                    dy = dy2;
                }
                float dz = Mathf.Max(mn.z - p.z, 0f);
                float dz2 = p.z - mx.z;
                if (dz2 > dz)
                {
                    dz = dz2;
                }
                return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }
    }
}
