using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{ 
    // Tweaks regarding the breath and bubbles in VR mode of the game
    static class BreathBubblesVR
    {

    }

    #region Patches

    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.MakeBubbles))]
    public static class PlayerBubbles_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
            //We are going to temporarily make the vrcamera the parent during the transform calculation 
            //and we want to set the parent back to the original when we are done
            //This makes it so you can spin in the real world and the bubbles will orient correctly (unlike the flippers)
            //but also does not lock the generated bubbles to the head so that you can look around and see them
            Transform parent = __instance.anchor.parent;
            Camera camera = VRCameraRig.instance?.vrCamera;
			if (camera != null)
			{
                __instance.anchor.SetParent(camera.transform);

 				Transform transform = camera.transform;
				Vector3 vector = transform.position + transform.forward * 0.2f; //put the bubbles a little out in front
                vector += new Vector3(0f, -0.2f, 0.0f); //move the bubbles a little lower toward the mouth
				Quaternion quaternion = transform.rotation;
                __instance.anchor.transform.SetPositionAndRotation(vector, quaternion);
                __instance.anchor.transform.localRotation = Quaternion.Euler( new Vector3(-90f, 0f, 0f) ); //setting the bubbles transform to match the camera breaks the bubbles x angle. Fix it here
                
                __instance.anchor.SetParent(parent);
			}        

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.MakeBreath))]
    public static class PlayerBreath_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
            //Without this the spy penguin shows breath
            if(uGUI_SpyPenguin.main.activePenguin == true)
            {
                __instance.Invoke("MakeBreath", 4f);
                return false;
            }
            //We are going to temporarily make the vrcamera the parent during the transform calculation 
            //and we want to set the parent back to the original when we are done
            //This makes it so you can spin in the real world and the breath will orient correctly (unlike the flippers)
            //but also does not lock the generated breath to the head so that you can look around and see it
            Transform parent = __instance.breathAnchor.parent;
            Camera camera = VRCameraRig.instance?.vrCamera;
			if (camera != null)
			{
                __instance.breathAnchor.SetParent(camera.transform);

 				Transform transform = camera.transform;
				Vector3 vector = transform.position + transform.forward * 0.01f; //put the breath a little out in front
                vector += new Vector3(0f, -0.07f, 0.0f); //move the breath a little lower toward the mouth
				Quaternion quaternion = transform.rotation;
                __instance.breathAnchor.transform.SetPositionAndRotation(vector, quaternion);
                __instance.breathAnchor.transform.localRotation = Quaternion.Euler( new Vector3(10f, 0f, 0f) ); 
                
                __instance.breathAnchor.SetParent(parent);
			}        
            
            return true;
        }
    }
/*
    //Make bubbles generate more often
    [HarmonyPatch(typeof(PlayerBreathBubbles), nameof(PlayerBreathBubbles.Start))]
    public static class PlayerBreathBubbles_Patch2
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerBreathBubbles __instance)
        {
           __instance.delay = 5.0f;
            return true;
        }
    }
*/
    #endregion

}
