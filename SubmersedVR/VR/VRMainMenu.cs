using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace SubmersedVR
{
    class VRMainMenu : MonoBehaviour
    {
        // This simply makes the VRCameraRig rig steal the main menu cameras
        // TODO: Can probably be done without an extra behaviour.
        public static void SetupMainMenu()
        {
            Camera uiCamera = FindObjectsOfType<Camera>().First(c => c.name.Equals("UI Camera"));
            VRCameraRig.instance.StealUICamera(uiCamera);
            Camera mainCamera = GameObject.FindGameObjectsWithTag("MainCamera").First(c => c.name.Equals("Main Camera")).GetComponent<Camera>();
            VRCameraRig.instance.StealCamera(mainCamera);

            var screenCanvas = uGUI.main.screenCanvas.gameObject;
            screenCanvas.WithParent(uiCamera.transform).ResetTransform();
        }
    }

    [HarmonyPatch(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Awake))]
    public static class MainMenu_SetupVR
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRMainMenu.SetupMainMenu();
        }
    }
}
