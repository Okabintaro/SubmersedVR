// Simple world-anchored sonar wave for the on-screen sonar effect.
//
// A pale-blue front expands from the ping origin (the vehicle position at
// ping time, sent by SonarScreenWave). Every surface point starts glowing
// when the front reaches it and keeps glowing for a two-phase persistence
// trail (plateau + final fade, see below), revealing terrain relief through
// the G-buffer world normals (surfaces facing the vehicle are lit, others
// are dimmer but never black). A light distance attenuation keeps the far
// range visible.
//
// No grid. Stateless: the whole effect is a pure function of (worldPos, t) -
// no accumulation buffer, trivially safe in multi-pass stereo.
//
// v55 - PER-EYE unprojection (the inter-eye distance mismatch fix). v54 used
// the CENTER camera matrices (built-in unity_CameraToWorld /
// unity_CameraProjection) for the screen->world unprojection while the depth
// is per-eye: the front was anchored to the center eye in both eyes, a
// ~64mm ghost that only became visible with the slim 4 m band. v55 uses the
// same per-eye mechanism as the working stereo grid: C# (SonarScreenWave)
// sends _EyeC2W = GetStereoViewMatrix(eye).inverse and _EyeProjTerms from
// GetStereoProjectionMatrix(eye) (same capture as SonarScreenShaderFixV2,
// incl. the baked opposite-eye flip), so each eye unprojects with its own
// matrices and the wave sits on the terrain in both eyes.
//
// v56 - TWO-PHASE trail with three C# sliders (replacing the single v54/v55
// trail value): phase 1 = the plateau (SonarWaveTrailPlateau seconds, the
// glow darkens slowly from full to SonarWaveTrailLevel), phase 2 = the
// final fade (SonarWaveTrailFade seconds, from the plateau level down to
// EXACTLY 0 - no residual glow when the next ping re-anchors the wave
// (_WaveTime resets per ping in C#), which was the "abnormal flash").
// SonarWaveTrailCurve picks the shape WITHIN each phase: linear (off) or
// ease-in 1-x^2 (on: slow at the phase start, steep at the phase end).
//
// v54 - the clean wave (the v43-v52 "black wave" bug is solved):
//   * Root cause found: the global `const float3 WaveColor` did NOT hold
//     its source value in this bundle-compiled pipeline (v53 proved it:
//     every const-based color rendered black, every literal/uniform fine;
//     the witness squares stayed white = no overlay, the front branch ran).
//   * The wave color is now a REAL uniform (_WaveColor, lagoon
//     (0,0.9,1), set by SonarScreenWave) - no global const anywhere.
//   * Front: solid _WaveColor, thinner band (4 m, set by C#).
//   * Trail: lerp toward _WaveColor with a soft boost (x0.8): the peak is
//     ~0.7 of the color (no more saturation right behind the front).
 //   * All v53 diagnostic probes removed (swatches, witnesses, quadrants,
 //     magenta). The 7-segment version label (C# used to set _VersionCode)
 //     was removed for the public release.
