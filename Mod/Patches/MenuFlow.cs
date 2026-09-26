using System.Collections;
using HarmonyLib;
using TMPro;

namespace MIUULan.Patches;

// REWRITTEN MENU BEHAVIOR FOR LAN PLAY:
// In LAN mode the Multiplayer button leads player 0 straight to the private game menu
// Every other player's Multiplayer button directly joins player 0.
static class MenuFlow
{
    // Set by the main menu's Multiplayer button.
    static bool joinOnEnter;

    [HarmonyPatch(typeof(MainMenuPanel))]
    static class MainMenuPanelPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MainMenuPanel.TryEnterMPUI))]
        static bool TryEnterMPUI()
        {
            if (!Lan.Enabled)
                return true;
            if (MapLoad.initialized && !JoystickUISelector.ignoreInput)
            {
                joinOnEnter = true;
                PanelManager.instance.PushPanel(PanelType.Multiplayer);
            }
            return false;
        }

        // Label the button for what it does in LAN mode. The game rewrites this text whenever
        // multiplayer is allowed or disallowed, and on language change.
        [HarmonyPostfix]
        [HarmonyPatch("UpdateMPButtonLanguage")]
        static void UpdateMPButtonLanguage(MainMenuPanel __instance)
        {
            if (Lan.Enabled)
                __instance.MPButton.GetComponentInChildren<TextMeshProUGUI>().text = Lan.Id == 0 ? "Host" : "Join";
        }
    }

    [HarmonyPatch(typeof(MultiplayerPanel))]
    static class MultiplayerPanelPatch
    {
        // Runs whenever the panel is pushed: from the main menu, and again by ReturnToMenu after
        // leaving a match. Leaving a match must not auto-join again.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MultiplayerPanel.EnterPanel))]
        static void EnterPanel(MultiplayerPanel __instance)
        {
            if (!Lan.Enabled)
                return;
            if (Lan.Id == 0)
            {
                __instance.StartCoroutine(OpenCreatorWhenSelected(__instance));
            }
            else if (joinOnEnter)
            {
                joinOnEnter = false;
                Lan.LanJoin();
            }
        }

        // Switching to the private game menu before then leaves focus on a hidden button
        // HACK: move focus a few frames later
        static IEnumerator OpenCreatorWhenSelected(MultiplayerPanel panel)
        {
            for (int frame = 0; frame < 10 && (frame < 3 || JoystickUISelector.ignoreInput); frame++)
                yield return null;
            panel.GoToCreatePanel();
        }

        // Every matchmaking button routes here
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MultiplayerPanel.JoinOrCreateGame))]
        static bool JoinOrCreateGame()
        {
            if (!Lan.Enabled)
                return true;
            if (Lan.Id == 0)
                Lan.LanHost();
            else
                Lan.LanJoin();
            return false;
        }

        // Back from the private game menu goes to the main menu, not the matchmaking menu.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MultiplayerPanel.GoBack))]
        static void GoBack(MultiplayerPanel __instance)
        {
            if (Lan.Enabled && __instance.curMode == MultiplayerPanel.PanelMode.Creator)
                __instance.curMode = MultiplayerPanel.PanelMode.Base;
        }
    }
}
