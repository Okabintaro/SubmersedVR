using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{ 
    using UnityEngine.XR;

    //Implementation of Snap Turning
    //Previous Snap Turning had a bug where occasionaly snap turning would
    //simply stop working. It appears that because there are many calls
    //to GetLookDelta in the SN code, occasionally something would make the
    //call and capture the single snap turn result (where vector is returned as (45,0)),
    //and the function that needed it would not get it. In this implementation we will 
    //only give snap turning results to the functions that really need it and only flip
    //the flag after those functions use it
    static class SnapTurning
    {
        public static bool hasSnapTurn = false;
        public static bool disableSnapTurn = false;
        public static float snapTurnAngle = 0f;

        public static void Reset()
        {
            hasSnapTurn = false;
            snapTurnAngle = 0.0f;
        }
    }

    #region Patches

    [HarmonyPatch(typeof(GameInput), nameof(GameInput.GetLookDelta))]
    public static class SnapTurningGetLookDelta
    {
        public static void Postfix(ref Vector2 __result)
        {
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isOnSnowBike = Player.main?.inHovercraft == true;
            bool isPilotingSeatruck = Player.main?.inSeatruckPilotingChair == true;
            
            if ((Settings.IsSnapTurningEnabled && !(isInExosuit || isOnSnowBike || isPilotingSeatruck)) || (Settings.IsExosuitSnapTurningEnabled && isInExosuit) || (Settings.IsSnowBikeSnapTurningEnabled && isOnSnowBike))
            {
                float angle = Settings.SnapTurningAngle; //player
                if(Settings.IsExosuitSnapTurningEnabled && isInExosuit)
                {
                    angle = Settings.ExosuitSnapTurningAngle;
                }
                if(Settings.IsSnowBikeSnapTurningEnabled && isOnSnowBike)
                {
                    angle = Settings.SnowBikeSnapTurningAngle;
                }

                float lookX = __result.x;
                float absX = Mathf.Abs(lookX);
                float threshold = 0.5f;

                if (absX > threshold && !SnapTurning.disableSnapTurn) 
                {
                    SnapTurning.snapTurnAngle = angle * Mathf.Sign(lookX);
                    SnapTurning.hasSnapTurn = true;
                    SnapTurning.disableSnapTurn = true;
                    //Mod.logger.LogInfo($"Snap Turned");
                } 
                else  
                {
                    __result.x = 0;
                    if (absX <= threshold) {
                        SnapTurning.disableSnapTurn = false;
                    }
                }
                //DebugPanel.Show($"GetLookDelta: r={r} l={l} lr={r - l}\ndelta={Time.deltaTime}\nlookX={lookX}\nabsX = {absX}\nsnapTurned={SteamVrGameInput.SnapTurned}", true);
            }
        }
    }

    //Snap turning override
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.Update))]
    static class VehicleSnapTurnFixerVR
    {
        public static bool Prefix(Vehicle __instance)
        {
            if (__instance.CanPilot())
            {
                __instance.steeringWheelYaw = Mathf.Lerp(__instance.steeringWheelYaw, 0f, Time.deltaTime);
                __instance.steeringWheelPitch = Mathf.Lerp(__instance.steeringWheelPitch, 0f, Time.deltaTime);
                if (__instance.mainAnimator)
                {
                    __instance.mainAnimator.SetFloat("view_yaw", __instance.steeringWheelYaw * 70f);
                    __instance.mainAnimator.SetFloat("view_pitch", __instance.steeringWheelPitch * 45f);
                }
            }
            //Mod.logger.LogInfo($"Vehicle.Update controlScheme = {__instance.controlSheme}");                       

            if (__instance.GetPilotingMode() && __instance.CanPilot() && (__instance.moveOnLand || __instance.transform.position.y < Ocean.GetOceanLevel()))
            {
                Vector2 vector = AvatarInputHandler.main.IsEnabled() ? GameInput.GetLookDelta() : Vector2.zero;
                __instance.steeringWheelYaw = Mathf.Clamp(__instance.steeringWheelYaw + vector.x * __instance.steeringReponsiveness, -1f, 1f);
                __instance.steeringWheelPitch = Mathf.Clamp(__instance.steeringWheelPitch + vector.y * __instance.steeringReponsiveness, -1f, 1f);
                if (__instance.controlSheme == Vehicle.ControlSheme.Submersible)
                {
                    float d = 3f;
                    __instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * __instance.sidewaysTorque * 0.0015f * d, ForceMode.VelocityChange);
                    __instance.useRigidbody.AddTorque(__instance.transform.right * -vector.y * __instance.sidewaysTorque * 0.0015f * d, ForceMode.VelocityChange);
                    __instance.useRigidbody.AddTorque(__instance.transform.forward * -vector.x * __instance.sidewaysTorque * 0.0002f * d, ForceMode.VelocityChange);
                }
                else if (__instance.controlSheme == Vehicle.ControlSheme.Submarine || __instance.controlSheme == Vehicle.ControlSheme.Mech)
                {
                    //Exosuit
                    if(Settings.IsExosuitSnapTurningEnabled == true && SnapTurning.hasSnapTurn)
                    {
                        __instance.useRigidbody.transform.rotation = Quaternion.Euler( new Vector3(  __instance.useRigidbody.transform.rotation.eulerAngles.x, __instance.useRigidbody.transform.rotation.eulerAngles.y + SnapTurning.snapTurnAngle,  __instance.useRigidbody.transform.rotation.eulerAngles.z));
                        SnapTurning.Reset();
                    }
                    else if (Settings.IsExosuitSnapTurningEnabled == false && vector.x != 0f)
                    {
                        __instance.useRigidbody.AddTorque(__instance.transform.up * vector.x * __instance.sidewaysTorque, ForceMode.VelocityChange);           
                    }
                }
                else if (__instance.controlSheme == Vehicle.ControlSheme.Hoverbike)
                {
                    Mod.logger.LogInfo($"Vehicle.Update called scheme = {__instance.controlSheme}");
                    __instance.useRigidbody.AddRelativeTorque(new Vector3(vector.y, 0f, 0f));
                }
            }
            bool flag = __instance.IsPowered();
            if (__instance.wasPowered != flag)
            {
                __instance.wasPowered = flag;
                __instance.OnPoweredChanged(flag);
            }
            __instance.ReplenishOxygen();
        
            return false;
        }
    }

    //Snap turning override
    [HarmonyPatch(typeof(Hoverbike), nameof(Hoverbike.PhysicsMove))]
    static class HoverbikeSnapTurn
    {
        public static bool Prefix(Hoverbike __instance)
        {
            __instance.isPiloting = __instance.GetPilotingCraft();
            if (__instance.dockedPad)
            {
                return false;
            }
            if (!__instance.isPiloting)
            {
                return false;
            }
            __instance.moveDirection = (AvatarInputHandler.main.IsEnabled() ? GameInput.GetMoveDirection() : Vector3.zero);
            new Vector3(__instance.moveDirection.x, 0f, __instance.moveDirection.z);
            Vector2 vector = __instance.overflowInput;
            if (__instance.energyMixin.IsDepleted())
            {
                __instance.moveDirection = Vector3.zero;
            }
            __instance.appliedThrottle = (__instance.moveDirection != Vector3.zero);
            float d = __instance.overWater ? (__instance.horizontalDampening / __instance.waterDampening) : __instance.horizontalDampening;
            __instance.rb.AddTorque(__instance.transform.right * -vector.x * __instance.sidewaysTorque * __instance.verticalDampening, ForceMode.VelocityChange);
            if(Settings.IsSnowBikeSnapTurningEnabled == false)
            {
                __instance.rb.AddTorque(__instance.transform.up * vector.y * __instance.sidewaysTorque * d, ForceMode.VelocityChange);
            }
            if(Settings.IsSnowBikeSnapTurningEnabled == true && SnapTurning.hasSnapTurn)
            {
                __instance.rb.transform.rotation = Quaternion.Euler( new Vector3( __instance.rb.transform.rotation.eulerAngles.x, __instance.rb.transform.rotation.eulerAngles.y + SnapTurning.snapTurnAngle,  __instance.rb.transform.rotation.eulerAngles.z));
                SnapTurning.Reset();
            }
            Vector3 velocity = __instance.rb.velocity;
            Vector3 point = __instance.moveDirection;
            Mathf.Min(1f, point.magnitude);
            point.y = 0f;
            point.Normalize();
            __instance.horizMoveDir = MainCamera.camera.transform.rotation * point;
            float d2 = __instance.overWater ? (__instance.forwardAccel / __instance.waterDampening) : __instance.forwardAccel;
            __instance.rb.AddForce(__instance.horizMoveDir * d2);
           
            return false;
        }
    }

    //This does 3 things
    //1 
    //While piloting vehicles, the player's head camera gets locked into position when using XRSettings.enabled = true
    //While on the hoverbike we dont want to be locked in because we want to eliminate the yaw and pitch to reduce nausea
    //2 
    //Disable the last XRSettings.enabled branch by replacing it with false
    //3
    //Enable Player Snap Turning

    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnUpdate))]
    static class MainCameraControlFixer
    {
        public static bool Prefix(MainCameraControl __instance)
        {
            //Fix 1
            bool xrenabled = true;
            if(Player.main.inHovercraft)
            {
                xrenabled = XRSettingsEnabled.isEnabled;
                XRSettingsEnabled.isEnabled = false;
            }
            ////
            
            float deltaTime = Time.deltaTime;
            if (Player.main.IsUnderwater())
            {
                __instance.swimCameraAnimation = Mathf.Clamp01(__instance.swimCameraAnimation + deltaTime);
            }
            else
            {
                __instance.swimCameraAnimation = Mathf.Clamp01(__instance.swimCameraAnimation - deltaTime);
            }
            Vector3 velocity = __instance.playerController.velocity;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            bool inExosuit = Player.main.inExosuit;
            bool flag4 = uGUI_BuilderMenu.IsOpen();
            bool flag5 = false;
            if (Player.main != null)
            {
                flag = Player.main.GetPDA().isInUse;
                flag3 = (Player.main.motorMode == Player.MotorMode.Vehicle);
                flag2 = (flag || flag3 || __instance.cinematicMode);
                flag5 = Player.main.inHovercraft;
                if (XRSettings.enabled && VROptions.gazeBasedCursor)
                {
                    flag2 = (flag2 || flag4);
                }
            }
            if (flag2 != __instance.wasInLockedMode || __instance.lookAroundMode != __instance.wasInLookAroundMode)
            {
                __instance.camRotationX = 0f;
                __instance.camRotationY = 0f;
                __instance.wasInLockedMode = flag2;
                __instance.wasInLookAroundMode = __instance.lookAroundMode;
            }
            bool flag6 = (!__instance.cinematicMode || __instance.lookAroundMode) && !flag && __instance.mouseLookEnabled && (flag3 || AvatarInputHandler.main == null || AvatarInputHandler.main.IsEnabled() || Builder.isPlacing);
            if (flag3 && !XRSettings.enabled && !inExosuit)
            {
                flag6 = false;
            }
            if (__instance.deathSequence)
            {
                flag6 = false;
            }
            Transform transform = __instance.transform;
            float num = (float)((flag || __instance.lookAroundMode || Player.main.GetMode() == Player.Mode.LockedPiloting) ? 1 : -1);
            if (!flag2 || (__instance.cinematicMode && !__instance.lookAroundMode))
            {
                __instance.cameraOffsetTransform.localEulerAngles = UWE.Utils.LerpEuler(__instance.cameraOffsetTransform.localEulerAngles, Vector3.zero, deltaTime * 5f);
            }
            else
            {
                transform = __instance.cameraOffsetTransform;
                __instance.rotationY = Mathf.LerpAngle(__instance.rotationY, 0f, PDA.deltaTime * 15f);
                __instance.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(__instance.transform.localEulerAngles.x, 0f, PDA.deltaTime * 15f), __instance.transform.localEulerAngles.y, 0f);
                __instance.cameraUPTransform.localEulerAngles = UWE.Utils.LerpEuler(__instance.cameraUPTransform.localEulerAngles, Vector3.zero, PDA.deltaTime * 15f);
            }
            if (!XRSettings.enabled)
            {
                float num2 = __instance.camPDAZOffset * num * PDA.deltaTime / __instance.cameraPDAZoomDuration;
                Vector3 localPosition = __instance.cameraOffsetTransform.localPosition;
                localPosition.z = Mathf.Clamp(localPosition.z + num2, 0f, __instance.camPDAZOffset);
                __instance.cameraOffsetTransform.localPosition = localPosition;
            }
            else
            {
                __instance.animator.SetFloat(MainCameraControl.pdaDistanceParamId, VROptions.pdaDistance);
            }
            Vector2 vector = Vector2.zero;
            if (flag6 && FPSInputModule.current.lastGroup == null)
            {
                vector = GameInput.GetLookDelta();

                //Fix 3
                bool isInExosuit = Player.main?.inExosuit == true;
                bool isOnSnowBike = Player.main?.inHovercraft == true;
                bool isPilotingSeatruck = Player.main?.inSeatruckPilotingChair == true;
                if (Settings.IsSnapTurningEnabled && SnapTurning.hasSnapTurn && !(isInExosuit || isOnSnowBike || isPilotingSeatruck))
                {
                    vector.x = SnapTurning.snapTurnAngle;
                    SnapTurning.Reset();
                }
                ////
                
                if (XRSettings.enabled && VROptions.disableInputPitch)
                {
                    vector.y = 0f;
                }
                if (inExosuit)
                {
                    vector.x = 0f;
                }
                vector *= Player.main.mesmerizedSpeedMultiplier;
                if (Player.main.frozenMixin.IsFrozen())
                {
                    vector *= Player.main.frozenMixin.cameraSpeedMultiplier;
                }
            }
            if (__instance.deathSequence)
            {
                vector = new Vector2(Mathf.Cos(Time.time * 8.5f) * 25f * Time.deltaTime, -50f * Time.deltaTime + Mathf.Cos(Time.time * 8f) * 9f * Time.deltaTime);
            }
            __instance.UpdateCamShake();
            if (__instance.cinematicMode && !__instance.lookAroundMode)
            {
                if (__instance.cinematicOverrideRotation)
                {
                    __instance.camRotationX = __instance.transform.localEulerAngles.y;
                    __instance.camRotationY = -__instance.transform.localEulerAngles.x;
                }
                else
                {
                    __instance.camRotationX = Mathf.LerpAngle(__instance.camRotationX, 0f, deltaTime * 2f);
                    __instance.camRotationY = Mathf.LerpAngle(__instance.camRotationY, 0f, deltaTime * 2f);
                    __instance.transform.localEulerAngles = new Vector3(-__instance.camRotationY, __instance.camRotationX, 0f);
                }
            }
            else if (flag2)
            {
                if (!XRSettings.enabled)
                {
                    bool flag7 = (!__instance.lookAroundMode && !inExosuit) || flag;
                    bool flag8 = !__instance.lookAroundMode || flag;
                    __instance.camRotationX += vector.x;
                    __instance.camRotationY += vector.y;
                    __instance.camRotationX = Mathf.Clamp(__instance.camRotationX, -60f, 60f);
                    __instance.camRotationY = Mathf.Clamp(__instance.camRotationY, -60f, 60f);
                    if (flag5)
                    {
                        __instance.cameraOffsetTransform.eulerAngles = __instance.vehicleOverrideHeadRot;
                    }
                    else
                    {
                        if (flag8)
                        {
                            __instance.camRotationX = Mathf.LerpAngle(__instance.camRotationX, 0f, PDA.deltaTime * 10f);
                        }
                        if (flag7)
                        {
                            __instance.camRotationY = Mathf.LerpAngle(__instance.camRotationY, 0f, PDA.deltaTime * 10f);
                        }
                        __instance.cameraOffsetTransform.localEulerAngles = new Vector3(-__instance.camRotationY, __instance.camRotationX, 0f);
                    }
                }
            }
            else
            {
                __instance.rotationX += vector.x;
                __instance.rotationY += vector.y;
                __instance.rotationY = Mathf.Clamp(__instance.rotationY, __instance.minimumY, __instance.maximumY);
                __instance.cameraUPTransform.localEulerAngles = new Vector3(Mathf.Min(0f, -__instance.rotationY), 0f, 0f);
                transform.localEulerAngles = new Vector3(Mathf.Max(0f, -__instance.rotationY), __instance.rotationX, 0f);
            }
            __instance.UpdateStrafeTilt();
            Vector3 localEulerAngles = __instance.transform.localEulerAngles + new Vector3(0f, 0f, __instance.cameraAngleMotion.y * __instance.cameraTiltMod + __instance.strafeTilt * 0.5f);
            float num3 = 0f - __instance.skin;
            if (!flag2 && __instance.GetCameraBob())
            {
                float target = Mathf.Min(1f, velocity.magnitude / 5f);
                __instance.smoothedSpeed = Mathf.MoveTowards(__instance.smoothedSpeed, target, deltaTime);
                num3 += (Mathf.Sin(Time.time * 6f) - 1f) * (0.02f + __instance.smoothedSpeed * 0.15f) * __instance.swimCameraAnimation;
            }
            if (__instance.impactForce > 0f)
            {
                __instance.impactBob = Mathf.Min(0.9f, __instance.impactBob + __instance.impactForce * deltaTime);
                __instance.impactForce -= Mathf.Max(1f, __instance.impactForce) * deltaTime * 5f;
            }
            num3 -= __instance.impactBob;
            num3 -= __instance.stepAmount;
            if (__instance.impactBob > 0f)
            {
                __instance.impactBob = Mathf.Max(0f, __instance.impactBob - Mathf.Pow(__instance.impactBob, 0.5f) * deltaTime * 3f);
            }
            __instance.stepAmount = Mathf.Lerp(__instance.stepAmount, 0f, deltaTime * Mathf.Abs(__instance.stepAmount));
            float num4 = __instance.shakeAmount / 20f;
            float x = UnityEngine.Random.Range(-num4, num4);
            float y = UnityEngine.Random.Range(-num4, num4);
            float z = UnityEngine.Random.Range(-num4, num4);
            Vector3 b = __instance.initialOffset + new Vector3(x, y, z) * __instance.camShake;
            __instance.shakeOffset = Vector3.Lerp(__instance.shakeOffset, b, deltaTime * 20f);
            Vector3 b2 = flag5 ? __instance.vehicleOverrideHeadPos : Vector3.zero;
            __instance.transform.localPosition = new Vector3(0f, num3, 0f) + b2 + __instance.shakeOffset;
            __instance.transform.localEulerAngles = localEulerAngles;
            if (Player.main.motorMode == Player.MotorMode.Vehicle)
            {
                __instance.transform.localEulerAngles = Vector3.zero;
            }
            Vector3 localEulerAngles2 = new Vector3(Mathf.LerpAngle(__instance.viewModel.localEulerAngles.x, 0f, deltaTime * 5f), __instance.transform.localEulerAngles.y, 0f);
            Vector3 localPosition2 = __instance.transform.localPosition;
            //Fix 2
            /*
            if (XRSettings.enabled) 
            {
                if (flag2 && !flag3)
                {
                    localEulerAngles2.y = __instance.viewModelLockedYaw;
                }
                else
                {
                    localEulerAngles2.y = 0f;
                }
                if (!flag3 && !__instance.cinematicMode)
                {
                    if (!flag2)
                    {
                        Quaternion rotation = __instance.playerController.forwardReference.rotation;
                        localEulerAngles2.y = (__instance.gameObject.transform.parent.rotation.GetInverse() * rotation).eulerAngles.y;
                    }
                    localPosition2 = __instance.gameObject.transform.parent.worldToLocalMatrix.MultiplyPoint(__instance.playerController.forwardReference.position);
                }
            }
            */
            __instance.viewModel.transform.localEulerAngles = localEulerAngles2;
            __instance.viewModel.transform.localPosition = localPosition2;     

            //Fix 1
            if(Player.main.inHovercraft)
            {
                XRSettingsEnabled.isEnabled = xrenabled;
            }
            ////
            
            return false;
        }
    }

        //Snap turning override
    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.Update))]
    static class MapRoomCameraFixer
    {
        public static bool Prefix(MapRoomCamera __instance)
        {
            __instance.UpdateEnergyRecharge();
            if (__instance.IsControlled() && __instance.inputStackDummy.activeInHierarchy)
            {
                if (!__instance.IsReady() && LargeWorldStreamer.main.IsWorldSettled())
                {
                    __instance.readyForControl = true;
                    __instance.connectingSound.Stop();
                    global::Utils.PlayFMODAsset(__instance.connectedSound, __instance.transform, 20f);
                }
                if (__instance.CanBeControlled(null) && __instance.readyForControl)
                {
                    Vector2 lookDelta = GameInput.GetLookDelta();
                    //__instance.rigidBody.AddTorque(__instance.transform.up * lookDelta.x * 45f * 0.0015f, ForceMode.VelocityChange);
                    if(Settings.IsSnapTurningEnabled == false)
                    {
                        __instance.rigidBody.AddTorque(__instance.transform.up * lookDelta.x * 45f * 0.0015f, ForceMode.VelocityChange);
                    }
                    if(!VROptions.disableInputPitch)
                    {
                        __instance.rigidBody.AddTorque(__instance.transform.right * -lookDelta.y * 45f * 0.0015f, ForceMode.VelocityChange);
                    }                   
                    if(Settings.IsSnapTurningEnabled == true && SnapTurning.hasSnapTurn)
                    {
                        lookDelta.x = SnapTurning.snapTurnAngle;
                        __instance.rigidBody.transform.rotation = Quaternion.Euler( new Vector3( __instance.rigidBody.transform.rotation.eulerAngles.x, __instance.rigidBody.transform.rotation.eulerAngles.y + lookDelta.x,  __instance.rigidBody.transform.rotation.eulerAngles.z));
                        SnapTurning.Reset();
                    }
                    __instance.wishDir = GameInput.GetMoveDirection();
                    __instance.wishDir.Normalize();
                    if (__instance.dockingPoint != null && __instance.wishDir != Vector3.zero)
                    {
                        __instance.dockingPoint.UndockCamera();
                    }
                }
                else
                {
                    __instance.wishDir = Vector3.zero;
                }
                if (Input.GetKeyUp(KeyCode.Escape) || GameInput.GetButtonUp(GameInput.Button.Exit))
                {
                    if (__instance.routine == null)
                    {
                        __instance.FreeCamera(true);
                        __instance.routine = __instance.StartCoroutine(__instance.CaptureScreenAndExitLockedMode());
                    }
                }
                else if (GameInput.GetButtonDown(GameInput.Button.CycleNext))
                {
                    __instance.screen.CycleCamera(1);
                }
                else if (GameInput.GetButtonDown(GameInput.Button.CyclePrev))
                {
                    __instance.screen.CycleCamera(-1);
                }
                else if (GameInput.GetButtonDown(GameInput.Button.RightHand))
                {
                    bool flag = !__instance.lightsParent.activeInHierarchy;
                    __instance.lightsParent.SetActive(flag);
                    if (flag)
                    {
                        FMODUWE.PlayOneShot(__instance.lightOnSound, __instance.transform.position, 1f);
                    }
                    else
                    {
                        FMODUWE.PlayOneShot(__instance.lightOffSound, __instance.transform.position, 1f);
                    }
                }
                if (Player.main != null && Player.main.liveMixin != null && !Player.main.liveMixin.IsAlive())
                {
                    __instance.FreeCamera(true);
                    __instance.ExitLockedMode(true);
                }
                float magnitude = __instance.rigidBody.velocity.magnitude;
                float time = Mathf.Clamp(__instance.transform.InverseTransformDirection(__instance.rigidBody.velocity).z / 15f, 0f, 1f);
                if (magnitude > 2f)
                {
                    __instance.engineSound.Play();
                    __instance.energyMixin.ConsumeEnergy(Time.deltaTime * 0.06666f);
                }
                else
                {
                    __instance.engineSound.Stop();
                }
                __instance.screenEffectModel.GetComponent<Renderer>().materials[0].SetColor(ShaderPropertyID._Color, __instance.gradientInner.Evaluate(time));
                __instance.screenEffectModel.GetComponent<Renderer>().materials[1].SetColor(ShaderPropertyID._Color, __instance.gradientOuter.Evaluate(time));
            }        
            return false;
        }
    }

    #endregion

}

