using System.Collections;
using System.Linq;
using HarmonyLib;
using Steamworks.NET;
using UnityEngine;

namespace MIUULan.Patches;

// -nosteam: never start Steamworks (see Lan.SteamDisabled).
// This is really just to avoid menu lag since this expects to run in a native-ish environment.
static class NoSteam
{
    static IEnumerator Done() => Enumerable.Empty<object>().GetEnumerator();

    // Skip SteamAPI.Init. OnEnable still registers the instance and Initialized stays false.
    [HarmonyPatch(typeof(SteamManager), "Awake")]
    static class SteamManagerPatch
    {
        static bool Prefix(SteamManager __instance)
        {
            if (!Lan.SteamDisabled)
                return true;
            Object.DontDestroyOnLoad(__instance.gameObject);
            return false;
        }
    }

    // Don't wait.
    [HarmonyPatch(typeof(SteamInitBarrier), nameof(SteamInitBarrier.WaitUntilReady))]
    static class SteamInitBarrierPatch
    {
        static bool Prefix(ref IEnumerator __result)
        {
            if (!Lan.SteamDisabled)
                return true;
            __result = Done();
            return false;
        }
    }

    // SteamId stays at its default of 0, so the network ID is STEAM_0, or STEAM_0_LAN<n> for players after the first.
    [HarmonyPatch(typeof(PlatformSetup), "AttachSteamNetwork")]
    static class PlatformSetupPatch
    {
        static bool Prefix(ref IEnumerator __result)
        {
            if (!Lan.SteamDisabled)
                return true;
            PlatformSetup.OfflineMode = true;
            
            SteamUGCManager.Initialized = true;
            __result = Done();
            return false;
        }
    }
}
