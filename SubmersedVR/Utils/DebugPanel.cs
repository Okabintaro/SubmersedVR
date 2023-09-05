using HarmonyLib;
using UnityEngine;
using TMPro;

namespace SubmersedVR
{
    // This behaves like ErrorMesage.Show(), but is designed to not spam with changing values
    // I used this to debug/view certain positions in the UI
    class DebugPanel : MonoBehaviour
    {

        TextMeshProUGUI entry;
        public static DebugPanel main;

        void Start()
        {
            var prefabMessage = ErrorMessage.main.prefabMessage;
            GameObject obj = Object.Instantiate(prefabMessage);
            entry = obj.GetComponent<TextMeshProUGUI>();
            entry.rectTransform.SetParent(ErrorMessage.main.messageCanvas, false);
            obj.SetActive(true);
            entry.text = "";

            main = this;
            Settings.IsDebugChanged += (isOn) =>
            {
                this.enabled = isOn;
            };
        }

        void OnDisable()
        {
            if (entry)
            {
                entry.text = "";
                entry.enabled = false;
            }
        }

        void OnEnable()
        {
            if (entry)
                entry.enabled = true;
        }

        public static void Show(string message, bool writeToLog = false)
        {
            if (DebugPanel.main == null)
            {
                return;
            }
            DebugPanel.main.entry.text = message;
            if(writeToLog)
            {
                Mod.logger.LogInfo($"{message}");
            }
        }

        // There might be a better hook for this
        [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
        public static class uGUI_CreateDebug
        {
            [HarmonyPostfix]
            public static void Postfix(uGUI_MainMenu __instance)
            {
                __instance.gameObject.AddComponent<DebugPanel>();
            }
        }

    }
}
