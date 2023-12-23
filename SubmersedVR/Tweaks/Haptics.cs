using HarmonyLib;
using UnityEngine;
using System;
using UnityEngine.UI;
using UWE;
using System.Collections;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;

    static class HapticsVR
    {
        public static GameObject lastHoverComponent = null;

        public static int lastBuilderCatID = -1;
        public static int lastPDACatID = -1;
        public static float effectsTimer = 0.0f;
        public static float leftSecondaryEffectsTimer = 0.0f;
        public static float rightSecondaryEffectsTimer = 0.0f;
        public static float effectsTimeout = 0.0f;
        public static float effectsScale = 0.0f;
        public static float effectsDuration = 0.0f;

        public static float defaultUIDuration = 0.05f;
        public static float defaultUIAmplitude = 0.4f;

        public enum Controller
        {
            Left,
            Right,
            Both
        }

        public static void PlayHapticsNew(Controller controller, float secondsFromNow, float durationSeconds, float frequency, float amplitude)
        {
            switch (controller)
            {
                case Controller.Left:
                    SteamVR_Actions.subnautica_HapticsLeft?.Execute(secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Sources.Any);
                    break;
                case Controller.Right:
                    SteamVR_Actions.subnautica_HapticsRight?.Execute(secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Sources.Any);
                    break;
                case Controller.Both:
                    SteamVR_Actions.subnautica_HapticsLeft?.Execute(secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Sources.Any);
                    SteamVR_Actions.subnautica_HapticsRight?.Execute(secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Sources.Any);
                    break;
            }

        }

        public static void PlayGameHaptics(Controller controller, float secondsFromNow, float durationSeconds, float frequency, float amplitude)
        {
            if (Settings.AreGameHapticsEnabled)
            {
                PlayHapticsNew(controller, secondsFromNow, durationSeconds, frequency, amplitude);
            }
        }

        public static void PlayUIHaptics(Controller controller, float secondsFromNow, float durationSeconds, float frequency, float amplitude)
        {
            if (Settings.AreUIHapticsEnabled)
            {
                PlayHapticsNew(controller, secondsFromNow, durationSeconds, frequency, amplitude);
            }
        }

        //Fading heartbeat
        public static IEnumerator PlayerDeath()
        {
            PlayGameHaptics(Controller.Both, 0.0f, 0.2f, 10f, 1.0f);
            yield return new WaitForSeconds(0.3f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.3f, 10f, 0.9f);
            yield return new WaitForSeconds(0.8f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.2f, 10f, 0.5f);
            yield return new WaitForSeconds(0.3f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.3f, 10f, 0.4f);
            yield return new WaitForSeconds(1.0f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.2f, 10f, 0.3f);
            yield return new WaitForSeconds(0.3f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.3f, 10f, 0.2f);
            yield return new WaitForSeconds(1.2f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.2f, 10f, 0.1f);
            yield return new WaitForSeconds(0.3f);
            PlayGameHaptics(Controller.Both, 0.0f, 0.3f, 10f, 0.1f);
        }


        public static IEnumerator PlayerHeartbeat()
        {
            Controller controller = Settings.PutBarsOnWrist ? Controller.Both : Controller.Left;
            PlayGameHaptics(controller, 0.0f, 0.15f, 10f, 0.8f);
            yield return new WaitForSeconds(0.2f);
            PlayGameHaptics(controller, 0.0f, 0.25f, 10f, 0.3f);
        }

        public static IEnumerator PlayerFrozen()
        {
            PlayGameHaptics(Controller.Both, 0.5f, 5.0f, 10f, 0.3f);
            yield return new WaitForSeconds(5.0f);
            PlayGameHaptics(Controller.Both, 0.0f, 5.0f, 10f, 0.2f);
            yield return new WaitForSeconds(5.0f);
            PlayGameHaptics(Controller.Both, 0.0f, 5.0f, 10f, 0.1f);
        }


    }

    #region Patches

    //Haptics when an item is clicked (eg tapping on a stone)
    //Haptics when an item is hovered over
    [HarmonyPatch(typeof(GUIHand), nameof(GUIHand.Send))]
    class HapticsUpdate
    {
        public static void Prefix(GameObject target, HandTargetEventType e, GUIHand hand)
        {
            //Mod.logger.LogInfo($"GUIHand.Send called"); 
            if (target == null || !target.activeInHierarchy || e == HandTargetEventType.None || target.GetComponent<IHandTarget>() == null)
            {
                HapticsVR.lastHoverComponent = null;
                return;
            }
            if (e == HandTargetEventType.Click)
            {
                // TODO: Should this be UI?
                HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.2f, 5f, 1.0f);
            }
            if (e == HandTargetEventType.Hover)
            {
                if (target == HapticsVR.lastHoverComponent)
                {
                    return;
                }

                HapticsVR.lastHoverComponent = target;
                //Mod.logger.LogInfo($"GUIHand.Send Hover called"); 
                HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.05f, 10f, 0.7f);
            }
        }
    }

    //Haptics when an item is bashed (eg knife held when rock hit)
    [HarmonyPatch(typeof(GUIHand), nameof(GUIHand.BashHit))]
    public static class PickupableHaptics
    {
        public static void Postfix(GUIHand __instance)
        {
            //Mod.logger.LogInfo($"GUIHand.BashHit called");
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.5f, 0.2f, 5f, 1.0f);
        }
    }


    //Detects a Hover Out while in the exosuit so that an item can be triggered more than once
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.UpdateActiveTarget))]
    public static class ExosuitHaptics
    {
        public static void Postfix(Exosuit __instance)
        {
            //Mod.logger.LogInfo($"Exosuit.UpdateActiveTarget called activeTraget is null = {__instance.activeTarget == null}");
            if (__instance.activeTarget == null)
            {
                HapticsVR.lastHoverComponent = null;
            }
        }
    }


    //Handles dual wielding drillers also
    [HarmonyPatch(typeof(Exosuit), nameof(Exosuit.Update))]
    public static class ExosuitDrillHaptics
    {
        public static void Postfix(Exosuit __instance)
        {
            ExosuitDrillArm left = __instance.leftArm as ExosuitDrillArm;
            ExosuitDrillArm right = __instance.rightArm as ExosuitDrillArm;
            if ((left != null && left.drilling) || (right != null && right.drilling))
            {
                //Mod.logger.LogInfo($"ExosuitDrillArm.Update {__instance.currentLeftArmType == TechType.ExosuitDrillArmModule} {__instance.currentRightArmType == TechType.ExosuitDrillArmModule}"); 
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > HapticsVR.effectsTimeout)
                {
                    float dur = UnityEngine.Random.Range(0.01f, 0.20f);
                    if (right?.drilling == true)
                    {
                        // HapticsVR.PlayHaptics(0.0f, dur, 10f, UnityEngine.Random.Range(0f, 1.0f) * (right?.drillTarget == true ? 1.0f : 0.1f), false, true, false);
                        float amp = UnityEngine.Random.Range(0f, 1.0f) * (right?.drillTarget == true ? 1.0f : 0.1f);
                        HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, dur, 10f, amp);
                    }
                    if (left?.drilling == true)
                    {
                        // HapticsVR.PlayHaptics(0.0f, dur, 10f, UnityEngine.Random.Range(0f, 1.0f) * (left?.drillTarget == true ? 1.0f : 0.1f), false, false, true);
                        float amp = UnityEngine.Random.Range(0f, 1.0f) * (left?.drillTarget == true ? 1.0f : 0.1f);
                        HapticsVR.PlayGameHaptics(HapticsVR.Controller.Left, 0.0f, dur, 10f, amp);
                    }

                    HapticsVR.effectsTimer = 0.0f;
                    HapticsVR.effectsTimeout = UnityEngine.Random.Range(dur, dur + 0.10f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ExosuitClawArm), nameof(ExosuitClawArm.OnHit))]
    public static class ExosuitClawArmHaptics
    {
        public static void Postfix(ExosuitClawArm __instance)
        {
            HapticsVR.Controller controller = __instance.exosuit.rightArm as ExosuitClawArm == __instance ? HapticsVR.Controller.Right : HapticsVR.Controller.Left;
            HapticsVR.PlayGameHaptics(controller, 0.0f, 0.2f, 10f, 1.0f);
        }
    }

    [HarmonyPatch(typeof(ExosuitGrapplingArm), nameof(ExosuitGrapplingArm.OnHit))]
    public static class ExosuitGrapplingArmHaptics
    {
        public static void Postfix(ExosuitGrapplingArm __instance)
        {
            // HapticsVR.PlayHaptics(0.0f, 0.2f, 10f, 0.5f, false, __instance.exosuit.rightArm as ExosuitGrapplingArm == __instance, __instance.exosuit.leftArm as ExosuitGrapplingArm == __instance);
            HapticsVR.Controller controller = __instance.exosuit.rightArm as ExosuitClawArm == __instance ? HapticsVR.Controller.Right : HapticsVR.Controller.Left;
            HapticsVR.PlayGameHaptics(controller, 0.0f, 0.2f, 10f, 0.5f);
        }
    }

    //Handles the case of dual wielded grappling hooks
    [HarmonyPatch(typeof(ExosuitGrapplingArm), nameof(ExosuitGrapplingArm.FixedUpdate))]
    public static class ExosuitGrapplingActiveArmHaptics
    {
        public static void Postfix(ExosuitGrapplingArm __instance)
        {
            bool isRightarm = __instance.exosuit.rightArm as ExosuitGrapplingArm == __instance;
            bool isLeftArm = __instance.exosuit.leftArm as ExosuitGrapplingArm == __instance;
            if (__instance.hook.attached)
            {
                if (isLeftArm)
                {
                    HapticsVR.leftSecondaryEffectsTimer += Time.deltaTime;
                    if (HapticsVR.leftSecondaryEffectsTimer > 0.8f)
                    {
                        HapticsVR.PlayGameHaptics(HapticsVR.Controller.Left, 0.0f, 0.6f, 10f, 1.0f);
                        HapticsVR.leftSecondaryEffectsTimer = 0.0f;
                    }
                }
                if (isRightarm)
                {
                    HapticsVR.rightSecondaryEffectsTimer += Time.deltaTime;
                    if (HapticsVR.rightSecondaryEffectsTimer > 0.8f)
                    {
                        HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.6f, 10f, 1.0f);
                        HapticsVR.rightSecondaryEffectsTimer = 0.0f;
                    }
                }
            }
            else if (__instance.hook.flying)
            {
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > 0.1)
                {
                    HapticsVR.PlayGameHaptics(isRightarm ? HapticsVR.Controller.Right : HapticsVR.Controller.Left, 0.0f, 0.1f, 10f, 0.3f);
                    HapticsVR.effectsTimer = 0.0f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ExosuitTorpedoArm), nameof(ExosuitTorpedoArm.Shoot))]
    public static class ExosuitTorpedoArmHaptics
    {
        public static void Postfix(ExosuitTorpedoArm __instance)
        {
            HapticsVR.Controller controller = __instance.exosuit.rightArm as ExosuitClawArm == __instance ? HapticsVR.Controller.Right : HapticsVR.Controller.Left;
            HapticsVR.PlayGameHaptics(controller, 1.2f, 0.8f, 10f, 1.0f);
        }
    }

    //Detects a Hover Out so that an item can be triggered more than once
    [HarmonyPatch(typeof(GUIHand), nameof(GUIHand.UpdateActiveTarget))]
    public static class GUIHandEnable
    {
        public static void Postfix(GUIHand __instance)
        {
            //Mod.logger.LogInfo($"GUIHand.OnUpdate called activeTarget is null = {__instance.activeTarget == null}");
            if (__instance.activeTarget == null)
            {
                HapticsVR.lastHoverComponent = null;
            }
        }
    }

    //Haptics when an item is picked up
    [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.PlayPickupSound))]
    public static class PickupHaptics
    {
        public static void Postfix(Pickupable __instance)
        {
            //Mod.logger.LogInfo($"Pickupable.PlayPickupSound called");
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.2f, 5f, 1.0f);
        }
    }

    //Haptics when an item is dropped
    [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.PlayDropSound))]
    public static class DropHaptics
    {
        public static void Postfix(Pickupable __instance)
        {
            //Mod.logger.LogInfo($"Pickupable.PlayDropSound called");
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.2f, 5f, 1.0f);
        }
    }

    //Damage from Vehicles going too deep
    [HarmonyPatch(typeof(CrushDamage), nameof(CrushDamage.CrushDamageUpdate))]
    public static class CrushDamagetHaptics
    {
        public static void Postfix(CrushDamage __instance)
        {
            if (__instance.GetCanTakeCrushDamage() && __instance.GetDepth() > __instance.crushDepth)
            {
                //Mod.logger.LogInfo($"DamageFX.CrushDamageUpdate called");
                HapticsVR.PlayGameHaptics(HapticsVR.Controller.Both, 0.0f, 0.5f, 10f, 1.0f);
            }
        }
    }

    //Handles several damage types but triggers for some unknown things so filter them out
    [HarmonyPatch(typeof(SoundOnDamage), nameof(SoundOnDamage.OnTakeDamage))]
    public static class SoundOnDamageHaptics
    {
        public static void Postfix(SoundOnDamage __instance, DamageInfo damageInfo)
        {
            //Mod.logger.LogInfo($"DamageFX.SoundOnDamage called {damageInfo.damage} {damageInfo.type}");
            if (damageInfo.damage > 0 && !(damageInfo.type == DamageType.Cold && (damageInfo.damage == 0.5)) && !(damageInfo.type == DamageType.Normal && (damageInfo.damage == 7 || damageInfo.damage == 30 || damageInfo.damage == 80)))
            {
                HapticsVR.PlayGameHaptics(HapticsVR.Controller.Both, 0.0f, 0.4f, 10f, 1.0f);
            }
        }
    }

    //Havent seen any instances of this firing
    [HarmonyPatch(typeof(DamageOverTime), nameof(DamageOverTime.DoDamage))]
    public static class DamageOverTimeHaptics
    {
        public static void Postfix(DamageOverTime __instance)
        {
            //Mod.logger.LogInfo($"(DamageOverTime.DoDamage called ");
            float dur = __instance.interval - 0.1f;
            if (dur < 0)
            {
                dur = 0.1f;
            }
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Both, 0.0f, dur, 10f, 1.0f);
        }
    }


    //Cyclops?
    [HarmonyPatch(typeof(SubRoot), nameof(SubRoot.OnTakeDamage))]
    public static class SubRootHaptics
    {
        public static void Postfix(SubRoot __instance, DamageInfo damageInfo)
        {
            //Mod.logger.LogInfo($"SubRoot.OnTakeDamage called {damageInfo.damage}");
            if (damageInfo.damage > 0)
            {
                // HapticsVR.PlayHaptics(0.0f, 0.4f, 10f, 1.0f, false, true, true);
                HapticsVR.PlayGameHaptics(HapticsVR.Controller.Both, 0.0f, 0.4f, 10f, 1.0f);
            }
        }
    }

    //Haptics while Seatruck is being attacked
    /* BZ only
    [HarmonyPatch(typeof(SeaTruckSegment), nameof(SeaTruckSegment.Update))]
    public static class SeaTruckAttackHaptics
    {
        public static void Postfix(SeaTruckSegment __instance)
        {      
            if(__instance.underCreatureAttack)
            {
                //Mod.logger.LogInfo($"SeaTruckSegment.Update under creature attack = {__instance.underCreatureAttack}");
                HapticsVR.effectsTimer += Time.deltaTime;
                if(HapticsVR.effectsTimer > 0.70)
                {
                    HapticsVR.PlayHaptics(0.0f, 0.55f, 10f, 1.0f, false, true, true);
                    HapticsVR.effectsTimer = 0.0f;
                }
            }
        }
    } 
    */
    /*
    [HarmonyPatch(typeof(GamepadVibration), nameof(GamepadVibration.Vibrate))]
    public static class GamepadVibrationHaptics
    {
        public static void Postfix( float duration, float leftIntesity, float rightIntensity)
        {      
            Mod.logger.LogInfo($"GamepadVibration.Vibrate called {duration} {leftIntesity} {rightIntensity}");           
            HapticsVR.PlayHaptics(0.0f, duration, 10f, rightIntensity, false, true, true, leftIntesity);           
        }
    } 
    */
    //When health is very low
    [HarmonyPatch(typeof(uGUI_HealthBar), nameof(uGUI_HealthBar.OnPulse))]
    public static class PulseHaptics
    {
        public static void Postfix(uGUI_HealthBar __instance, float scalar)
        {
            //Mod.logger.LogInfo($"uGUI_HealthBar.OnPulse called scaler = {scalar} statePulse = {__instance.statePulse.normalizedTime}");
            if ((__instance.statePulse.normalizedTime > 0 && HapticsVR.effectsTimer == 0.0f) || (__instance.statePulse.normalizedTime < HapticsVR.effectsTimer && __instance.statePulse.normalizedTime != 0f))
            {
                CoroutineHost.StartCoroutine(HapticsVR.PlayerHeartbeat());
            }
            HapticsVR.effectsTimer = __instance.statePulse.normalizedTime;
        }
    }

    [HarmonyPatch(typeof(uGUI_FoodBar), nameof(uGUI_FoodBar.OnPulse))]
    public static class FoodbarHaptics
    {
        public static void Postfix(uGUI_FoodBar __instance, float scalar)
        {
            if (__instance.pulseAnimationState == null)
            {
                return;
            }
            //Mod.logger.LogInfo($"uGUI_FoodBar.OnPulse called scaler = {scalar} statePulse = {__instance.pulseAnimationState.normalizedTime}");
            if ((__instance.pulseAnimationState.normalizedTime > 0 && HapticsVR.effectsTimer == 0.0f) || (__instance.pulseAnimationState.normalizedTime < HapticsVR.effectsTimer && __instance.pulseAnimationState.normalizedTime != 0f))
            {
                //HapticsVR.PlayHaptics(0.0f, 0.1f, 10f, 0.3f, false, !Settings.PutBarsOnWrist, true);
                CoroutineHost.StartCoroutine(HapticsVR.PlayerHeartbeat());
            }
            HapticsVR.effectsTimer = __instance.pulseAnimationState.normalizedTime;
        }
    }

    [HarmonyPatch(typeof(uGUI_OxygenBar), nameof(uGUI_OxygenBar.OnPulse))]
    public static class OxygenBarHaptics
    {
        public static void Postfix(uGUI_OxygenBar __instance, float scalar)
        {
            //Mod.logger.LogInfo($"uGUI_OxygenBar.OnPulse called scaler = {scalar} statePulse = {__instance.pulseAnimationState.normalizedTime}");
            if ((__instance.statePulse.normalizedTime > 0 && HapticsVR.effectsTimer == 0.0f) || (__instance.statePulse.normalizedTime < HapticsVR.effectsTimer && __instance.statePulse.normalizedTime != 0f))
            {
                CoroutineHost.StartCoroutine(HapticsVR.PlayerHeartbeat());
            }
            HapticsVR.effectsTimer = __instance.statePulse.normalizedTime;
        }
    }

    [HarmonyPatch(typeof(uGUI_WaterBar), nameof(uGUI_WaterBar.OnPulse))]
    public static class WaterbarHaptics
    {
        public static void Postfix(uGUI_WaterBar __instance, float scalar)
        {
            if (__instance.pulseAnimationState == null)
            {
                return;
            }
            //Mod.logger.LogInfo($"uGUI_WaterBar.OnPulse called scaler = {scalar} statePulse = {__instance.pulseAnimationState.normalizedTime}");
            if ((__instance.pulseAnimationState.normalizedTime > 0 && HapticsVR.effectsTimer == 0.0f) || (__instance.pulseAnimationState.normalizedTime < HapticsVR.effectsTimer && __instance.pulseAnimationState.normalizedTime != 0f))
            {
                CoroutineHost.StartCoroutine(HapticsVR.PlayerHeartbeat());
            }
            HapticsVR.effectsTimer = __instance.pulseAnimationState.normalizedTime;
        }
    }
    /* BZ only
        [HarmonyPatch(typeof(uGUI_BodyHeatMeter), nameof(uGUI_BodyHeatMeter.OnPulse))]
        public static class BodyHeatMeterHaptics
        {
            public static void Postfix(uGUI_BodyHeatMeter __instance, float scalar)
            {
                //Mod.logger.LogInfo($"uGUI_BodyHeatMeter.OnPulse called scaler = {scalar} statePulse = {__instance.pulseAnimationState.normalizedTime}");
                if((__instance.statePulse.normalizedTime > 0 && HapticsVR.effectsTimer == 0.0f) || (__instance.statePulse.normalizedTime < HapticsVR.effectsTimer && __instance.statePulse.normalizedTime != 0f))
                {
                    CoroutineHost.StartCoroutine(HapticsVR.PlayerHeartbeat());
                }
                HapticsVR.effectsTimer = __instance.statePulse.normalizedTime;
            }
        }
    */
    //Player taking direct damage
    //This is not called if the damage is enough to kill the player
    [HarmonyPatch(typeof(Player), nameof(Player.OnTakeDamage))]
    public static class PlayerDamageHaptics
    {
        public static void Postfix(Player __instance, DamageInfo damageInfo)
        {
            float intensity = damageInfo.damage * 3f / 100f;
            float shakeAmount = Mathf.Clamp(intensity, 0f, 5f);
            //Mod.logger.LogInfo($"Player.OnTakeDamage called damage = {damageInfo.damage} intensity = {intensity} duration = {shakeAmount * 2f}");
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Both, 0.0f, shakeAmount * 2f, 10f, Mathf.Clamp(intensity, 0f, 1f));

        }
    }
    /* BZ only
        //Handler that triggers even if player damage is enough to kill the player
        [HarmonyPatch(typeof(LeviathanAttackTimelineManager), nameof(LeviathanAttackTimelineManager.OnPlayerAttack))]
        public static class LeviathanPlayerDamageHaptics
        {
            public static void Postfix(LeviathanAttackTimelineManager __instance)
            {    
                Mod.logger.LogInfo($"LeviathanAttackTimelineManager.OnPlayerAttack called");
                HapticsVR.PlayHaptics(0.0f, 4.0f, 10f, 1.0f, false, true, true);

            }
        } 
    */
    //When the player dies
    //Called after death, not during attack
    [HarmonyPatch(typeof(Player), nameof(Player.OnKill))]
    public static class DeathHaptics
    {
        public static void Postfix(Player __instance)
        {
            CoroutineHost.StartCoroutine(HapticsVR.PlayerDeath());
        }
    }



    [HarmonyPatch(typeof(Knife), nameof(Knife.OnToolUseAnim))]
    public static class KnifeHaptics
    {
        public static void Postfix(Knife __instance)
        {
            //Mod.logger.LogInfo($"Knife.OnToolUseAnim");
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.2f, 5f, 1.0f);
        }
    }

    [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.OnOpenPDA))]
    class PDAOpenHaptics
    {
        public static void Postfix(uGUI_PDA __instance)
        {
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Left, 0.0f, 0.2f, 20f, 0.5f);
        }
    }

    [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.OnClosePDA))]
    class PDACloseHaptics
    {
        public static void Postfix(uGUI_PDA __instance)
        {
            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Left, 0.0f, 0.2f, 10f, 0.5f);
        }
    }

    //Propulsion cannon when shooting objects
    //Same code is used for both in and out of the exosuit
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.OnShoot))]
    class PropulsionCannonShootHaptics
    {
        public static void Postfix(PropulsionCannon __instance)
        {
            // bool rightArm = true;
            // bool leftArm = false;
            // Exosuit exosuit = VehiclesVR.PilotedExosuit();
            // if(exosuit != null)
            // {
            //     rightArm = (exosuit.rightArm as ExosuitPropulsionArm)?.propulsionCannon == __instance;
            //     leftArm = (exosuit.leftArm as ExosuitPropulsionArm)?.propulsionCannon == __instance;
            // }
            bool rightArm = true;
            Exosuit exosuit = VehiclesVR.PilotedExosuit();
            if (exosuit != null)
            {
                rightArm = (exosuit.rightArm as ExosuitPropulsionArm)?.propulsionCannon == __instance;
            }
            //Mod.logger.LogInfo($"PropulsionCannon.OnShoot in vehicle exosuit = {exosuit != null}");              

            HapticsVR.Controller controller = rightArm ? HapticsVR.Controller.Right : HapticsVR.Controller.Left;
            HapticsVR.PlayGameHaptics(controller, 0.0f, 0.6f, 10f, 1.0f);
        }
    }


    //While actively holding an object, use a pulsating haptic
    //Same code is used for both in and out of the exosuit
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.Update))]
    class PropulsionCannonHoldHaptics
    {
        public static void Postfix(PropulsionCannon __instance)
        {
            if ((__instance.grabbedObject != null && __instance.grabbedObject.GetComponent<Rigidbody>() != null) || __instance.firstUseGrabbedObject != null)
            {
                Exosuit exosuit = VehiclesVR.PilotedExosuit();
                if (exosuit != null)
                {
                    bool rightArm = rightArm = (exosuit.rightArm as ExosuitPropulsionArm)?.propulsionCannon == __instance;
                    bool leftArm = leftArm = (exosuit.leftArm as ExosuitPropulsionArm)?.propulsionCannon == __instance;
                    if (leftArm)
                    {
                        HapticsVR.leftSecondaryEffectsTimer += Time.deltaTime;
                        if (HapticsVR.leftSecondaryEffectsTimer > 0.9f)
                        {
                            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Left, 0.0f, 0.6f, 10f, 0.4f);
                            HapticsVR.leftSecondaryEffectsTimer = 0.0f;
                        }
                    }
                    if (rightArm)
                    {
                        HapticsVR.rightSecondaryEffectsTimer += Time.deltaTime;
                        if (HapticsVR.rightSecondaryEffectsTimer > 0.9f)
                        {
                            HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.6f, 10f, 0.4f);
                            HapticsVR.rightSecondaryEffectsTimer = 0.0f;
                        }
                    }

                }
                else
                {
                    HapticsVR.effectsTimer += Time.deltaTime;
                    if (HapticsVR.effectsTimer > 0.9)
                    {
                        HapticsVR.Controller controller = __instance.firstUseGrabbedObject != null ? HapticsVR.Controller.Both : HapticsVR.Controller.Right;
                        HapticsVR.PlayGameHaptics(controller, 0.0f, 0.6f, 10f, 0.4f);
                        HapticsVR.effectsTimer = 0.0f;
                    }
                }
            }
        }
    }

    //Laser cutter has a randomized amplitude and on/off period
    [HarmonyPatch(typeof(LaserCutter), nameof(LaserCutter.Update))]
    class LaserCutterHaptics
    {
        public static void Postfix(LaserCutter __instance)
        {
            if (__instance.activeCuttingTarget != null && __instance.activeCuttingTarget.openedAmount < __instance.activeCuttingTarget.maxOpenedAmount && __instance.GetUsedToolThisFrame())
            {
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > HapticsVR.effectsTimeout)
                {
                    //Mod.logger.LogInfo($"LaserCutter.Update {Time.deltaTime}"); 
                    float dur = UnityEngine.Random.Range(0.01f, 0.20f);
                    HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.6f, 10f, UnityEngine.Random.Range(0f, 1.0f));
                    HapticsVR.effectsTimer = 0.0f;
                    HapticsVR.effectsTimeout = UnityEngine.Random.Range(dur, dur + 0.20f);
                }
            }
        }
    }

    //Welder has a kind of slow sine wave haptic
    [HarmonyPatch(typeof(Welder), nameof(Welder.Update))]
    class WelderHaptics
    {
        public static void Postfix(Welder __instance)
        {

            if (__instance.activeWeldTarget != null && __instance.activeWeldTarget.GetHealthFraction() < 1f && __instance.GetUsedToolThisFrame())
            {
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > 0.10)
                {
                    //Mod.logger.LogInfo($"Welder.Update {Time.deltaTime}"); 
                    HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.1f, 10f, HapticsVR.effectsScale);
                    HapticsVR.effectsTimer = 0.0f;
                    HapticsVR.effectsScale += 0.10f;
                    if (HapticsVR.effectsScale > 1.0f)
                    {
                        HapticsVR.effectsScale = 0.0f;
                    }
                }
            }
        }
    }

    //ScannerTool has a kind of quick sine wave haptic
    [HarmonyPatch(typeof(ScannerTool), nameof(ScannerTool.Update))]
    class ScannerToolHaptics
    {
        public static void Postfix(ScannerTool __instance)
        {

            if (PDAScanner.scanTarget.progress > 0 && __instance.GetUsedToolThisFrame())
            {
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > 0.10)
                {
                    //Mod.logger.LogInfo($"ScannerTool.Update {Time.deltaTime}"); 
                    // HapticsVR.PlayHaptics(0.0f, 0.10f, 10f, HapticsVR.effectsScale, false);
                    HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.1f, 10f, HapticsVR.effectsScale);
                    HapticsVR.effectsTimer = 0.0f;
                    HapticsVR.effectsScale += 0.20f;
                    if (HapticsVR.effectsScale > 1.0f)
                    {
                        HapticsVR.effectsScale = 0.0f;
                    }
                }
            }
        }
    }

    //BuilderTool has a kind of quick inverse sine wave haptic
    [HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.Update))]
    class BuilderToolHaptics
    {
        public static void Postfix(BuilderTool __instance)
        {

            if (__instance.isConstructing && __instance.GetUsedToolThisFrame())
            {
                HapticsVR.effectsTimer += Time.deltaTime;
                if (HapticsVR.effectsTimer > 0.10)
                {
                    //Mod.logger.LogInfo($"BuilderTool.Update {Time.deltaTime}"); 
                    HapticsVR.PlayGameHaptics(HapticsVR.Controller.Right, 0.0f, 0.1f, 10f, HapticsVR.effectsScale);
                    HapticsVR.effectsTimer = 0.0f;
                    HapticsVR.effectsScale -= 0.20f;
                    if (HapticsVR.effectsScale < 0.0f)
                    {
                        HapticsVR.effectsScale = 1.0f;
                    }
                }
            }
        }
    }
    /* Doesnt handle the "hover out" so only fires once
        [HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.OnHover))]
        [HarmonyPatch(new Type[] { typeof(Constructable) })]
        public static class ConstructableHoverHaptics
        {
            public static void Postfix(BuilderTool __instance, Constructable constructable)
            {       
                //Play Haptics
                if (constructable.constructed && !constructable.deconstructionAllowed)
                {
                    HapticsVR.lastConstructable = null;
                    return;
                }
                if(constructable == HapticsVR.lastConstructable)
                {
                    return;
                }

                HapticsVR.lastConstructable = constructable;
                HapticsVR.PlayHaptics(0.0f, 0.1f, 10f, 0.5f);
            }
        }

        [HarmonyPatch(typeof(BuilderTool), nameof(BuilderTool.OnHover))]
        [HarmonyPatch(new Type[] { typeof(BaseDeconstructable) })]
        public static class DeconstructableHoverHaptics
        {
            public static void Postfix(BaseDeconstructable __instance, BaseDeconstructable deconstructable)
            {       
                //Play Haptics
                if(deconstructable == HapticsVR.lastDeconstructable)
                {
                    return;
                }

                HapticsVR.lastDeconstructable = deconstructable;
                HapticsVR.PlayHaptics(0.0f, 0.1f, 10f, 0.5f);
            }
        }
    */


    //UI haptics
    [HarmonyPatch(typeof(uGUI_ButtonSound), nameof(uGUI_ButtonSound.OnPointerEnter))]
    public static class uGUI_ButtonSoundHaptics
    {
        public static void Postfix(uGUI_ButtonSound __instance)
        {
            //Mod.logger.LogInfo($"uGUI_ButtonSound.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }
    [HarmonyPatch(typeof(uGUI_OptionSelection), nameof(uGUI_OptionSelection.OnSelect))]
    public static class uGUI_ChoiceSoundHaptics
    {
        public static void Postfix(uGUI_OptionSelection __instance)
        {
            //Mod.logger.LogInfo($"uGUI_ChoiceSound.OnValueChanged");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 1.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }
    /* BZ only
        [HarmonyPatch(typeof(uGUI_ToggleOptionSound), nameof(uGUI_ToggleOptionSound.OnValueChanged))]
        public static class uGUI_ToggleOptionSoundHaptics
        {
            public static void Postfix(uGUI_ToggleOptionSound __instance)
            {
                //Mod.logger.LogInfo($"uGUI_ToggleOptionSound.OnValueChanged");
                HapticsVR.PlayHaptics(0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude, true);
            }
        } 

        [HarmonyPatch(typeof(uGUI_SliderSound), nameof(uGUI_SliderSound.OnValueChanged))]
        public static class uGUI_SliderSoundHaptics
        {
            public static void Postfix(uGUI_SliderSound __instance)
            {
                //Mod.logger.LogInfo($"uGUI_ToggleOptionSound.OnValueChanged");
                HapticsVR.PlayHaptics(0.0f, 0.03f, 20f, 0.2f, true);
            }
        } 
    */
    //This does most of the standard UI work
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerEnter))]
    public static class SelectableHaptics
    {
        public static void Postfix(Selectable __instance)
        {
            //Mod.logger.LogInfo($"Selectable.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }

    //PDA Encyclopedia tab items
    [HarmonyPatch(typeof(uGUI_ListEntry), nameof(uGUI_ListEntry.OnPointerEnter))]
    class ListEntryHaptics
    {
        public static void Postfix(uGUI_ListEntry __instance)
        {
            //Mod.logger.LogInfo($"uGUI_ListEntry.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }

    //The equipents slots in the inventory section of the PDA
    [HarmonyPatch(typeof(uGUI_EquipmentSlot), nameof(uGUI_EquipmentSlot.OnPointerEnter))]
    class EquipmentSlotHaptics
    {
        public static void Postfix(uGUI_EquipmentSlot __instance)
        {
            //Mod.logger.LogInfo($"uGUI_EquipmentSlot.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }

    [HarmonyPatch(typeof(uGUI_BlueprintsTab), nameof(uGUI_BlueprintsTab.OnPointerEnter))]
    class BlueprintsHaptics
    {
        public static void Postfix(uGUI_BlueprintsTab __instance)
        {
            //Mod.logger.LogInfo($"uGUI_EquipmentSlot.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }

    [HarmonyPatch(typeof(uGUI_ItemIcon), nameof(uGUI_ItemIcon.OnPointerEnter))]
    [HarmonyPatch(new Type[] { })]
    class ItemIconHaptics
    {
        public static void Postfix(uGUI_ItemIcon __instance)
        {
            //Mod.logger.LogInfo($"uGUI_ItemIcon.OnPointerEnter");
            HapticsVR.PlayUIHaptics(HapticsVR.Controller.Right, 0.0f, HapticsVR.defaultUIDuration, 10f, HapticsVR.defaultUIAmplitude);
        }
    }


    #endregion

}
