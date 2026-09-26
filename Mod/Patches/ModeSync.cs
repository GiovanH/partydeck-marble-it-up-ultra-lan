using HarmonyLib;

namespace MIUULan.Patches;

// The host sets the mode in Lan.LanHost (SetupNetworkMode); clients get it from the host's ModeRPC.
static class ModeSync
{
    // RoomMode is synced from the master server room, which is empty in LAN mode. Stub.
    [HarmonyPatch(typeof(MultiplayerPanel), "ApplyRoomMode")]
    static class MultiplayerPanelPatch
    {
        static bool Prefix() => !Lan.Enabled;
    }
}
