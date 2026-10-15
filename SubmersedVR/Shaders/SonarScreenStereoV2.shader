// Stereo-corrected replacement for the game's "Image Effects/Sonar" screen
// effect (the "legacy (3D fixed)" option).
//
// A register-faithful port of the game's pass (the 2018 3DMigoto
// replacement shader of that same pass, byte-identical in the
// 2018/2021/2024 fix archives), with ONE correction: the world position
// reconstruction is done PER EYE.
//
// Why: the game reconstructs every pixel's world position from the
// per-eye depth buffer with the CENTER camera matrices, so in VR the
// grid's vanishing point ends up between the two eyes (a double grid that
// follows the head). The fix feeds the reconstruction the per-eye
// projection terms (p00, p11, p02, p12 of Camera.GetStereoProjectionMatrix,
// in _EyeProjTerms) and the per-eye view matrix inverse (the inverse of
// Camera.GetStereoViewMatrix, in _EyeC2W). The depth linearization stays
// EXACTLY as the game's (near/far clip planes are shared between the two
// eyes).
//
// C# (SonarScreenShaderFixV2) sets _EyeC2W + _EyeProjTerms on every
// OnRenderImage call. _SonarPingDistance is set globally by the game
// (SonarScreenFX).
//
// Build: Unity 2019.4, asset bundle named "sonar_resources" (flat, next to
// amplify_resources in StreamingAssets) containing this shader, platform
// Windows 64. The mod looks the shader up by name
// "SubmersedVR/SonarScreenStereoV2".
Shader "SubmersedVR/SonarScreenStereoV2"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        // _CameraDepthTexture / _CameraGBufferTexture2 are built-in: the
        // engine auto-binds them to the declared samplers; a material
        // property with a default would override that binding
        _UseNormalGate ("Use GBuffer2 Normal Gate", Float) = 1.0
        _EyeProjTerms ("Eye Projection Terms (p00 p11 p02 p12)", Vector) = (0, 0, 0, 0)
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

            // Set globally by the game (SonarScreenFX, pingDistanceShaderID)
            uniform float _SonarPingDistance;
            uniform float _UseNormalGate;
            // Per-eye projection terms (p00, p11, p02, p12), set every frame
            // by C# from GetStereoProjectionMatrix(eye)
            uniform float4 _EyeProjTerms;
            // 4x4 matrix set with Material.SetMatrix; no Properties entry
            // needed (the uniform is registered by the shader)
            uniform float4x4 _EyeC2W;

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
                float4 r0, r1, r2, r3;

                // --- linearized depth (verbatim from the game shader;
                // near/far are shared between the eyes, keep it untouched) ---
                r0.x = _ProjectionParams.y * _ProjectionParams.z;
                r0.yz = _ProjectionParams.zy + -_ProjectionParams.yz;
                r1.xyzw = tex2D(_CameraDepthTexture, i.uv).xyzw;
                r0.w = 1 + -r1.x;
                r0.yz = r0.ww * r0.yz + _ProjectionParams.yz;
                r0.x = r0.x / r0.z;

                float depth = r0.x;

                r0.y = r0.y + -r0.x;
                r0.x = unity_OrthoParams.w * r0.y + r0.x;
                r0.y = 1 + -r0.x;
                r0.y = unity_OrthoParams.w * r0.y + r0.x;

                // --- projection terms: PER EYE (sent by C# from
                // GetStereoProjectionMatrix) ---
                float2 pOffset = _EyeProjTerms.zw;
                float2 pScale = float2(_EyeProjTerms.x, _EyeProjTerms.y);

                // --- NDC -> view XY ---
                r1.xy = i.uv * float2(2, 2) + float2(-1, -1);
                r1.zw = -pOffset + r1.xy;
                r1.zw = r1.zw / pScale;
                r2.xy = r1.xy / pScale;
                r0.yz = r1.zw * r0.yy;

                // --- view -> world: PER EYE _EyeC2W (=
                // GetStereoViewMatrix(eye).inverse, set by C#) ---
                float3 viewPos = float3(r0.y, r0.z, -r0.x);
                r0.xyz = mul(_EyeC2W, float4(viewPos, 1.0)).xyz;

                // --- world-space grid (verbatim) ---
                r0.xyz = _Time.yyy * float3(0.300000012, 0.300000012, 0.300000012) + r0.xyz;
                r0.xyz = float3(1.33333337, 1.33333337, 1.33333337) * r0.xyz;
                r0.xyz = frac(r0.xyz);
                r0.x = min(r0.x, r0.y);
                r0.x = min(r0.x, r0.z);
                r0.x = 1 + -r0.x;
                r0.x = log2(r0.x);
                r0.x = 7 * r0.x;
                r0.x = exp2(r0.x);

                // --- facing gate: per-pixel ray direction (r2.xy is
                // ndc/scale from above, so it follows the eye) ---
                r2.z = -1;
                r0.z = dot(r2.xyz, r2.xyz);
                r0.z = rsqrt(r0.z);
                r1.xyz = r2.xyz * r0.zzz;
                r0.z = dot(-r1.xyz, -r1.xyz);
                r0.z = rsqrt(r0.z);
                r1.xyz = -r1.xyz * r0.zzz;
                r2.xyzw = tex2D(_CameraGBufferTexture2, i.uv).xyzw;
                r2.xyz = float3(-0.5, -0.5, -0.5) + r2.xyz;
                r0.z = dot(r1.xyz, r2.xyz);
                r0.z = abs(r0.z) * abs(r0.z);
                r0.yz = float2(2, 6) * r0.xz;
                r0.z = min(1, r0.z);
                r0.z = lerp(1.0, r0.z, _UseNormalGate);
                r1.x = r0.y * r0.z;
                r0.y = -r0.z * 0.5 + 1;

                // --- ping wave (verbatim) ---
                r0.z = 0.999899983 + -r0.w;
                r0.w = log2(r0.w);
                r0.w = 2000 * r0.w;
                r0.w = exp2(r0.w);
                r0.w = min(1, r0.w);
                r0.z = ceil(r0.z);
                r1.w = _SonarPingDistance * 2 + -r0.w;
                r2.x = 100 * r1.w;
                r1.w = saturate(r1.w * 4 + -2.5);
                r1.w = 1 + -r1.w;
                r2.x = saturate(r2.x);
                r2.x = r2.x * r1.w;
                r2.y = 0.899999976 + -r0.w;
                r2.x = r2.x * r2.y;
                r2.y = 1000 * r0.w;
                r2.y = min(1, r2.y);
                r2.x = r2.x * r2.y;
                r0.z = saturate(r2.x * r0.z);
                r0.xy = r0.xy * r0.zz;
                r0.y = r0.y * r0.w;
                r0.w = r0.w + r0.z;
                r0.w = min(1, r0.w);
                r2.xyz = r0.www * float3(0, -0.100000001, -0.800000012) + float3(0, 0.100000001, 0.800000012);
                r0.w = r1.w * r0.z;
                r0.w = -r0.w * 0.5 + 1;
                r1.w = r1.w * r0.w;
                r1.w = log2(r1.w);
                r1.w = 20 * r1.w;
                r1.w = exp2(r1.w);
                r1.w = r1.w * r0.z;
                r0.y = r1.w * 10 + r0.y;
                r2.xyz = r0.yyy * r2.xyz;

                // --- final blend (verbatim) ---
                r3.xyzw = tex2D(_MainTex, i.uv).xyzw;
                r0.yzw = r3.xyz * r0.www + r2.xyz;
                r1.yz = float2(0, 0);
                r1.xyz = r1.xyz + -r0.yzw;
                r3.xyz = r0.xxx * r1.xyz + r0.yzw;
                r3.w = r3.w;
                return r3;
            }
            ENDCG
        }
    }
}
