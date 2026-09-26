using System;
using System.Linq;
using HarmonyLib;

namespace MIUULan.Patches;

// -unlockcosmetics and -randomskin
static class Cosmetics
{
    // -unlockcosmetics: answer "owned" for every cosmetic without calling Unlock(), so nothing is
    // written to the save and no unlock notifications appear
    [HarmonyPatch(typeof(UnlockManager), nameof(UnlockManager.IsUnlocked), typeof(Cosmetic), typeof(CosmeticType))]
    static class UnlockManagerPatch
    {
        static void Postfix(ref bool __result)
        {
            if (Lan.Enabled && Lan.UnlockCosmetics)
                __result = true;
        }
    }

    // Kill achievements when cosmetic unlock flag is active
    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.CheckAllTrophies))]
    static class AchievementManagerPatch
    {
        static bool Prefix() => !(Lan.Enabled && Lan.UnlockCosmetics);
    }

    // Ignore inventory backend, allow all cosmetics
    [HarmonyPatch(typeof(MPUtil), "ApplyOwnedCosmetics")]
    static class MPUtilPatch
    {
        static bool Prefix() => !(Lan.Enabled && Lan.UnlockCosmetics);
    }

    // -randomskin: each LAN player starts on a random marble skin (trail and hat are left alone)
    [HarmonyPatch(typeof(CosmeticManager))]
    static class CosmeticManagerPatch
    {
        static Cosmetic rolled;
        static bool applied;
        // UnityEngine.Random is clock-seeded, so instances launched together roll the same skin.
        static readonly Random random = new(Guid.NewGuid().GetHashCode());

        static bool Wanted => Lan.Enabled && Lan.RandomSkin;

        // Roll and equip right away without waiting for cloud preferences
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void Awake()
        {
            if (Wanted && Lan.UnlockCosmetics)
                Equip(rolled = Roll());
        }

        // Apply roll when called the first time at game load, but not when the player updates their own skin manually
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CosmeticManager.UpdateSelected))]
        static void UpdateSelected()
        {
            if (!Wanted || applied)
                return;
            applied = true;
            rolled ??= Roll();
            if (rolled != null)
            {
                MarbleSelect.currentPreset.skinID = rolled.Id;
                Equip(rolled);
            }
        }

        static Cosmetic Roll()
        {
            var skins = CosmeticManager.Skins.Where(IsRollable).ToArray();
            if (skins.Length == 0)
                return null;
            var skin = skins[random.Next(skins.Length)];
            Lan.Log($"random skin: {skin.Id} (from {skins.Length})");
            return skin;
        }

        static void Equip(Cosmetic skin)
        {
            if (skin != null)
                CosmeticManager.MySkin = skin;
        }

        // Skip the developer-only marble and the ones the game marks as not for players
        // ("NOT AVAILABLE TO PLAYERS - UNLOCK ALL ONLY", "NOT PLAYER UNLOCKABLE")
        // Developer marble has multiplayer detection built-in, others are just confusing
        static bool IsRollable(Cosmetic skin) =>
            (Lan.UnlockCosmetics || UnlockManager.Get().IsUnlocked(skin, CosmeticType.Skin))
            && skin.Id != "DeveloperMarble"
            && !$"{skin.flavorText} {skin.unlockText}".Contains("PLAYER");
    }
}
