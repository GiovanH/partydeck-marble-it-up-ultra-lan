using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MIUULan.Patches;

// Never contact the official master server or stats backend, and allow multiplayer without them.
static class OfficialServers
{
    // TECNet.MasterClient is internal, so it is targeted by name.
    internal static readonly Type MasterClient = AccessTools.TypeByName("TECNet.MasterClient");

    [HarmonyPatch]
    static class MasterClientPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(MasterClient, "Process");
            yield return AccessTools.Method(MasterClient, "ReportRoom");
            yield return AccessTools.Method(MasterClient, "TerminateServing");
        }

        static bool Prefix() => !Lan.Enabled;
    }

    // Dummy reports to official backend
    [HarmonyPatch(typeof(MPWorldState), "OnMatchEndSubmit")]
    static class MPWorldStatePatch
    {
        static bool Prefix() => !Lan.Enabled;
    }

    // Allow multiplayer menus/buttons even when steam is unreachable
    [HarmonyPatch(typeof(MainMenuPanel), "OnDisallowMultiplayer")]
    static class MainMenuPanelPatch
    {
        static bool Prefix(MainMenuPanel __instance)
        {
            if (!Lan.Enabled)
                return true;
            AccessTools.Method(typeof(MainMenuPanel), "OnAllowMultiplayer").Invoke(__instance, null);
            return false;
        }
    }
}
