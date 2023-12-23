using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using FMODUnity;

namespace SubmersedVR
{
    class ParticleFX
    {
        /* Tests dont show any obvious benefit 
        [HarmonyPatch(typeof(VFXDestroyAfterSeconds), nameof(VFXDestroyAfterSeconds.OnEnable))]
        class VFXDestroyAfterSeconds_OnEnable_Patch
        {
            static void Postfix(VFXDestroyAfterSeconds __instance)
            {
                if(Settings.EnableParticleFix)
                {
                    foreach (ParticleSystemRenderer particleSR in __instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        particleSR.allowRoll = false;
                        particleSR.alignment = ParticleSystemRenderSpace.Facing;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(AmbientParticles), nameof(AmbientParticles.Init))]
        class AmbientParticles_Init_Patch
        {
            static void Postfix(AmbientParticles __instance)
            {
                if(Settings.EnableParticleFix)
                {
                    foreach (ParticleSystemRenderer particleSR in __instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        particleSR.allowRoll = false;
                        particleSR.alignment = ParticleSystemRenderSpace.Facing;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(VFXController), nameof(VFXController.SpawnFX))]
        class VFXController_Start_Patch
        {
            static void Postfix(VFXController __instance, int i)
            {
                if(Settings.EnableParticleFix)
                {
                    if (__instance.emitters[i].fxPS != null)
                    {
                        foreach (ParticleSystemRenderer particleSR in __instance.emitters[i].fxPS.gameObject.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        {
                            particleSR.allowRoll = false;
                            particleSR.alignment = ParticleSystemRenderSpace.Facing;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.MakeBubbles))]
        class PlayerBreathBubbles_MakeBubbles_Patch
        {
            static void Postfix(PlayerBreathBubbles __instance, GameObject ___bubbles)
            {
                if(Settings.EnableParticleFix)
                {
                    if (___bubbles)
                    {
                        foreach (ParticleSystemRenderer particleSR in ___bubbles.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        {
                            particleSR.allowRoll = false;
                            particleSR.alignment = ParticleSystemRenderSpace.Facing;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ResourceTracker), nameof(ResourceTracker.Start))]
        class ResourceTracker_Start_Patch
        {
            static void Postfix(ResourceTracker __instance, TechType ___techType)
            {
                if(Settings.EnableParticleFix)
                {
                    if (___techType == TechType.HeatArea)
                    {
                        foreach (ParticleSystemRenderer particleSR in __instance.gameObject.GetComponentsInChildren<ParticleSystemRenderer>(true))
                        {
                            particleSR.allowRoll = false;
                            particleSR.alignment = ParticleSystemRenderSpace.Facing;
                        }
                    }
                }
            }
        }
        */
    }
}