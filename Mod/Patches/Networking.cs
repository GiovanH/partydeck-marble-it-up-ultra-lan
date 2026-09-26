using HarmonyLib;
using TECNet;

namespace MIUULan.Patches;

static class Networking
{
    // In LAN mode bind 39010 + player number instead so the host (player 0) is always on 39010.
    // Otherwise instances started together race for 39010, and the host can end up on 39011.
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.Get))]
    static class NetworkManagerPatch
    {
        static void Prefix(NetworkManager ____Instance)
        {
            if (!Lan.Enabled || ____Instance == null || ____Instance.Interface != null)
                return;
            var address = new Address();
            address.SetAny(Lan.DefaultPort + Lan.Id);
            
            // Refuse joins until lanHost; otherwise a client that starts first connects to a menu.
            ____Instance.Interface = new NetworkInterface(address, autoFindPort: true) { DoesAllowConnections = false };
            if (____Instance.Interface.IsBound)
                Lan.Log("listening on " + ____Instance.Interface.GetFirstBoundInterfaceAddress().ToShortString());
        }
    }
}
