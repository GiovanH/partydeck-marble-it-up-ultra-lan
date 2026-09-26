using System;
using HarmonyLib;
using MIU;
using Rewired;
using TECNet;
using UnityEngine;

namespace MIUULan;

// New behavior and manager for lan play logic
public static class Lan
{
    public const ushort DefaultPort = 39010;

    public static bool Enabled { get; private set; }
    public static int Id { get; private set; }

    // -unlockcosmetics: every cosmetic reads as owned. Runtime only, never saved; LAN mode only.
    public static bool UnlockCosmetics { get; private set; }

    // -randomskin: equip a random unlocked marble skin at startup. LAN mode only.
    public static bool RandomSkin { get; private set; }

    // -nosteam: never start Steamworks; play as an offline profile. LAN mode only.
    public static bool NoSteam { get; private set; }

    public static bool SteamDisabled => Enabled && NoSteam;

    // For debugging. Shown in overlay and console.
    public static string Status { get; private set; } = "idle";

    public static void ParseArgs()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-lan":
                    Enabled = true;
                    break;
                case "-unlockcosmetics":
                    UnlockCosmetics = true;
                    break;
                case "-randomskin":
                    RandomSkin = true;
                    break;
                case "-nosteam":
                    NoSteam = true;
                    break;
                // PartyDeck passes the zero-indexed instance number; player 0 hosts, the rest join it.
                case "-player" or "-lanid" when i + 1 < args.Length && int.TryParse(args[i + 1], out int id):
                    Id = id;
                    Enabled = true;
                    i++;
                    break;
            }
        }
    }

    public static void Enable()
    {
        if (Enabled)
            return;
        Enabled = true;
        ApplyLanSettings();
    }

    public static void ApplyLanSettings()
    {
        if (!Enabled)
            return;

        // Don't kick inactive players
        MultiplayerGameMode.KICK_TIME = float.PositiveInfinity;

        // Every instance but one is unfocused, and each must still read its own pad.
        Application.runInBackground = true;
        // Rewired may not be up yet at the first scene load, and it can re-initialize later.
        ReInput.InitializedEvent += KeepInputWhenUnfocused;
        if (ReInput.isReady)
            KeepInputWhenUnfocused();
    }

    static void KeepInputWhenUnfocused() =>
        ReInput.configuration.ignoreInputWhenAppNotInFocus = false;

    // Parse IP from IP + port
    public static IPAddress ParseAddress(string text)
    {
        var parts = text.Trim().Split(':');
        ushort port = DefaultPort;
        if (parts.Length > 2 || !System.Net.IPAddress.TryParse(parts[0], out var ip)
            || (parts.Length == 2 && !ushort.TryParse(parts[1], out port)))
            return null;
        return new IPAddress { netNum = ip.GetAddressBytes(), port = port };
    }

    public static ushort LocalPort =>
        NetworkManager.Get()?.Interface?.GetFirstBoundInterfaceAddress().Port ?? 0;

    // MultiplayerPanel.EnterGameInner quits the app when this is false, and it is
    // only set by the AllowMultiplayer event from the platform/master checks.
    static void AllowMultiplayer() =>
        AccessTools.Field(typeof(MultiplayerPanel), "mp").SetValue(null, true);

    // Only the multiplayer handshake gets a per-instance ID.
    // Game XORs save files with the real id for "encryption"
    public static string MpNetworkId(ref NetworkAccount account)
    {
        string id = account.GetNetworkId();
        return Enabled && Id != 0 ? $"{id}_LAN{Id}" : id;
    }

    public static void Log(string message) => Debug.Log("[MIUULan] " + message);

    static string Report(string message)
    {
        Status = message;
        Log("Status: " + message);
        return message;
    }

    [ConsoleCommand("lanhost", "Host a LAN game with the selected mode and map")]
    public static string LanHost()
    {
        Enable();
        AllowMultiplayer();

        // LoadServerMap reads this; only HeadlessLogic or the master path sets it otherwise.
        var settings = CreateGameSettings.instance;
        var map = GlobalContext.LevelData.multiplayerMaps[settings.selectedMap];
        GlobalContext.MultiplayerLevelID = map.id;
        
        CreateGameSettings.SetupNetworkMode((int)settings.selectedMode);
        AccessTools.Method(typeof(MultiplayerPanel), "CreateLocalGame").Invoke(MultiplayerPanel.instance, null);
        NetworkManager.Get().Interface.DoesAllowConnections = true;
        // CreateNewRoom leaves the panel InQueue; MPGameInfoUI hides the menus once the camera finds
        // the player marble, but only while InQueue, so don't hide the queue group here.
        SaveIndicator.isSaving = false;
        return Report($"hosting {settings.selectedMode} on {map.id}; join with: lanjoin {LocalPort}");
    }

    [ConsoleCommand("lanjoin", "Join a LAN game directly", "[ip:port | port]")]
    public static string LanJoin(params string[] args)
    {
        string target = args.Length > 0 ? args[0] : DefaultPort.ToString();
        if (ushort.TryParse(target, out _))
            target = "127.0.0.1:" + target;
        var address = ParseAddress(target);
        if (address == null)
            return Report("bad address: " + target);
        if (address.netNum[0] == 127 && address.port == LocalPort)
            return Report($"{address} is this instance's own port; use the host's port.");
        Enable();
        AllowMultiplayer();
        AccessTools.Field(Patches.OfficialServers.MasterClient, "JoinAddy").SetValue(null, address);
        // JoinOrCreateGame normally sets InQueue; MPGameInfoUI needs it to dismiss the menus on join.
        AccessTools.Method(typeof(MultiplayerPanel), "UpdateCanvasForMode")
            .Invoke(MultiplayerPanel.instance, new object[] { MultiplayerPanel.PanelMode.InQueue });
        MultiplayerPanel.instance.JoinRoom(address);
        return Report("joining " + address);
    }
}
