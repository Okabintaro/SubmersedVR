extern alias SteamVRRef;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SteamVRRef::Valve.VR;
using UnityEngine;

namespace SubmersedVR;

public class SwimmingController : MonoBehaviour
{
    private const float MinTimeBetweenStrokes = 0.1f;
    private const float MinForce = 1.0f;
    private const float MaxSwimMagnitude = 5.0f;
    
    private float _swimSpeed = 26.0f;
    private float _leftCooldown;
    private float _rightCooldown;
    private Rigidbody _rb;
    private PlayerController _playerController;
    private Vector3 _localVelocity;
    private bool _leftHandEnabled = true;
    private bool _rightHandEnabled;

    public static SwimmingController Instance = null!;

    public void Setup(PlayerController playerController)
    {
        Instance = this;
        
        _playerController = playerController;
        playerController.defaultSwimDrag = 0.7f;
        _rb = playerController.activeController.rb;

        Settings.SwimDragForceChanged += SetSwimDragForce;
        Settings.SwimSpeedChanged += SetSwimSpeedForce;
    }

    private void SetSwimDragForce(float dragForce)
    {
        _playerController.defaultSwimDrag = dragForce;
    }
    
    private void SetSwimSpeedForce(float speed)
    {
        _swimSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (!_playerController.underWater ||
            !Settings.ImmersiveSwimming ||
            Player.main.motorMode == Player.MotorMode.Seaglide)
        {
            return;
        }
     
        _leftCooldown += Time.fixedDeltaTime;
        _rightCooldown += Time.fixedDeltaTime;
        
        var left = GameInput.GetButtonHeld(GameInput.Button.MoveDown);
        var right = GameInput.GetButtonHeld(GameInput.Button.MoveUp);

        _localVelocity = Vector3.zero;

        if (left && _leftCooldown >= MinTimeBetweenStrokes)
        {
            var force = _leftHandEnabled ? 1.0f : 0.3f;
            _localVelocity += VRCameraRig.instance.LeftHandPosition.GetVelocity(SteamVR_Input_Sources.LeftHand) * force;
            _leftCooldown = 0.0f;
        }

        if (right && _rightCooldown >= MinTimeBetweenStrokes)
        {
            var force = _rightHandEnabled ? 1.0f : 0.3f;
            _localVelocity += VRCameraRig.instance.RightHandPosition.GetVelocity(SteamVR_Input_Sources.RightHand) * force;
            _rightCooldown = 0.0f;
        }
        
        _localVelocity *= -1.0f;
        
        if (_localVelocity.sqrMagnitude > MinForce * MinForce)
        {
            var worldVelocity =  _playerController.player.camRoot.transform.TransformDirection(_localVelocity);
            _rb.AddForce(worldVelocity * _swimSpeed, ForceMode.Acceleration);
        }
        
        _rb.velocity = Vector3.ClampMagnitude(_rb.velocity, MaxSwimMagnitude);
    }

    public void SetLeftHandActive(bool value)
    {
        _leftHandEnabled = value;
    }
    
    public void SetRightHandActive(bool value)
    {
        _rightHandEnabled = value;
    }
}

#region Patches

[HarmonyPatch(typeof(Seaglide), nameof(Seaglide.FixedUpdate))]
internal static class Seaglide_FixedUpdate_Patch
{
    internal static bool Prefix(Seaglide __instance)
    {
        if (!__instance.powerGlideActive)
        {
            return false;
        }

        var forward = VRCameraRig.instance.rightController.transform.forward;
        Player.main.gameObject.GetComponent<Rigidbody>().AddForce(forward * __instance.powerGlideForce, ForceMode.Force);
        return false;
    }
}


[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Start))]
internal static class PlayerController_Start_Patch
{
    internal static void Postfix(PlayerController __instance)
    {
        var swimmingController = __instance.gameObject.AddComponent<SwimmingController>();
        swimmingController.Setup(__instance);
    }
}

[HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateMove))]
internal static class GameInput_UpdateMove_Patch
{
    private static void ResetMovement()
    {
        if (Settings.ImmersiveSwimming &&
            Player.main?.IsUnderwaterForSwimming() == true &&
            Player.main?.motorMode != Player.MotorMode.Seaglide)
        {
            GameInput.moveDirection.Set(0, 0, 0);
        }
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        var isPrimaryDeviceGamepadMethod = AccessTools.Method(typeof(GameInput), nameof(GameInput.IsPrimaryDeviceGamepad));
        var helperMethod = AccessTools.Method(typeof(GameInput_UpdateMove_Patch), nameof(ResetMovement));

        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            if (code.opcode != OpCodes.Call || (MethodInfo)code.operand != isPrimaryDeviceGamepadMethod)
            {
                continue;
            }
            
            // Insert ResetMovement call before IsPrimaryDeviceGamepad call
            codes.Insert(i, new CodeInstruction(OpCodes.Call, helperMethod));
            break;
        }

        return codes.AsEnumerable();
    }
}

#endregion