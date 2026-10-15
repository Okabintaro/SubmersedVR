using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    static class VehiclesVR
    {
        public static Exosuit PilotedExosuit()
        {
            return Player.main?.currentMountedVehicle == null ? null : Player.main?.currentMountedVehicle as Exosuit;
        }
    }

    //Recenters the VR view a short delay after entering a vehicle,
    //so the player can straighten their head before the orientation is locked in
    static class DelayedVehicleRecenter
    {
        static int token;

        public static void Schedule()
        {
            var rig = VRCameraRig.instance;
            if (rig == null) return;
            rig.StartCoroutine(RecenterAfterDelay(++token));
        }

        static IEnumerator RecenterAfterDelay(int current)
        {
            yield return new WaitForSeconds(Settings.VehicleRecenterDelay);
            if (current != token) yield break;
            if (Player.main?.currentMountedVehicle == null) yield break;
            VRUtil.Recenter();
        }
    }

    //This gets called when starting to pilot the seamoth and exosuit but not the cyclops
    [HarmonyPatch(typeof(Player), nameof(Player.EnterLockedMode))]
    static class RecenterWhenPilotingLocked
    {
        public static void Postfix()
        {
            DelayedVehicleRecenter.Schedule();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.EnterPilotingMode))]
    static class RecenterWhenPilotingCyclops
    {
        public static void Postfix()
        {
            DelayedVehicleRecenter.Schedule();
        }
    }

    //This gets called when, while piloting the cyclops, camera mode is turned on
    [HarmonyPatch(typeof(CyclopsExternalCams), nameof(CyclopsExternalCams.SetActive))]
    static class RecenterWhenUsingCyclopsCams
    {
        public static void Postfix()
        {
            VRUtil.Recenter();
        }
    }

}