//
// Build: Unity 2019.4, asset bundle named "sonar_resources" (flat, next to
// amplify_resources in StreamingAssets) containing this shader, platform
// Windows 64. The mod looks the shader up by name
// "SubmersedVR/SonarScreenWave".
Shader "SubmersedVR/SonarScreenWave"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        // _CameraDepthTexture / _CameraGBufferTexture2 are built-in: the
        // engine auto-binds them to the declared samplers; a material
        // property with a default would override that binding
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            // Built-in camera depth texture: the engine auto-binds its content,
            // but the sampler itself must be declared here (UnityCG.cginc does
            // NOT declare it)
            sampler2D _CameraDepthTexture;
            sampler2D _CameraGBufferTexture2;

            // Set per render call by SonarScreenWave
            uniform float3 _WaveOrigin;
            uniform float _WaveTime;     // seconds since ping start (<0 = off)
            uniform float _WaveSpeed;    // m/s (range / front duration)
            uniform float _WaveRange;    // m
            // Two-phase trail (all set per frame by SonarScreenWave from the
            // settings sliders)
            uniform float _WaveTrailStart;   // 0..1, level right behind the front
            uniform float _WaveTrailPlateau; // s, phase 1 (start -> level)
            uniform float _WaveTrailLevel;   // 0..1, level at the plateau end
            uniform float _WaveTrailFade;    // s, phase 2 (level -> 0)
            uniform float _WaveTrailCurve;   // 0 = linear, 1 = ease-in 1-x^2
            uniform float _WaveBand;     // m, soft front band width
            // v59: 0 = constant-speed front, 1 = accelerated (quadratic)
            // front - starts at 0 speed, ends at 2x average speed, crosses
            // the range at exactly t = sweep
            uniform float _WaveEaseFront;
            // v59: relief shading floor (0..0.35) - darkens the backlit
            // slopes; the crest max stays 1.0 (never brighter)
            uniform float _WaveReliefMin;
            // v60: 0 = classic diffuse (toward the ping bright), 1 = radar
            // mode (inverted fresnel: the grazing rims orient -> 0 glow)
            uniform float _WaveRadar;
            // v61 radar curve: rim glow = pow(1-orient, p) - 1 = linear,
            // <1 = broad glow, >1 = narrow silhouette rim
            uniform float _WaveRadarCurve;
            // v64 distance fade: the glow is FULL below this distance (m),
            // then fades linearly to _WaveAttenEnd at d = _WaveRange
            uniform float _WaveAttenStart;
            // v64: the fade level (0..1) reached at d = _WaveRange -
            // applied to the front AND the trail
            uniform float _WaveAttenEnd;
            // v65/v67 fade curve exponent p: fade = 1 - (1-end) * (u^p),
            // u goes 0 at the start distance to 1 at the max range. p=1
            // linear. p>1 (2 = squared) keeps the glow bright longer and
            // drops it fast near the edge (75% left at half range vs 50%).
            // p<1 (0.5 = square root) darkens faster with distance (~30%
            // left at half range). Slider range 0.1..1 (v67)
            uniform float _WaveAttenCurve;
            // v68: GLOBAL fade mode (holo map only) - the whole revealed
            // disc fades together (no comet following the front), synced
            // with the 3D holo floor. 0 = comet trail (blue wave mode),
            // 1 = global fade (set by C# only in the holo map mode)
            uniform float _WaveGlobalFade;
            // v68: total fade duration (s) - auto-calibrated by C# to the
            // 3D holo floor's fade (SonarHoloMapFade) so both layers
            // disappear at the same instant
            uniform float _WaveFadeTotal;
            // v68: hold-at-full duration (s) right after the reveal
            uniform float _WavePlateauTime;
            // v68: final rapid-fade duration (s); the gradual transparency
            // increase fills the rest of _WaveFadeTotal
            uniform float _WaveRapidTime;

            // Set once by SonarScreenWave: the wave color (lagoon
            // (0,0.9,1)). A real uniform - v43-v52 proved that a global
            // const in this bundle-compiled pipeline does NOT hold its
            // source value (every const-based color rendered black).
            uniform float3 _WaveColor;

            // Set per render call by SonarScreenWave (same capture as the
            // stereo grid driver): the PER-EYE camera-to-world matrix and
            // projection terms (p00, p11, p02, p12) of the eye currently
            // being rendered - the screen->world unprojection must use them
            // (the depth is per-eye; the built-in matrices are the center
            // eye's and produced the inter-eye distance mismatch)
            uniform float4x4 _EyeC2W;
            uniform float4 _EyeProjTerms;

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Vertex stage matching the game's original "Image Effects/Sonar"
            // shader exactly (source extracted from the game's shader asset):
            // full MVP transform + the blit mesh's own UVs (its input
            // channels are POSITION + TEXCOORD0, confirmed in the asset
            // metadata).
            v2f vert(AppData v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                float4 r0, r1, r3;

                // Source image (the pass-through base)
                r3.xyzw = tex2D(_MainTex, i.uv).xyzw;

                // No active wave: clean pass-through
                if (_WaveTime < 0.0)
                {
                    return r3;
                }

                // --- screen -> world (PER-EYE matrices from C#, v55) ---
                r0.x = _ProjectionParams.y * _ProjectionParams.z;
                r0.yz = _ProjectionParams.zy + -_ProjectionParams.yz;
                r1.xyzw = tex2D(_CameraDepthTexture, i.uv).xyzw;
                r0.w = 1 + -r1.x;
                r0.yz = r0.ww * r0.yz + _ProjectionParams.yz;
                r0.x = r0.x / r0.z;
                r0.y = r0.y + -r0.x;
                r0.x = unity_OrthoParams.w * r0.y + r0.x;
                r0.y = 1 + -r0.x;
                r0.y = unity_OrthoParams.w * r0.y + r0.x;

                // Per-eye projection terms (p00, p11, p02, p12), same
                // fields the built-in matrix would provide but for the eye
                // currently being rendered (the depth below is per-eye too)
                float2 pOffset = _EyeProjTerms.zw;
                float2 pScale = float2(_EyeProjTerms.x, _EyeProjTerms.y);
                r1.xy = i.uv * float2(2, 2) + float2(-1, -1);
                r1.zw = -pOffset + r1.xy;
                r1.zw = r1.zw / pScale;
                r0.yz = r1.zw * r0.yy;

                float3 viewPos = float3(r0.y, r0.z, -r0.x);
                // Per-eye camera-to-world (the grid does exactly this): the
                // wave sits on the terrain in BOTH eyes
                float3 worldPos = mul(_EyeC2W, float4(viewPos, 1.0)).xyz;

                // Distance from the ping origin
                float d = distance(worldPos, _WaveOrigin);

                // Beyond the range: clean pass-through (no wave out there)
                if (d > _WaveRange)
                {
                    return r3;
                }

                // --- front + persistence trail ---
                float frontR = _WaveTime * _WaveSpeed;
                float tArr = d / max(_WaveSpeed, 0.0001);
                // v59 accelerated front (legacy style): quadratic - starts
                // at 0 speed, ends at 2x the average speed, crosses the
                // range at exactly t = sweep (sweep = _WaveRange /
                // _WaveSpeed, so no separate uniform: frontR = v^2*t^2/range,
                // tArr = sqrt(d*range)/v). Only the front's position and
                // each point's arrival time change - the trail profile
                // (plateau/fade, a function of local age) is untouched
                if (_WaveEaseFront > 0.5)
                {
                    frontR = _WaveSpeed * _WaveSpeed * _WaveTime * _WaveTime / max(_WaveRange, 0.0001);
                    tArr = sqrt(d * max(_WaveRange, 0.0001)) / max(_WaveSpeed, 0.0001);
                }
                float front = 1.0 - smoothstep(0.0, max(_WaveBand, 0.0001), abs(d - frontR));
                float trail = 0.0;
                float reveal = 0.0;
                if (_WaveGlobalFade > 0.5)
                {
                    // v68 GLOBAL fade (holo map mode): every revealed point
                    // shares the same glow level G(_WaveTime), so the whole
                    // disc fades together (no comet following the front).
                    // G is 1 during the reveal, then a plateau (hold at
                    // full) -> a gradual transparency increase -> a rapid
                    // drop to 0, ending at sweep + _WaveFadeTotal (auto-
                    // calibrated to the 3D holo floor's fade so both layers
                    // disappear together). The reference t0 is the ping
                    // launch (_WaveTime = t - t0) - the fade no longer
                    // depends on each point's front arrival
                    float sweepT = _WaveRange / max(_WaveSpeed, 0.0001);
                    float ft = _WaveTime - sweepT;
                    float g = 1.0;
                    if (ft >= 0.0)
                    {
                        float total = max(_WaveFadeTotal, 0.0001);
                        float p = max(_WavePlateauTime, 0.0);
                        float r = max(_WaveRapidTime, 0.0);
                        float grad = max(total - p - r, 0.0);
                        float endL = saturate(_WaveTrailLevel);
                        if (ft >= total)
                        {
                            g = 0.0;
                        }
                        else if (ft <= p)
                        {
                            g = 1.0;
                        }
                        else if (ft <= p + grad)
                        {
                            float x = grad > 0.0001 ? (ft - p) / grad : 1.0;
                            g = 1.0 - (1.0 - endL) * x;
                        }
                        else
                        {
                            float x = r > 0.0001 ? (ft - p - grad) / r : 1.0;
                            g = endL * (1.0 - x);
                        }
                    }
                    float band = max(_WaveBand, 0.0001);
                    float revealed = 1.0 - smoothstep(frontR, frontR + band, d);
                    trail = saturate(_WaveTrailStart) * revealed;
                    reveal = g * max(front, trail);
                }
                else
                {
                    // v57 two-phase per-point trail (blue wave mode, the
                    // finalized comet behavior - unchanged):
                    //   phase 1 (0 .. _WaveTrailPlateau): _WaveTrailStart -> _WaveTrailLevel
                    //   phase 2 (.. + _WaveTrailFade):     _WaveTrailLevel -> 0
                    // Exactly 0 at the end of the window - the wave is fully
                    // attenuated before the next ping re-anchors it (no
                    // residual flash). _WaveTrailCurve = 1 applies an ease-in
                    // (1-x^2) WITHIN each phase (slow at the phase start,
                    // steeper at the phase end); 0 = linear
                    float trailAge = _WaveTime - tArr;
                    if (trailAge > 0.0)
                    {
                        float t1 = max(_WaveTrailPlateau, 0.0);
                        float t2 = max(_WaveTrailFade, 0.0);
                        float lvl = saturate(_WaveTrailLevel);
                        float st = saturate(_WaveTrailStart);
                        if (trailAge < t1 + t2)
                        {
                            if (trailAge <= t1)
                            {
                                float x = t1 > 0.0 ? trailAge / t1 : 1.0;
                                float drop = _WaveTrailCurve > 0.5 ? x * x : x;
                                trail = st - (st - lvl) * drop;
                            }
                            else
                            {
                                float x = t2 > 0.0 ? (trailAge - t1) / t2 : 1.0;
                                float drop = _WaveTrailCurve > 0.5 ? x * x : x;
                                trail = lvl * (1.0 - drop);
                            }
                        }
                    }
                    reveal = max(front, trail);
                }
                if (reveal <= 0.001)
                {
                    return r3;
                }

                // --- relief: G-buffer world normal vs direction toward the sub ---
                float relief = 1.0;
                float3 nRaw = tex2D(_CameraGBufferTexture2, i.uv).xyz;
                float3 nEnc = nRaw * 2.0 - 1.0;
                float nLen = length(nEnc);
                // the nLen guard kills the NaN case nEnc=(0,0,0) (buffer
                // holding the neutral gray 0.5) that normalize() would
                // produce; the length(nRaw) gate keeps the (likely) empty
                // buffer (0,0,0) at relief = 1.0 exactly as before
                if (length(nRaw) > 0.01 && nLen > 0.001)
                {
                    float3 n = nEnc / nLen;
                    float3 ldir = normalize(_WaveOrigin - worldPos);
                    float orient = dot(n, ldir);
                    // v59: slider floor (default 0.35 = the old hard-coded
                    // value); the max stays exactly 1.0 - never brighter
                    float rfMin = saturate(_WaveReliefMin);
                    if (_WaveRadar > 0.5)
                    {
                        // v61 radar mode: VISIBLE faces only have orient in
                        // [0,1] (a face pointing away is never rendered),
                        // so the dead orient<0 half of the v60 inverted
                        // formula is dropped - the full F..1 range is used
                        // over the visible half: the rim (orient -> 0, the
                        // silhouette) = 1.0, the face toward the ping
                        // (orient -> 1) = F. pow curve: <1 = broad glow,
                        // >1 = narrow silhouette rim, 1 = linear
                        float rim = 1.0 - saturate(orient);
                        relief = rfMin + (1.0 - rfMin) * pow(rim, max(_WaveRadarCurve, 0.05));
                    }
                    else
                    {
                        // v62 classic mode, remapped: visible faces only
                        // have orient in [0,1], so use the full F..1 range
                        // over the visible half - face toward the ping
                        // (orient -> 1) = 1.0, grazing (orient -> 0) = F.
                        // The old 0.5+0.5*orient mapping wasted the
                        // orient<0 half (never rendered), so the visible
                        // floor was stuck at F+(1-F)/2
                        relief = rfMin + (1.0 - rfMin) * saturate(orient);
                    }
                }

                // v64/v65 distance fade: FULL up to _WaveAttenStart, then
                // down to _WaveAttenEnd at d = _WaveRange with a curve
                // exponent p (v65): fade = 1 - (1-end) * u^p, u = 0 at the
                // start distance, 1 at the range. p=1 linear, p=2 squared
                // (25% left at half range instead of 50%), p=0.5 square
                // root (softer). Applied to the front AND the trail - since
                // the glow is a lerp toward _WaveColor, fading it =
                // transparency (the scene shows through more at range)
                float span = max(_WaveRange - _WaveAttenStart, 0.0001);
                float u = saturate((d - _WaveAttenStart) / span);
                float fade = 1.0 - pow(u, max(_WaveAttenCurve, 0.05)) * (1.0 - _WaveAttenEnd);

                // The wave: the front band lerps toward _WaveColor with
                // the distance fade (v64: no longer solid - it gets
                // transparent at range); the trail behind it lerps with
                // intensity = reveal x relief x fade - at start level 1.0
                // the trail peaks exactly as bright as the front.
                float intensity = saturate(reveal * relief * fade);
                if (front > 0.5)
                {
                    r3.rgb = lerp(r3.rgb, _WaveColor, saturate(fade));
                }
                else
                {
                    r3.rgb = lerp(r3.rgb, _WaveColor, intensity);
                }
                r3.rgb = saturate(r3.rgb);
                return r3;
            }
            ENDCG
        }
    }
}
