using HarmonyLib;
using UnityEngine;
using System.Collections;
using UWE;
using System;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UWEXR;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;

    using rail;
    using UnityEngine.Playables;
    using UnityEngine.XR;

    // Tweaks regarding the cinematics in VR mode of the game
    static class CinematicsVR
    {
        /*
        public static bool hasSlept = false;
        static public IEnumerator DelayedCancel(PlayerCinematicController __instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            __instance.OnPlayerCinematicModeEnd();
        }
        */
    }

    #region Patches

    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.SetVrActiveParam))]
    public static class SetVrActiveParamFixer
    {
        public static void Postfix(PlayerCinematicController __instance)
        {
            string text = "vr_active";
            //Mod.logger.LogInfo($"PlayerCinematicController.SetVrActiveParam called animationMode={VRGameOptions.GetVrAnimationMode()}");
            bool vrAnimationMode = VRGameOptions.GetVrAnimationMode();
            if (__instance.animator != null)
            {
                __instance.animator.SetBool(text, vrAnimationMode);
            }
            for (int i = 0; i < __instance.animParamReceivers.Length; i++)
            {
                IAnimParamReceiver component = __instance.animParamReceivers[i].GetComponent<IAnimParamReceiver>();
                if (component != null)
                {
                    component.ForwardAnimationParameterBool(text, vrAnimationMode);
                }
            }
        }
    }


    //Disabled cinematic sequences no longer control the player's head rotation greatly reducing nausea affect of cinematics
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.UpdatePlayerPosition))]
    public static class ManagedLateUpdateFixer
    {
        public static bool Prefix(PlayerCinematicController __instance)
        {
            Transform component = __instance.player.GetComponent<Transform>();
            Transform component2 = MainCameraControl.main.GetComponent<Transform>();
            component.position = __instance.animatedTransform.position;
            component2.position = __instance.player.camAnchor.position;
        
            //disabling head rotation for these cinematics do not work well           
            bool enableRotation = __instance.gameObject.name == "seatruck_module_sleeper_anim" //going to sleep
                || __instance.gameObject.name == "IntroCinematics(Clone)" //Planet crash landing
                || __instance.gameObject.name == "submarine_hatch_02_doorway_01_cin2" //entering base through the seatruck bay hatch
                || __instance.gameObject.name == "Top_Left_Collision"   //going down the ladder at the radio tower
                || __instance.gameObject.name == "Top_Right_Collision";  //going down the ladder at the radio tower
            if(enableRotation || VROptions.enableCinematics)
            {
                component.rotation = __instance.animatedTransform.rotation;
                component2.rotation = __instance.animatedTransform.rotation;
            }
		
            return false;
        }
    }

    //Fixes PDA being offset after an animated climb/exit/etc
    //Bypassing the compiled code disables the system hand animation override during the cinematic. 
    //Might be better to somehow reset hands at the end instead?
    /*
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.InitDirector))]
    public static class InitDirectorFixer
    {
        public static bool Prefix(PlayerCinematicController __instance)
        {
            return false;
        }
    }
    

    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.OnDirectorStopped))]
    public static class StoppedDirectorFixer
    {
        public static void Postfix(PlayerCinematicController __instance)
        {
            VRHands.instance.ResetHandTargets();
        }
    }
    */
    //Enable specific cinematics to be bypassed
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.StartCinematicMode))]
    public static class StartCinematicModeFixer
    {
        public static bool Prefix(PlayerCinematicController __instance, Player setplayer)
        {
            VRUtil.Recenter();
            if (__instance.debug)
            {
                Debug.Log(__instance.gameObject.name + ".StartCinematicMode");
            }
            if (__instance.cinematicModeActive)
            {
                if (__instance.debug)
                {
                    Debug.Log(__instance.gameObject.name + " cinematic already active!");
                }
                return false;
            }

            Mod.logger.LogInfo($"StartCinematicMode name = {__instance.gameObject.name} playInVR = {__instance.playInVr} cinematicsEnabled = {VROptions.enableCinematics} "); 
            bool canSkip = __instance.gameObject.name == "Drop_Pod_anim" //entering/exiting the drop pod
                        //|| __instance.gameObject.name == "model"
                        || __instance.gameObject.name == "precursor_teleporter_cin"; //after going through the teleporter in the end sequences
            bool mustSkip = __instance.gameObject.name == "IntroCinematics(Clone)" && VROptions.skipIntro //The intro
                        || __instance.gameObject.name == "Necklace_Scene_Placements"; //picking up Sam's necklace

            __instance.player = null;
            if ((!__instance.playInVr && !VROptions.enableCinematics && canSkip) || mustSkip )
            {
                if (__instance.debug)
                {
                    Debug.Log(__instance.gameObject.name + " skip cinematic");
                }
                Mod.logger.LogInfo(__instance.gameObject.name + " skipping cinematic");          
                __instance.SkipCinematic(setplayer);
                return false;
            }
            if (__instance.animator != null)
            {
                __instance.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            __instance.cinematicModeActive = true;
            __instance.timeCinematicModeStarted = Time.time;
            if (__instance.director != null)
            {
                __instance.director.played += __instance.OnDirectorPlayed;
                __instance.director.stopped += __instance.OnDirectorStopped;
                if (__instance.restoreControl)
                {
                    __instance.Invoke("DoFailSafeCancel", (float)__instance.director.duration + 0.2f);
                }
            }
            if (setplayer)
            {
                __instance.SetPlayer(setplayer);
                __instance.Subscribe(__instance.player, true);
            }
            __instance.state = PlayerCinematicController.State.In;
            if (__instance.informGameObject != null)
            {
                __instance.informGameObject.SendMessage("OnPlayerCinematicModeStart", __instance, SendMessageOptions.DontRequireReceiver);
            }
            if (__instance.player)
            {
                Transform component = __instance.player.GetComponent<Transform>();
                Transform component2 = MainCameraControl.main.GetComponent<Transform>();
                __instance.cameraPosition = __instance.player.camAnchor.InverseTransformPoint(component2.position);
                __instance.player.frozenMixin.Unfreeze();
                __instance.player.cinematicModeActive = true;
                __instance.player.FreezeStats();
                __instance.player.playerController.SetEnabled(false);
                Player.main.GetPDA().Close();
                if (__instance.leftIKTarget || __instance.rightIKTarget)
                {
                    __instance.player.armsController.SetWorldIKTargetSmooth(__instance.leftIKTarget, __instance.rightIKTarget, __instance.ikSmoothTime);
                }
                __instance.cameraFromRotation = component.rotation;
                __instance.playerFromPosition = component.position;
                __instance.playerFromRotation = component.rotation;
                if (__instance.playerViewInterpolateAnimParam.Length > 0)
                {
                    SafeAnimator.SetBool(__instance.player.GetComponentInChildren<Animator>(), __instance.playerViewInterpolateAnimParam, true);
                }
            }
            if (__instance.animator != null && !string.IsNullOrEmpty(__instance.interpolateAnimParam))
            {
                __instance.animator.SetBool(__instance.interpolateAnimParam, true);
            }
            if (__instance.interpolateDuringAnimation)
            {
                __instance.animState = true;
            }
            if (__instance.debug)
            {
                Debug.Log(__instance.gameObject.name + " successfully started cinematic");
            }
            float num = (__instance.director == null) ? 60f : ((float)__instance.director.duration + 10f);
            float num2 = Time.time + num;
            PlayerCinematicController.cinematicActivityExpireTime = ((PlayerCinematicController.cinematicModeCount == 0) ? num2 : Mathf.Max(PlayerCinematicController.cinematicActivityExpireTime, num2));
            PlayerCinematicController.cinematicModeCount++;
            HandReticle.main.RequestCrosshairHide();

            return false;
        }
    }

/*
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.InitDirector))]
    public static class InitDirectorFixer
    {
        public static bool Prefix(PlayerCinematicController __instance)
        {
            if (__instance.director == null)
            {
                return false;
            }
            foreach (PlayableBinding playableBinding in __instance.director.playableAsset.outputs)
            {
                Type outputTargetType = playableBinding.outputTargetType;
                
                UnityEngine.Object sourceObject = playableBinding.sourceObject;
                string streamName = playableBinding.streamName;
                Type left = (sourceObject != null) ? sourceObject.GetType() : null;
                Mod.logger.LogInfo($"PlayerCinematicController.InitDirector name = { __instance.gameObject.name} left = {left} streamName = {streamName} outputTargetType = {outputTargetType}");

                if (left == typeof(AnimationTrack))
                {
                    if (streamName == "Player")
                    {
                        foreach (TimelineClip timelineClip in (sourceObject as AnimationTrack).GetClips())
                        {
                            AnimationClip anim = timelineClip.animationClip;                           
                            if(anim != null)
                            {
                                Mod.logger.LogInfo($"PlayerCinematicController.InitDirector animationClip = { anim.name} empty = {anim.empty}");
                                if(anim.name == "RT_climb_up")
                                {
                                    return false;
                                }
                                foreach (AnimationEvent ev in anim.GetEventsInternal())
                                {
                                     Mod.logger.LogInfo($"PlayerCinematicController.InitDirector event = {ev.functionName} {ev.GetType()}");
                                }
                            }
                            PropertyName exposedName = (timelineClip.asset as AnimationPlayableAsset).name;
                            Mod.logger.LogInfo($"PlayerCinematicController.InitDirector clipName = {exposedName}");
                        }
                        __instance.director.SetGenericBinding(sourceObject, __instance.player.playerAnimator);
                    }
                }
                else if (left == typeof(ControlTrack))
                {
                    GameObject gameObject = null;
                    if (streamName == "RightHand")
                    {
                        gameObject = __instance.player.socketRightHand;
                    }
                    else if (streamName == "LeftHand")
                    {
                        gameObject = __instance.player.socketLeftHand;
                    }
                    if (gameObject != null)
                    {
                        foreach (TimelineClip timelineClip in (sourceObject as ControlTrack).GetClips())
                        {
                            PropertyName exposedName = (timelineClip.asset as ControlPlayableAsset).sourceGameObject.exposedName;
                            Mod.logger.LogInfo($"PlayerCinematicController.InitDirector exposedName = {exposedName}");
                            __instance.director.SetReferenceValue(exposedName, gameObject);
                        }
                    }
                }
            }

            return false;
        }
    }
*/
/*
    //Checks UnityEngine.XR instead of UWEXR
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.SkipCinematic))]
    public static class SkipCinematicFixer
    {
        public static bool Prefix(PlayerCinematicController __instance, Player player)
        {
            Mod.logger.LogInfo($"Calling PlayerCinematicController.SkipCinematic");
            if (__instance.interruptAutoMove)
            {
                GameInput.SetAutoMove(false);
            }
            __instance.player = player;
            if (player)
            {
                Transform component = player.GetComponent<Transform>();
                Transform component2 = MainCameraControl.main.GetComponent<Transform>();
                if (__instance.UseEndTransform())
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.SkipCinematic using EndTransform");
                    player.playerController.SetEnabled(false);
                    if (XRSettings.enabled)
                    {
                        MainCameraControl.main.ResetCamera();
                        VRUtil.Recenter();
                    }
                    component.position = __instance.endTransform.position;
                    component.rotation = __instance.endTransform.rotation;
                    component2.rotation = component.rotation;
                }
                player.playerController.SetEnabled(true);
                player.cinematicModeActive = false;
            }
            if (__instance.informGameObject != null)
            {
                Mod.logger.LogInfo($"PlayerCinematicController.SkipCinematic informGameObject = {__instance.informGameObject.ToString()}");
                __instance.informGameObject.SendMessage("OnPlayerCinematicModeEnd", __instance, SendMessageOptions.DontRequireReceiver);
            }
           return false;
        }
    }
*/

/*
    //Force Animations for climbing up and down the radio tower ladder (Bottom_Collision and Top_Left_Collision and Top_Right_Collision),
    //entering and exiting the seatruck (seatruck_anim), interacting with the AI cube (sanctuary_cube_cin)
    //because these interactions do not have an EndTranform set and so without the animation, the player is not moved
    //to the end location. Also in the case of the AI cube, not sure how/if the story would progress without the animation
    //Also handles the skip intro flag
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.StartCinematicMode))]
    public static class StartCinematicModeFixer
    {
        public static bool Prefix(PlayerCinematicController __instance, Player setplayer)
        {
            if (__instance.cinematicModeActive)
            {
                Mod.logger.LogInfo(__instance.gameObject.name + " cinematic already active!");
                return false;
            }
            __instance.player = null;
            bool skipIntro = __instance.gameObject.name == "IntroCinematics(Clone)" && VROptions.skipIntro;
            //Do not skip these or else actions won't happen or the story wont progress
            bool skipAnimations = !__instance.playInVr && VRGameOptions.GetVrAnimationMode() 
                //&& __instance.gameObject.name != "model" //enter/exit base climbing ladder (breaks game)
                && __instance.gameObject.name != "submarine_hatch_02_doorway_01_cin2" //enter/exit base through sub bay  
                //&& __instance.gameObject.name != "hatch_side_anim" //enter/exit base through hatch (can be skipped)                          
                && __instance.gameObject.name != "seatruck_anim" //enter/exit SeaTruck Motor section when not docked with base
                && __instance.gameObject.name != "seatruck_module_prawn_anim" //enter/exit SeaTruck Prawn module
                && __instance.gameObject.name != "seatruck_module_sleeper_anim" //when exiting the animation it puts the player on the ceiling and disables all input              
                && __instance.gameObject.name != "Top_Left_Collision" && __instance.gameObject.name != "Top_Right_Collision" && __instance.gameObject.name != "Bottom_Collision" //ladder for comm tower                
                && __instance.gameObject.name != "MaintenanceBox" // Animation of installing the maintence override (has a clipping issue slot0011)
                && __instance.gameObject.name != "sanctuary_cube_cin" // Cutscene when encountering AI Cube
                && __instance.gameObject.name != "marge_intro_cin" // Cutscene when meeting Marge for the first time (slot0001)               
                && __instance.gameObject.name != "marge_base1_cin" // Cutscene when entering marge's home (slot0009)
                && __instance.gameObject.name != "marge_greenhouse_cin" // Cutscene when entering marge's greenhouse (slot0012)
                && __instance.gameObject.name != "DockPlayerCinematic" //enter/exit seatruck when docked at base (hud doesnt release)
                && __instance.gameObject.name != "Mobile_Extractor_anim" //antidote insertion (slot 0015) Without this it still works but you dont see the bottle connected to the injector
                && __instance.gameObject.name != "Precursor_fabricator_room_anim" //Alien body mind transfer
                && __instance.gameObject.name != "NoReturnTimeline" //Alien transfer gate
                //&& __instance.gameObject.name != "precursor_teleporter_cin" //Alien gate cinematic (can be skipped)
                && __instance.gameObject.name != "Greetings" //Alien world cinematic       
                && __instance.gameObject.name != "RepairPillar1" //Repair Pillar 1                        
                && __instance.gameObject.name != "RepairPillar2" //Repair Pillar 2
                && __instance.gameObject.name != "ReturnArmsLeft" //Return robot arms   
                && __instance.gameObject.name != "ReturnArmsRight" //Return robot arms   
                && __instance.gameObject.name != "EnterShip" //Enter escape ship from below   
                && __instance.gameObject.name != "Takeoff" //Brace yourself on escape ship                                                                     
                //&& __instance.gameObject.name != "Necklace_Scene_Placements" //Picking up the necklace (slot 0017) does nothing with InitDirectorFixer disabled. When skipped ends with PlayerCinematicController.SkipCinematic informGameObject = FrozenLeviathan_Necklace(Clone)
                && __instance.gameObject.name != "IntroCinematics(Clone)";  //crash landing on planet  
            
            if(__instance.gameObject.name == "seatruck_module_sleeper_anim")   
            {
                skipAnimations = CinematicsVR.hasSlept;
                CinematicsVR.hasSlept = !CinematicsVR.hasSlept;
            }     
            
            Mod.logger.LogInfo($"{__instance.gameObject.name}.StartCinematicMode skipIntro = {skipIntro} skipAnimations = {skipAnimations} "); 
            //if (!__instance.playInVr && VRGameOptions.GetVrAnimationMode())
            if(skipIntro || skipAnimations)
            {
                Mod.logger.LogInfo(__instance.gameObject.name + " skipping cinematic");          
                __instance.SkipCinematic(setplayer);
                return false;
            }
            if (__instance.animator != null)
            {
                __instance.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            __instance.cinematicModeActive = true;
            __instance.timeCinematicModeStarted = Time.time;
            if (__instance.director != null)
            {
                __instance.director.played += __instance.OnDirectorPlayed;
                __instance.director.stopped += __instance.OnDirectorStopped;
                if (__instance.restoreControl)
                {
                    __instance.Invoke("DoFailSafeCancel", (float)__instance.director.duration + 0.2f);
                }
            }
            if (setplayer)
            {
                __instance.SetPlayer(setplayer);
                __instance.Subscribe(__instance.player, true);
            }
            __instance.state = PlayerCinematicController.State.In;
            if (__instance.informGameObject != null)
            {
                __instance.informGameObject.SendMessage("OnPlayerCinematicModeStart", __instance, SendMessageOptions.DontRequireReceiver);
            }
            if (__instance.player)
            {
                Transform component = __instance.player.GetComponent<Transform>();
                Transform component2 = MainCameraControl.main.GetComponent<Transform>();
                __instance.cameraPosition = __instance.player.camAnchor.InverseTransformPoint(component2.position);
                __instance.player.frozenMixin.Unfreeze();
                __instance.player.cinematicModeActive = true;
                __instance.player.FreezeStats();
                __instance.player.playerController.SetEnabled(false);
                Player.main.GetPDA().Close();
                if (__instance.leftIKTarget || __instance.rightIKTarget)
                {
                    __instance.player.armsController.SetWorldIKTargetSmooth(__instance.leftIKTarget, __instance.rightIKTarget, __instance.ikSmoothTime);
                }
                __instance.cameraFromRotation = component.rotation;
                __instance.playerFromPosition = component.position;
                __instance.playerFromRotation = component.rotation;
                if (__instance.playerViewInterpolateAnimParam.Length > 0)
                {
                    SafeAnimator.SetBool(__instance.player.GetComponentInChildren<Animator>(), __instance.playerViewInterpolateAnimParam, true);
                }
            }
            if (__instance.animator != null && !string.IsNullOrEmpty(__instance.interpolateAnimParam))
            {
                __instance.animator.SetBool(__instance.interpolateAnimParam, true);
            }
            if (__instance.interpolateDuringAnimation)
            {
                __instance.animState = true;
            }
            if (__instance.debug)
            {
                Mod.logger.LogInfo(__instance.gameObject.name + " successfully started cinematic");
            }
            float num = (__instance.director == null) ? 60f : ((float)__instance.director.duration + 10f);
            float num2 = Time.time + num;
            PlayerCinematicController.cinematicActivityExpireTime = ((PlayerCinematicController.cinematicModeCount == 0) ? num2 : Mathf.Max(PlayerCinematicController.cinematicActivityExpireTime, num2));
            PlayerCinematicController.cinematicModeCount++;
            HandReticle.main.RequestCrosshairHide();

            //CoroutineHost.StartCoroutine(Cinematics.DelayedCancel(__instance, 0.2f)); //Doesnt work
            return false;
        }
    }
*/
/*
    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.EndCinematicMode))]
    public static class EndCinematicModeFixer
    {
        public static bool Prefix(PlayerCinematicController __instance, bool forceRestoreControl = false)
        {
            Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 1");
            if (__instance.cinematicModeActive)
            {
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 2");
                if (__instance.IsInvoking("DoFailSafeCancel"))
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 3");
                    __instance.CancelInvoke("DoFailSafeCancel");
                }
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 4");
                if (__instance.interruptAutoMove)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 5");
                    GameInput.SetAutoMove(false);
                }
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 6");
                if (__instance.animator != null && __instance.resetToCullCompletely)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 7");
                    __instance.animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 8");
                __instance.animState = false;
                __instance.state = PlayerCinematicController.State.None;
                if (__instance.player)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 9");
                    __instance.player.playerController.SetEnabled(true);
                    __instance.player.cinematicModeActive = false;
                    __instance.player.UnfreezeStats();
                    if (!__instance.restoreControl && !forceRestoreControl)
                    {
                        Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 10");
                        __instance.player.playerController.SetEnabled(false);
                    }
                    if (__instance.leftIKTarget || __instance.rightIKTarget)
                    {
                        Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 11");
                        __instance.player.armsController.SetWorldIKTarget(null, null);
                    }
                }
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 12");
                __instance.cinematicModeActive = false;
                PlayerCinematicController.cinematicModeCount--;
                HandReticle.main.UnrequestCrosshairHide();
                if (__instance.rumbleCinematic)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 13");
                    __instance.rumbleCinematic.Stop();
                }
                Mod.logger.LogInfo($"PlayerCinematicController.EndCinematicMode 14");
            }           
            
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerCinematicController), nameof(PlayerCinematicController.OnPlayerCinematicModeEnd))]
    public static class OnPlayerCinematicModeEndFixer
    {
        public static bool Prefix(PlayerCinematicController __instance)
        {
            Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 1");
            if (__instance.cinematicModeActive && !__instance.onCinematicModeEndCall)
            {
                Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 2");
                if (__instance.director != null)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 3");
                   __instance.director.played -= __instance.OnDirectorPlayed;
                    __instance.director.stopped -= __instance.OnDirectorStopped;
                }
                Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 4");
                if (__instance.player)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 5");
                    __instance.UpdatePlayerPosition();
                }
                Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 6");
                __instance.animState = false;
                if (__instance.UseEndTransform())
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd using EndTransform");
                    __instance.state = PlayerCinematicController.State.Out;
                    if (__instance.player)
                    {
                        Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 7");
                        Transform component = __instance.player.GetComponent<Transform>();
                        __instance.playerFromPosition = component.position;
                        __instance.playerFromRotation = component.rotation;
                    }
                }
                else
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 8");
                    __instance.EndCinematicMode(false);
                }
                Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 9");
                if (__instance.informGameObject != null)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 10");
                    __instance.onCinematicModeEndCall = true;
                    __instance.informGameObject.SendMessage("OnPlayerCinematicModeEnd", __instance, SendMessageOptions.DontRequireReceiver);
                    __instance.onCinematicModeEndCall = false;
                }
                if (__instance.player)
                {
                    Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 11");
                    __instance.player.playerController.ForceControllerSize();
                }
                Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 12");
            }
            Mod.logger.LogInfo($"PlayerCinematicController.OnPlayerCinematicModeEnd 13");
           
            return false;
        }
    }
*/
/*
    [HarmonyPatch(typeof(PlayableDirector), nameof(PlayableDirector.SetGenericBinding)) ]
    public static class PlayableDirector_mod
    {
        public static bool Prefix(PlayableDirector __instance, Object key, Object value)
        {
            Mod.logger.LogInfo($"PlayableDirector.SetGenericBinding {key} {value}");
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayableDirector), nameof(PlayableDirector.Play), new[] {typeof(PlayableAsset), typeof(DirectorWrapMode)})]
    public static class PlayableDirectorPlay_mod
    {
        public static bool Prefix(PlayableDirector __instance, PlayableAsset asset, DirectorWrapMode mode)
        {
            Mod.logger.LogInfo($"PlayableDirector.SetGenericBinding {asset.name} {mode}");
            return true;
        }
    }
*/
 
    #endregion

}
