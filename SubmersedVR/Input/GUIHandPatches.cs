using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{

	// To move the UI of Fabricators, Storage etc in front of the player, we have to set the uGUI_CanvasScaler dirty flag.
	// That way in UpdateTransform.UpdateTransform() the UI will be set to be infront of the UI Camera when vrMode is set to static.
	[HarmonyPatch(typeof(GUIHand), nameof(GUIHand.Send))]
	class ResetStaticCanvasScalerOnInteract : MonoBehaviour {
		public static void DirtyAllCanvases() {
			// TODO: Should cache this maybe, not sure when those could change though
            FindObjectsOfType<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
		}

		public static void Postfix(GameObject target, HandTargetEventType e, GUIHand hand) {
			if (e == HandTargetEventType.Click) {
				DirtyAllCanvases();
			}
		}
	}
}