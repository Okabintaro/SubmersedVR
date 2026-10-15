using UnityEngine;

namespace SubmersedVR
{
    // 3D world-locked sphere for the precursor teleport (the VR fix,
    // Settings.FixTeleportEffect).
    //
    // Replaces the 2D head-locked swirl (TeleportScreenFX) with a 3D
    // sphere that envelops the player. Comfort: a WORLD-LOCKED 3D object
    // (no full-screen head-following animation) is comfortable, unlike
    // the 2D swirl.
    //
    // Mechanics:
    // - The sphere is PARENTED to the player (position follows the t=1s
    //   teleport snap), but its ORIENTATION is WORLD-LOCKED: the
    //   body rotation at spawn is captured (frozenWorldRot) and forced
    //   every frame (localRotation = body^-1 x frozen), so the sphere's
    //   world orientation is constant from spawn to destroy regardless
    //   of body rotation. The sphere is a SCENERY element: it does not
    //   follow the body's rotation.
    // - Material: the game's own door effect material (the green
    //   water-door of the teleport room, VFXPrecursorTeleporter.portal-
    //   Renderer, shader FX/WBOIT-PrecursorPortal: a tiling noise texture
    //   + scroll + slow rotation + radial fade) is COPIED (new Material)
    //   and applied to the sphere. The copy is owned by us: the VFX On-
    //   Destroy destroys the game's live instance, not our copy, and
    //   driving our fade does not touch the door in the room. The fade
    //   mirrors the game's own door fade: the same uniform (_RadialFade)
    //   is driven here from fx.amount.
    // - Pattern center: the door shader's radial pattern focus is
    //   the texture center (UV 0.5, 0.5). At spawn the mesh vertex
    //   closest to that UV is found, and the exact yaw that rotates its
    //   direction to local -Z (the player's forward) is computed
    //   (atan2) and applied to the vertices. Deterministic: no
    //   hardcoded angle, survives mesh/shader changes.
    // - World-lock: applied in Update AND LateUpdate. The game's
    //   teleport warp (t=1s) and arrival snap are both Update-phase
    //   (coroutine steps / Invoke callbacks), so the LateUpdate
    //   re-apply always lands after them and before render.
    // - Fallback (no door material found, e.g. a future game version):
    //   Sprites/Default (alpha blended) + the game's swirl
    //   "nerves" texture (static), the color lerps black -> green (fade
    //   in) and the alpha goes TeleportFadeInAlpha -> 0 (fade out).
    // - Known visual trade-offs (accepted): (a) the game's VRUtil
    //   .Recenter() at arrival shifts the camera, and the world-locked
    //   sphere does not follow it (it stays put in the world); (b) the
    //   t=1s body yaw snap changes where the frozen pattern focus
    //   appears in the view for the rest of the cycle.
    //
    // No bundle / no custom shader: CreatePrimitive(Sphere) + a REVERSED
    // mesh (the interior faces become the front) so a Cull Back unlit
    // shader shows the inside.
    //
    // The sphere is rendered by the main camera (the player's layer), NOT by
    // the UI camera -> it never shows up in the PDA screen.
    static class TeleportSphere3D
    {
        const float Radius = 3.0f;            // interior radius (m)
        const float MonoR = 0.20f, MonoG = 1.0f, MonoB = 0.55f; // precursor green (fallback only)
        const float FadeInAlpha = 0.5f;       // fallback path: opacity at fade-in peak
        const float AliveSafety = 30.0f;      // a cycle is ~10 s

        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        static readonly int RadialFadeID = Shader.PropertyToID("_RadialFade");
        static bool yawLogged;

        static GameObject sphereGo;
        static Material sphereMat;
        static bool usingDoorMat;
        static double spawnTime;
        static float prevAmount;
        static bool hasPrevAmount;
        static Texture2D whiteTexture;

        // The body's world rotation at spawn = the sphere's permanent
        // world orientation (world-locked scenery element)
        static Quaternion frozenWorldRot;

