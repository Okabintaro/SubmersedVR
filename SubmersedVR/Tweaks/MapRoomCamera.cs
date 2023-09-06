using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Tweaks regarding the toolbars in VR mode of the game
    static class MapRoomCameraVR
    {

    }

    #region Patches

    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.ControlCamera))]
    class MapRoomCameraStartFixer
    {
        public static void Postfix(MapRoomCamera __instance)
        {
            //turning off the armsController when IK is disabled does 
            //bad things when hands are turned back on
            if(Player.main.armsController.ik.enabled)
            {
                Player.main.armsController.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(MapRoomCamera), nameof(MapRoomCamera.FreeCamera))]
    class MapRoomCameraEndFixer
    {
        public static void Postfix(MapRoomCamera __instance)
        {
            if(Player.main.armsController.ik.enabled)
            {
                Player.main.armsController.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_CameraDrone), nameof(uGUI_CameraDrone.Awake))]
    class MapRoomCameraConnectingFixer
    {
        public static void Postfix(uGUI_CameraDrone __instance)
        {
             __instance.fader.transform.localScale = new Vector3(5, 5, 5);
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
                    if(Settings.IsSnapTurningEnabled == true)
                    {
                        __instance.rigidBody.transform.rotation = Quaternion.Euler( new Vector3( __instance.rigidBody.transform.rotation.eulerAngles.x, __instance.rigidBody.transform.rotation.eulerAngles.y + lookDelta.x,  __instance.rigidBody.transform.rotation.eulerAngles.z));
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
