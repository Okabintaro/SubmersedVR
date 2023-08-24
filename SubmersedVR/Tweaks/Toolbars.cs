using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{
    // Tweaks regarding the toolbars in VR mode of the game
    static class ToolbarsVR
    {

    }

    #region Patches

    //Make the cycle actions work in Builder Menu Toolabr
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Update))]
    class uGUI_BuilderMenu_ToolbarTab_Fixer
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            if(uGUI_BuilderMenu.IsOpen())
            {
                if (GameInput.GetButtonDown(GameInput.Button.UINextTab))
                {
                    int currentTab = (__instance.TabOpen + 1) % __instance.TabCount;
                    __instance.SetCurrentTab(currentTab);
                }
                if (GameInput.GetButtonDown(GameInput.Button.UIPrevTab))
                {
                int currentTab = (__instance.TabOpen - 1 + __instance.TabCount) % __instance.TabCount;
                    __instance.SetCurrentTab(currentTab);
                }
            }
       }
    }

    //Make the cycle actions work in PDA Toolabr
    [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.Update))]
    class uGUI_PDA_ToolbarTab_Fixer
    {
        public static void Postfix(uGUI_PDA __instance)
        {
            if(Player.main.GetPDA().isInUse)
            {
                if (GameInput.GetButtonDown(GameInput.Button.UINextTab))
                {
                    int currentTabIndex = (__instance.currentTabs.IndexOf(__instance.tabOpen) + 1) % __instance.currentTabs.Count;
                    __instance.OpenTab( __instance.currentTabs[currentTabIndex]);
                }
                if (GameInput.GetButtonDown(GameInput.Button.UIPrevTab))
                {
                    int currentTabIndex = (__instance.currentTabs.IndexOf(__instance.tabOpen) - 1 + __instance.currentTabs.Count) % __instance.currentTabs.Count;
                    __instance.OpenTab( __instance.currentTabs[currentTabIndex]);
                }
            }
       }
    }

    //Hide the tooltip for cycling toolbar tabs since there isnt a good one for left dpad up and down for index controllers
    [HarmonyPatch(typeof(uGUI_Toolbar), nameof(uGUI_Toolbar.UpdateCycleBtnText))]
    class uGUI_Toolbar_Hints_Fixer
    {
        public static bool Prefix(uGUI_Toolbar __instance)
        {
			if (__instance.cycleLeftBtnText != null)
			{
				__instance.cycleLeftBtnText.text = uGUI.FormatButton(GameInput.Button.CyclePrev, false, " / ", false);
				__instance.cycleLeftBtnText.gameObject.SetActive(false);
			}
			if (__instance.cycleRightBtnText != null)
			{
				__instance.cycleRightBtnText.text = uGUI.FormatButton(GameInput.Button.CycleNext, false, " / ", false);
				__instance.cycleRightBtnText.gameObject.SetActive(false);
			}

            return false;
        }
    }

    #endregion

}
