using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MIU;

namespace MIUULan.Patches;

// Instances on one Steam account share a network ID and name; give each its own for multiplayer
static class Identity
{
    // Swap in Lan.MpNetworkId only where the ID is sent to the host: the connect data and the
    // UserInfo RPC that sets MPUserID.
    [HarmonyPatch]
    static class NetworkAccountPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MultiplayerPanel), "StartConnect");
            yield return AccessTools.Method(typeof(MPUtil), nameof(MPUtil.SendCosmeticInfo));
        }

        // Use Lan MpNetworkId instead
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            instructions.MethodReplacer(
                AccessTools.Method(typeof(NetworkAccount), nameof(NetworkAccount.GetNetworkId)),
                AccessTools.Method(typeof(Lan), nameof(Lan.MpNetworkId)));
    }

    // Player name
    [HarmonyPatch(typeof(MPUtil), nameof(MPUtil.GetUserName))]
    static class MPUtilPatch
    {
        static void Postfix(ref string __result)
        {
            if (Lan.Enabled)
                __result = $"Player{Lan.Id + 1}";
        }
    }
}
