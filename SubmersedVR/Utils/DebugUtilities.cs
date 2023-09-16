using UnityEngine;
using HarmonyLib;


namespace SubmersedVR
{
    public static class DebugUtilities
    {
        public static bool adjustAngle = false;
        public static void ManualPositionAdjustments(float x, float y, float z)
        {
            string result = adjustAngle ? "Adjusting angles\n" : "Adjusting positions\n";
            if(adjustAngle)
            {
                //result += WristHud.AdjustHUD(0, 0, 0, x, y, z);
                //result += VRHands.AdjustPDA(0, 0, 0, x, y, z);
            }
            else
            {
                //result += WristHud.AdjustHUD(x, y, z, 0, 0, 0);
                //result += VRHands.AdjustPDA(x, y, z, 0, 0, 0);
            }
            //result += VRHands.AdjustFinger(x, y, z);

            DebugPanel.Show($"{result}", true);
        }
        public static void ToggleAdjustmentType()
        {
            DebugUtilities.adjustAngle = !DebugUtilities.adjustAngle;
            string result = adjustAngle ? "Adjusting angles\n" : "Adjusting positions\n";
            DebugPanel.Show($"{result}", true);
        }
    
    }

    
    [HarmonyPatch(typeof(GameInput), nameof(GameInput.UpdateKeyInputs))]
    public  class GameInputKeyTracker : MonoBehaviour
    {
        public static void Postfix(GameInput __instance, bool useKeyboard, bool useController)
        {
            if(Settings.IsDebugEnabled == false) { return; }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] != -1)
            {
               //DebugPanel.Show($"Key ID: {GameInput.lastInputPressed[(int)GameInput.lastDevice]}", true);
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 107) //F6 key
            {
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 106) //F5 key
            {
                //SteamVrGameInput.debugSnapTurn = !SteamVrGameInput.debugSnapTurn;
                //Mod.logger.LogInfo($"Snap turn debugging is {SteamVrGameInput.debugSnapTurn}");
                /*
                foreach (HideForScreenshots hideForScreenshots in UnityEngine.Object.FindObjectsOfType<HideForScreenshots>())
                {
                    Mod.logger.LogInfo($"HideForScreenshots {hideForScreenshots.type} {hideForScreenshots.name} {hideForScreenshots.tag} {hideForScreenshots.enabled}");
                }   
                */        
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 84) //Y+
            {
                DebugUtilities.ManualPositionAdjustments(0f, 1f, 0f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 78) //Y-
            {
                DebugUtilities.ManualPositionAdjustments(0f, -1f, 0f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 82) //X+
            {
                DebugUtilities.ManualPositionAdjustments(1f, 0f, 0f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 80) //X-
            {
                DebugUtilities.ManualPositionAdjustments(-1f, 0f, 0f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 83) //Z+
            {
                DebugUtilities.ManualPositionAdjustments(0f, 0f, 1f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 77) //Z-
            {
                DebugUtilities.ManualPositionAdjustments(0f, 0f, -1f);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 76) //0 INS
            {
                DebugUtilities.ToggleAdjustmentType();
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 61) //Pinky 1
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_PinkyFinger1);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 45) //Pinky 2
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_PinkyFinger2);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 70) //Pinky 3
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_PinkyFinger3);
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 67) //Ring 1
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_RingFinger1);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 63) //Ring 2
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_RingFinger2);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 68) //Ring 3
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_RingFinger3);
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 49) //Middle 1
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_MiddleFinger1);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 48) //Middle 2
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_MiddleFinger2);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 47) //Middle 3
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_MiddleFinger3);
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 62) //Index 1
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_IndexFinger1);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 50) //Index 2
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_IndexFinger2);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 66) //Index 3
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_IndexFinger3);
            }

            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 64) //Thumb 1
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_Thumb1);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 51) //Thumb 2
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_Thumb2);
            }
            if(GameInput.lastInputPressed[(int)GameInput.lastDevice] == 46) //Thumb 3
            {
                VRHands.instance.SetCurrentEditFinger((int)VRHands.HandSkeletonBone.eBone_Thumb3);
            }

        }
    }


/*
    //Make building with the fabricator much quicker
    [HarmonyPatch(typeof(Constructable), nameof(Constructable.GetConstructInterval))]
    public static class AutoBuildFix
    {
        public static bool Prefix(Base __instance, ref float __result)
        {           
           __result = 0.05f;
           return false;
        }
    }
*/

}