        static Texture2D WhiteTexture()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTexture.name = "TeleportSphereWhite";
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }
            return whiteTexture;
        }

        // The game's swirl "nerves" texture (grabbed at runtime from
        // TeleportScreenFX.mat in OnPreRender). Fallback path only: the
        // texture is static, so the sphere rotates around local Z to make
        // it look like it scrolls (the vortex effect). null = fallback to
        // white.
        static Texture swirlTexture;

        public static void SetSwirlTexture(Texture tex)
        {
            if (swirlTexture == null && tex != null)
            {
                swirlTexture = tex;
            }
        }

        // The game's live door effect material (the green water-door of
        // the teleport room). The VFX is spawned at scene load (Precursor-
        // Teleporter.Start) so it exists for the whole time the player is
        // in the room. Nearest door wins (the player stands in front of the
        // one being teleported through). The CALLER copies it (new
        // Material) so it owns the result.
        static Material TryGetDoorMaterial()
        {
            try
            {
                Vector3 p = Player.main != null ? Player.main.transform.position : Vector3.zero;
                Material best = null;
                float bestDist = float.MaxValue;
                var vfxs = Object.FindObjectsOfType<VFXPrecursorTeleporter>();
                for (int i = 0; i < vfxs.Length; i++)
                {
                    var v = vfxs[i];
                    if (v == null || v.portalRenderer == null)
                    {
                        continue;
                    }
                    var m = v.portalRenderer.material;
                    if (m == null)
                    {
                        continue;
                    }
                    float d = (v.portalRenderer.transform.position - p).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = m;
                    }
                }
                return best;
            }
            catch (System.Exception e)
            {
                Mod.logger.LogWarning("[TeleportSphere] door material lookup failed: " + e.Message);
                return null;
            }
        }

        // Create the sphere (idempotent). Called from the module's cycle-start
        // and on-demand from UpdateSphere (mid-teleport hot-swap). `amount`
        // is the current fx.amount, so the first rendered frame starts at
        // the right fade level (not at the door's live _RadialFade = 1,
        // which would pop one full frame).
        public static void Spawn(float amount)
        {
            if (sphereGo != null)
            {
                return;
            }
            var player = Player.main;
            if (player == null)
            {
                Mod.logger.LogInfo("[TeleportSphere] spawn skipped: Player.main NULL");
                return;
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SubmersedVR_TeleportSphere";
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col); // no player/sphere collision
            }
            // reversed mesh -> the interior faces become the front, so a
            // Cull Back unlit shader renders the inside; the vertices are
            // additionally yawed so the door pattern's focus faces the
            // player's forward (computed at runtime, see below)
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var src = mf.sharedMesh;
                var m = new Mesh();
                m.name = "TeleportSphereReversed";
                // Find the mesh vertex closest to UV (0.5, 0.5) = the
                // texture center = the door shader's radial pattern focus.
                // Compute the yaw that rotates its direction to local -Z
                // (the player's forward). Deterministic: no hardcoded angle.
                var uvs = src.uv;
                int focusIdx = 0;
                float bestUvDist = float.MaxValue;
                for (int i = 0; i < uvs.Length; i++)
                {
                    float du = uvs[i].x - 0.5f, dv = uvs[i].y - 0.5f;
                    float d2 = du * du + dv * dv;
                    if (d2 < bestUvDist) { bestUvDist = d2; focusIdx = i; }
                }
                Vector3 fv = src.vertices[focusIdx];
                float yawDeg = Mathf.Atan2(fv.x, -fv.z) * Mathf.Rad2Deg;
                var yaw = Quaternion.Euler(0f, yawDeg, 0f);
                var verts = src.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] = yaw * verts[i];
                }
                m.vertices = verts;
                m.uv = src.uv;
                int[] tris = src.triangles;
                int[] rev = new int[tris.Length];
                for (int i = 0; i < tris.Length; i += 3)
                {
                    rev[i] = tris[i];
                    rev[i + 1] = tris[i + 2];
                    rev[i + 2] = tris[i + 1];
                }
                m.triangles = rev;
                mf.mesh = m;
                if (!yawLogged)
                {
                    yawLogged = true;
                    Mod.logger.LogInfo("[TeleportSphere] focus yaw = " + yawDeg.ToString("F1", Inv) + "\u00b0 (focus UV dist\u00b2 = " + bestUvDist.ToString("F4", Inv) + ")");
                }
            }
            // Try to reuse the game's door effect material (the green
            // water-door). Copy it: we own the copy, and the VFX OnDestroy
            // destroys the game's live instance, not our copy
            var door = TryGetDoorMaterial();
            string matDesc;
            if (door != null)
            {
                sphereMat = new Material(door);
                usingDoorMat = true;
                matDesc = "door material " + sphereMat.name + " (shader " + sphereMat.shader.name + ")";
                // start at the current fade level (not the door's live
                // _RadialFade = 1, which would pop one full frame)
                sphereMat.SetFloat(RadialFadeID, Mathf.Clamp01(amount));
            }
            else
            {
                usingDoorMat = false;
                // Fallback: Sprites/Default is alpha blended and
                // exists in-game (the solid Unlit/Color ignores alpha); the
                // tint comes from the _Color (the white 1x1 mainTexture)
                var shader = Shader.Find("Sprites/Default");
                bool spriteShader = shader != null;
                if (!spriteShader)
                {
                    shader = Shader.Find("Unlit/Transparent");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }
                if (shader == null)
                {
                    shader = ShaderManager.preloadedShaders.DebugDisplaySolid;
                }
                sphereMat = new Material(shader);
                if (spriteShader)
                {
                    // the swirl "nerves" texture (recalls the vortex);
                    // fallback to white if it was not grabbed yet
                    sphereMat.mainTexture = swirlTexture != null ? swirlTexture : WhiteTexture();
                }
                // start black (UpdateSphere lerps the color black -> green to
                // fx.amount during the fade in; the opaque black masks the
                // raw world)
                sphereMat.color = Color.black;
                matDesc = "fallback shader " + shader.name;
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = sphereMat;
            }
            // reset the fade direction tracking (the first frame is "fading in")
            prevAmount = 0f;
            hasPrevAmount = false;
            // parent to the player (position follows the snap), centered,
            // radius R; capture the body's world rotation for the
            // world-locked orientation.
            // Known limitation: vehicle teleports (Seamoth/Crawler through
            // a precursor door) move the vehicle, not the player root; the
            // sphere may not envelop the view. The swirl removal (the
            // comfort fix) still applies regardless.
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            frozenWorldRot = player.transform.rotation;
            go.transform.localScale = Vector3.one * (Radius * 2f);
            go.layer = player.gameObject.layer;
            sphereGo = go;
            spawnTime = Time.time;
            Mod.logger.LogInfo("[TeleportSphere] spawned (r=" + Radius + "m, " + matDesc + ", parent=player, layer=" + player.gameObject.layer + ")");
        }

        // Called from the module's cycle-end (fx.amount reached 0 -> the
        // sphere is already faded out, destroy now, no pop).
        public static void OnCycleEnd()
        {
            if (sphereGo != null)
            {
                DestroySphere();
            }
        }

        static void DestroySphere()
        {
            if (sphereGo != null)
            {
                Object.Destroy(sphereGo);
                sphereGo = null;
            }
            if (sphereMat != null)
            {
                // our copy (door material) or our material (fallback):
                // safe to destroy either way
                Object.Destroy(sphereMat);
                sphereMat = null;
            }
            usingDoorMat = false;
            frozenWorldRot = Quaternion.identity;
            Mod.logger.LogInfo("[TeleportSphere] destroyed");
        }

        // Called every frame from TeleportMonoGuard (on the FX/main camera).
        public static void UpdateSphere(TeleportScreenFX fx)
        {
            bool fixEnabled = Settings.FixTeleportEffect;
            float amount = fx != null ? fx.amount : 0f;
            if (sphereGo == null)
            {
                // on-demand spawn: the user enabled the fix
                // mid-teleport (the cycle-start already passed)
                if (fixEnabled && amount > 0f)
                {
                    Spawn(amount);
                }
                return;
            }
            if (!fixEnabled)
            {
                DestroySphere(); // the user switched away from the sphere mode
                return;
            }
            // safety: a teleport cycle is ~10 s; if the sphere has been
            // alive > 30 s, the cycle is stuck -> force-end it (resets the
            // cycle tracking so the next teleport can spawn a new sphere)
            if (Time.time - spawnTime > AliveSafety)
            {
                Mod.logger.LogInfo("[TeleportSphere] safety: sphere alive > " + AliveSafety + " s (stuck cycle?), force-ending");
                TeleportScreenFXVRFix.ForceCycleEnd();
                return;
            }
            if (sphereMat == null)
            {
                return;
            }
            float a = Mathf.Clamp01(amount);
            // fade direction: the amount increases (fade in) or decreases
            // (fade out); used by the fallback color path
            bool fadingIn = !hasPrevAmount || a > prevAmount;
            prevAmount = a;
            hasPrevAmount = true;
            if (usingDoorMat)
            {
                // the game's own door fade: the same uniform
                // (VFXPrecursorTeleporter.Update drives it 0 -> 1 -> 0
                // during its own fade in/out), mirrored on fx.amount
                sphereMat.SetFloat(RadialFadeID, a);
            }
            else
            {
                // Fallback
                float peakAlpha = FadeInAlpha;
                Color green = new Color(MonoR, MonoG, MonoB, 1f);
                if (fadingIn)
                {
                    // fade in: the color lerps black -> green, the alpha
                    // goes 1 -> peakAlpha (the opaque black masks the raw
                    // world at the start, then the green appears and
                    // becomes translucent)
                    Color c = Color.Lerp(Color.black, green, a);
                    c.a = 1f - (1f - peakAlpha) * a;
                    sphereMat.color = c;
                }
                else
                {
                    // fade out (or peak): the color stays green, the alpha
                    // goes peakAlpha -> 0 (the green becomes transparent,
                    // the raw world shows through)
                    sphereMat.color = new Color(MonoR, MonoG, MonoB, peakAlpha * a);
                }
            }
            ApplyWorldLock();
        }

        // Called from the guard's LateUpdate: re-apply the world-lock
        // AFTER the game's managed late update (where the end-of-teleport
        // body snap may happen), so a same-frame correction is applied
        // before render.
        public static void ApplyAnchor()
        {
            ApplyWorldLock();
        }

        // Force the sphere's world orientation to frozenWorldRot
        // (captured at spawn) regardless of the body's rotation:
        // localRotation = body^-1 x frozenWorldRot
        //  -> world rotation = body x (body^-1 x frozen) = frozen (constant)
        static void ApplyWorldLock()
        {
            if (sphereGo == null)
            {
                return;
            }
            var bodyTf = sphereGo.transform.parent;
            if (bodyTf == null)
            {
                return;
            }
            sphereGo.transform.localRotation = Quaternion.Inverse(bodyTf.rotation) * frozenWorldRot;
        }
    }
}
