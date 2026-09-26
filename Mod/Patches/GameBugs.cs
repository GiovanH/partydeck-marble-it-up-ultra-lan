using HarmonyLib;

namespace MIUULan.Patches;

// Workarounds for bugs in the game itself that LAN play runs into.
static class GameBugs
{
    // An empty "Blocked" pref resolves to [""], but MPUserIDs are empty by default,
    // so blocked warnings can flash before ids load. Ignore IsUserBlocked for lan.
    [HarmonyPatch(typeof(MPUtil), nameof(MPUtil.IsUserBlocked))]
    static class MPUtilPatch
    {
        static bool Prefix(string userID, ref bool __result)
        {
            if (!Lan.Enabled || !string.IsNullOrEmpty(userID))
                return true;
            __result = false;
            return false;
        }
    }

    // The original turns runInBackground off, which LAN instances can't have, and clears Steam rich
    // presence, which throws when Steam isn't initialized under Goldberg or with Steam not running.
    // That exception skipped ReturnToMenu and left the player stuck in the level.
    [HarmonyPatch(typeof(MultiplayerPanel), nameof(MultiplayerPanel.OnLeftRoom))]
    static class MultiplayerPanelPatch
    {
        static bool Prefix(MultiplayerPanel __instance)
        {
            if (!Lan.Enabled)
                return true;
            __instance.HideQueueGroup();
            __instance.ReturnToMenu();
            MPScoreWidget.SetObjectiveText("");
            return false;
        }
    }
}
